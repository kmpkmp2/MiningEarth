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

        // 블록이 항상 스폰되어야 할 고정 월드 좌표(런 시작 시 1회 캡처). depth 기반 보정을 시도했었으나
        // (아래 SpawnNextBlockAsync 주석 참고) GameManager.CurrentDepth는 채광이 아닌 노드(몬스터/이벤트 등)
        // 방문 시에도 증가하는 반면 MapRoot는 실제로 채광이 일어날 때만 밀려나서, 두 카운터가 어긋나며
        // 채광 노드가 아닌 노드를 지날 때마다 블록이 카메라에서 점점 멀어지는 버그가 있었다(2026-08-27).
        // 이 앵커 방식은 어떤 카운터도 추적하지 않고 항상 같은 월드 좌표를 직접 타겟하므로 그 문제와 무관하다.
        private Vector3 _blockSpawnAnchorWorldPos;
        private bool _anchorCaptured;

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

        // GameManager.RunStart()가 TunnelGenerator.ResetGenerator()(MapRoot를 월드 원점으로 리셋)와
        // 함께 호출한다 — 그 시점의 spawnPoint 월드 위치를 이번 런 내내 블록이 스폰될 고정 지점으로 고정한다.
        public void ResetSpawnAnchor()
        {
            if (spawnPoint == null) return;
            _blockSpawnAnchorWorldPos = spawnPoint.position;
            _anchorCaptured = true;
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

            // BlockSpawnRoot는 MapRoot의 자식이라 채광마다 MapView.MoveMapBack()이 MapRoot를 밀어내는 만큼
            // 같이 움직인다. depth를 로컬 Z 보정값으로 쓰는 방식(과거 시도)은 GameManager.CurrentDepth와
            // MapRoot의 실제 이동 횟수가 항상 1:1이라는 전제가 있어야 하는데, 채광 노드가 아닌 노드(몬스터/
            // 이벤트/상인/휴식 등)에서도 CurrentDepth는 증가하지만 MapRoot는 그때 움직이지 않아 전제가
            // 깨진다 — 그래서 계속 조금씩 어긋나 카메라에서 멀어졌다. 대신 런 시작 시 1회 캡처해둔
            // 고정 월드 좌표(_blockSpawnAnchorWorldPos)를 그대로 타겟한다 — 어떤 카운터도 참조하지 않으므로
            // MapRoot가 실제로 몇 번 움직였든 항상 정확하다.
            if (!_anchorCaptured) ResetSpawnAnchor();
            _currentBlockObject.transform.position = _blockSpawnAnchorWorldPos;
            _currentBlockObject.transform.rotation = spawnPoint.rotation;

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
