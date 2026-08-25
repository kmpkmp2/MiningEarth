using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Map;

namespace DeepEarth.Mining
{
    public class MiningSystem : MonoBehaviour
    {
        private static MiningSystem _instance;
        public static MiningSystem Instance => _instance;

        [SerializeField] private Transform spawnPoint;

        private BlockPresenter _currentBlockPresenter;
        private GameObject _currentBlockObject;

        private readonly Dictionary<BlockType, MineralData> _mineralDataCache = new Dictionary<BlockType, MineralData>();
        private UniTask _mineralDataLoadTask;
        private bool _mineralDataLoadStarted;

        public event Action OnBlockCleared;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Transform blockSpawnPoint)
        {
            spawnPoint = blockSpawnPoint;
        }

        public async UniTask SpawnNextBlockAsync()
        {
            // Clear current block if any
            ClearCurrentBlock();

            await EnsureMineralDataLoadedAsync();

            int depth = GameManager.Instance.CurrentDepth;
            BlockType type = ChooseBlockTypeByDepth(depth);
            string addressableKey = GetAddressableKeyForBlock(type);

            // Load and instantiate from PoolSystem
            _currentBlockObject = await PoolSystem.Instance.GetAsync(addressableKey, spawnPoint);
            if (_currentBlockObject == null)
            {
                Debug.LogError($"Failed to spawn block: {addressableKey}");
                return;
            }

            // BlockSpawnRoot는 MapRoot의 자식이고, MapPresenter.HandleBlockMinedAsync()가 채굴마다
            // MapView.MoveMapBack()으로 MapRoot를 -Z로 1유닛씩 계속 밀어낸다(터널 스크롤 연출).
            // 그래서 여기서 depth를 로컬 Z로 다시 밀어 넣어야 MapRoot의 누적 이동을 상쇄해서
            // 블록이 카메라 기준 항상 같은 월드 위치에 스폰된다 — depth를 안 쓰면(과거 수정) 블록이
            // MapRoot를 따라 카메라 쪽으로/뒤로 계속 흘러가 버린다(2026-08-25 재발 확인, 원상복구).
            _currentBlockObject.transform.localPosition = new Vector3(0, 0, depth);
            _currentBlockObject.transform.localRotation = Quaternion.identity;

            var view = _currentBlockObject.GetComponent<BlockView>();
            if (view == null)
            {
                view = _currentBlockObject.AddComponent<BlockView>();
            }

            int baseHits = _mineralDataCache.TryGetValue(type, out var mineralData) ? mineralData.baseHits : 1;
            var model = new BlockModel(type, depth, baseHits);
            _currentBlockPresenter = new BlockPresenter(model, view);
            _currentBlockPresenter.OnBlockDestroyed += HandleBlockDestroyed;
        }

        // ── MineralData Loading (cached-UniTask + .Preserve() — see EnsureBossPatternsLoadedAsync for the same pattern) ──

        private UniTask EnsureMineralDataLoadedAsync()
        {
            if (!_mineralDataLoadStarted)
            {
                _mineralDataLoadStarted = true;
                _mineralDataLoadTask = LoadMineralDataAsync().Preserve();
            }
            return _mineralDataLoadTask;
        }

        private async UniTask LoadMineralDataAsync()
        {
            var minerals = await ResourceManager.Instance.LoadAllByLabelAsync<MineralData>(AddressableKeys.LabelMineralData);
            if (minerals != null)
                foreach (var m in minerals)
                    if (m != null) _mineralDataCache[m.blockType] = m;
        }

        private void ClearCurrentBlock()
        {
            if (_currentBlockPresenter != null)
            {
                _currentBlockPresenter.OnBlockDestroyed -= HandleBlockDestroyed;
                _currentBlockPresenter.Dispose();
                _currentBlockPresenter = null;
            }

            if (_currentBlockObject != null)
            {
                PoolSystem.Instance.Return(_currentBlockObject);
                _currentBlockObject = null;
            }
        }

        private void HandleBlockDestroyed(BlockPresenter presenter)
        {
            // 뷰가 ClearCurrentBlock()으로 즉시 반환/재활용되기 전에 픽업 연출 시작 위치를 미리 캐싱.
            Vector3 blockWorldPos = presenter.View.transform.position;

            // Resource drop logic — 확정 지급(드롭 실패 없음)
            RewardPlayerForBlock(presenter.Model.Type, blockWorldPos);

            // Play shatter effects
            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetBlockColor());
            EffectSystem.Instance.ShakeCamera(0.25f, 0.12f);

            // Clear and invoke events
            ClearCurrentBlock();
            OnBlockCleared?.Invoke();

            // Pickaxe durability loss (before HP death check in ProcessActionTurn)
            PickaxeDurabilityManager.Instance?.OnOreDestroyed(presenter.Model.Type);

            // Achievement event
            DeepEarth.Common.GameEvents.FireOreMined(presenter.Model.Type, 1);

            // Action turn: triggers status effect ticks (Burn, etc.)
            StatusEffectManager.Instance?.ProcessActionTurn();

            // Notify GameManager to proceed — OnBlockMined guards against GameOver state internally
            GameManager.Instance.OnBlockMined().Forget();
        }

        private void RewardPlayerForBlock(BlockType type, Vector3 worldPosition)
        {
            // Root 전용 부가 보너스(주 보상과 무관, 그대로 유지)
            if (type == BlockType.Root)
            {
                if (UnityEngine.Random.value < 0.4f)
                {
                    StatManager.Instance.Heal(2);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "+2 HP", Color.green);
                }

                if (UnityEngine.Random.value < 0.10f)
                {
                    bool added = InventoryManager.Instance.AddItem(AddressableKeys.ItemBurnCure, 1);
                    if (added)
                    {
                        string itemName = LocalizationManager.Instance.GetTranslation("item_burn_cure_name");
                        EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up * 1.2f, $"+1 {itemName}", new Color(0.4f, 0.9f, 1f));
                    }
                }
            }

            if (!_mineralDataCache.TryGetValue(type, out var mineralData) || mineralData == null || string.IsNullOrEmpty(mineralData.itemID))
                return;

            int depth = GameManager.Instance.CurrentDepth;
            var reward = RewardCalculator.Calculate(type, depth, mineralData, GameManager.Instance.DepthRewardTable);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Mining]\nMineral Destroyed\nType : {type}\nBase Reward : {reward.Base}\nDepth Bonus : +{reward.DepthBonus}\nFinal Reward : {type} x{reward.FinalAmount}\nInventory Updated");
#endif

            InventoryManager.Instance.AddItem(reward.ItemID, reward.FinalAmount);
            int totalGranted = reward.FinalAmount;

            if (reward.LuckyMineTriggered)
            {
                InventoryManager.Instance.AddItem(reward.ItemID, 1);
                totalGranted += 1;
            }
            if (reward.MineHealTriggered)
            {
                StatManager.Instance.Heal(1);
            }

            var targetRect = GameManager.Instance.GetInventoryButtonRect();
            if (targetRect != null)
            {
                EffectSystem.Instance.SpawnMiningRewardPickup(worldPosition, mineralData.itemID, totalGranted, targetRect);
            }
        }

        // 깊이는 "출현 확률"에 직접 관여하지 않는다 — MineralData.unlockDepth 이상인 광물들 중
        // 각자의 spawnWeight 가중치 랜덤으로 선택한다(깊이는 이후 RewardCalculator의 획득량 계산에만 관여).
        private BlockType ChooseBlockTypeByDepth(int depth)
        {
            var candidates = _mineralDataCache
                .Where(kv => depth >= kv.Value.unlockDepth)
                .Select(kv => kv.Value)
                .ToList();

            if (candidates.Count == 0) return BlockType.Dirt; // MineralData 로드 실패 시 안전 폴백

            float totalWeight = candidates.Sum(c => c.spawnWeight);
            float roll = UnityEngine.Random.value * totalWeight;
            float acc = 0f;
            foreach (var candidate in candidates)
            {
                acc += candidate.spawnWeight;
                if (roll < acc) return candidate.blockType;
            }
            return candidates[candidates.Count - 1].blockType;
        }

        private string GetAddressableKeyForBlock(BlockType type)
        {
            switch (type)
            {
                case BlockType.Dirt: return AddressableKeys.BlockDirt;
                case BlockType.Stone: return AddressableKeys.BlockStone;
                case BlockType.Root: return AddressableKeys.BlockRoot;
                case BlockType.Iron: return AddressableKeys.BlockIron;
                case BlockType.Silver: return AddressableKeys.BlockSilver;
                case BlockType.Gold: return AddressableKeys.BlockGold;
                case BlockType.Diamond: return AddressableKeys.BlockDiamond;
                default: return AddressableKeys.BlockDirt;
            }
        }
    }
}
