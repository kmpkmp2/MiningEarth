using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Map;

namespace DeepEarth.Combat
{
    public class CombatSystem : MonoBehaviour, DeepEarth.Battle.IMonsterSource
    {
        private static CombatSystem _instance;
        public static CombatSystem Instance => _instance;

        [SerializeField] private Transform spawnPoint;

        private readonly List<MonsterPresenter> _activePresenters    = new List<MonsterPresenter>();
        private readonly List<GameObject>        _activeMonsterObjects = new List<GameObject>();
        private readonly Dictionary<MonsterType, MonsterData>          _monsterDataCache = new Dictionary<MonsterType, MonsterData>();
        private readonly Dictionary<MonsterType, MonsterPatternData>   _patternDataCache = new Dictionary<MonsterType, MonsterPatternData>();
        private readonly Dictionary<MonsterPresenter, Action<MonsterPresenter>> _killHandlers = new Dictionary<MonsterPresenter, Action<MonsterPresenter>>();

        private UniTaskCompletionSource _combatTcs;
        private MonsterSpawnTable _spawnTable;
        private int _spawnCounter;
        private int _pendingSpawns;
        private UniTask _dataLoadTask;
        private bool _dataLoadStarted;

        private DeepEarth.UI.BattleView _battleView;
        private DeepEarth.Battle.BattlePresenter _battlePresenter;

        // 전투 중 새로 스폰되는 몬스터(예: 슬라임 분열) 알림. BattlePresenter가 구독해 턴 시스템에 편입시킨다.
        public event Action<MonsterPresenter> OnMonsterSpawned;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        // Battle UI(Addressables 인스턴스화 + BattlePresenter 구성) 완료를 외부에서 대기할 수 있도록 노출.
        // GameBootstrap의 Ready 집계가 이 태스크를 await한다.
        private UniTask _battleUIReadyTask = UniTask.CompletedTask;
        public UniTask BattleUIReadyTask => _battleUIReadyTask;

        public void Initialize(Transform monsterSpawnPoint, Transform canvasTransform = null)
        {
            spawnPoint = monsterSpawnPoint;
            EnsureDataLoadedAsync().Forget();
            if (canvasTransform != null) _battleUIReadyTask = SetupBattleUIAsync(canvasTransform);
        }

        private async UniTask SetupBattleUIAsync(Transform canvasTransform)
        {
            var go = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelBattle, canvasTransform);
            if (go == null)
            {
                Debug.LogWarning("[CombatSystem] UI_Panel_Battle not found — Addressables 등록 필요");
                return;
            }

            _battleView = go.GetComponent<DeepEarth.UI.BattleView>();
            if (_battleView == null) return;

            var intentData = await ResourceManager.Instance.LoadAssetAsync<MonsterIntentData>(AddressableKeys.MonsterIntentDataKey);
            await ShieldData.LoadAsync();
            _battlePresenter = new DeepEarth.Battle.BattlePresenter(_battleView, intentData, _battleView.IntentViewPrefab, _battleView.IntentLayer);
            _battleView.SetVisible(false);
        }

        // ── Public API ───────────────────────────────────────────────────

        public bool HasActiveMonsters => _activePresenters.Count > 0;

        public IReadOnlyList<MonsterPresenter> ActivePresenters => _activePresenters;

        // 엘리트/보스가 동일한 턴 루프(BattleView/TurnPresenter/IntentPresenter 포함)를 공유하기 위한 접근점.
        // 새 Singleton을 만들지 않고 기존 CombatSystem.Instance를 경유한다.
        public DeepEarth.Battle.BattlePresenter SharedBattlePresenter => _battlePresenter;

        // 폭탄류 소모 아이템 전용 진입점 — 현재 전투 중인 몬스터 전체에게 즉시 피해를 준다.
        public void ApplyItemDamageToActiveMonsters(int amount)
        {
            for (int i = _activePresenters.Count - 1; i >= 0; i--)
                _activePresenters[i]?.ApplyExternalDamage(amount);
        }

        public MonsterType PickMonsterForDepth(int depth)
        {
            return _spawnTable != null ? _spawnTable.PickMonster(depth) : MonsterType.CaveRat;
        }

        // 채굴 HUD 액션(예: 응급 수리)이 전투 중에는 비활성화되어야 할 때 참조하는 플래그.
        public bool IsCombatActive { get; private set; }

        public async UniTask StartCombatAsync(MonsterType type, int depth)
        {
            await EnsureDataLoadedAsync();

            ClearActiveMonsters();
            _combatTcs    = new UniTaskCompletionSource();
            _spawnCounter = 0;
            _pendingSpawns = 0;
            IsCombatActive = true;

            // Instant damage on encounter (CurseInstantDamageOnEncounter)
            int instantDmg = StatManager.Instance.GetEncounterInstantDamage();
            if (instantDmg > 0)
            {
                StatManager.Instance.TakeDamage(instantDmg);
                EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
                EffectSystem.Instance.ShakeCamera(0.25f, 0.1f);
                string msg = $"-{instantDmg} HP" + LocalizationManager.Instance.GetTranslation("curse_tag");
                EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, msg, Color.red);
            }

            if (!_monsterDataCache.TryGetValue(type, out var data))
            {
                Debug.LogError($"[CombatSystem] MonsterData not found for {type}. Completing combat.");
                _combatTcs.TrySetResult();
                goto PostCombat;
            }

            // Spawn based on MonsterData.spawnCount
            if (data.spawnCount <= 1)
            {
                await SpawnMonsterInstanceAsync(data, Vector3.zero, depth);
            }
            else
            {
                var spawnTasks = new List<UniTask>();
                for (int i = 0; i < data.spawnCount; i++)
                {
                    Vector3 offset = (data.spawnOffsets != null && i < data.spawnOffsets.Length)
                        ? data.spawnOffsets[i]
                        : new Vector3((i - data.spawnCount / 2f + 0.5f) * 1.0f, 0f, 0f);
                    spawnTasks.Add(SpawnMonsterInstanceAsync(data, offset, depth));
                }
                await UniTask.WhenAll(spawnTasks.ToArray());
            }

            if (_battlePresenter != null)
                await _battlePresenter.RunTurnLoopAsync(this, GetPatternData, GimmickWrapperFactory, allowRetreat: true);
            else
                await _combatTcs.Task; // BattleUI 미구성(Addressables 미등록 등) 시 안전한 폴백 — 실시간 자동 처치 없이 대기만 함

            PostCombat:
            IsCombatActive = false;
            ClearActiveMonsters();

            // 유물: 응급 붕대 — 전투 종료 후 HP 회복
            int postHeal = RelicManager.Instance?.GetPostCombatHealBonus() ?? 0;
            if (postHeal > 0)
            {
                StatManager.Instance.Heal(postHeal);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{postHeal} HP", Color.green);
            }

            // 유물: 자동 수리 키트 — 처치 시 내구도 회복
            int repair = RelicManager.Instance?.GetPickaxeRepairOnKill() ?? 0;
            if (repair > 0) PickaxeDurabilityManager.Instance?.RepairOnKill(repair);

            // 유물: 채굴 허가증 — 20% 확률 철 +1
            if (RelicManager.Instance?.CheckKillIronChance() ?? false)
            {
                InventoryManager.Instance.AddItem("Item_Iron", 1);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up * 0.5f, "+1 철", new Color(0.7f, 0.7f, 0.75f));
            }

            // Healing item drop (35%)
            if (UnityEngine.Random.value < 0.35f)
            {
                InventoryManager.Instance.AddItem("Item_Potion", 1);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "+1 Potion", Color.green);
            }

            // Burn Cure drop (5%)
            if (UnityEngine.Random.value < 0.05f)
            {
                bool added = InventoryManager.Instance.AddItem(AddressableKeys.ItemBurnCure, 1);
                if (added)
                {
                    string itemName = LocalizationManager.Instance.GetTranslation("item_burn_cure_name");
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up * 1.2f, $"+1 {itemName}", new Color(0.4f, 0.9f, 1f));
                }
            }

            Debug.Log("[Battle]\nCombat Finished");
        }

        // ── Internal Spawn ───────────────────────────────────────────────

        private async UniTask SpawnMonsterInstanceAsync(MonsterData data, Vector3 localOffset, int depth)
        {
            Vector3 worldPos = spawnPoint.position + spawnPoint.TransformDirection(localOffset);
            await SpawnMonsterAtWorldPosAsync(data, worldPos, depth);
        }

        private async UniTask SpawnMonsterAtWorldPosAsync(MonsterData data, Vector3 worldPos, int depth)
        {
            GameObject mGo = await PoolSystem.Instance.GetAsync(data.addressableKey, spawnPoint);
            if (mGo == null)
            {
                Debug.LogError($"[CombatSystem] Failed to spawn: {data.addressableKey}");
                CheckCombatEnd();
                return;
            }

            mGo.transform.position = worldPos;
            mGo.transform.rotation = spawnPoint.rotation;
            _activeMonsterObjects.Add(mGo);

            var view = mGo.GetComponent<MonsterView>();
            if (view == null) view = mGo.AddComponent<MonsterView>();

            int spawnIdx = _spawnCounter++;
            view.InitializeSpawn(spawnIdx);
            Debug.Log($"[Battle]\nSpawn Monster\nType : {data.monsterType}\nIndex : {spawnIdx}\nPosition : {worldPos.x:F2},{worldPos.y:F2},{worldPos.z:F2}");

            var model     = new MonsterModel(data, depth);
            var presenter = new MonsterPresenter(model, view);
            _activePresenters.Add(presenter);

            Action<MonsterPresenter> handler = p => HandleMonsterKilled(p, data, depth);
            _killHandlers[presenter] = handler;
            presenter.OnMonsterKilled += handler;

            OnMonsterSpawned?.Invoke(presenter);
        }

        // ── Kill Handler ─────────────────────────────────────────────────

        private void HandleMonsterKilled(MonsterPresenter presenter, MonsterData data, int depth)
        {
            if (_killHandlers.TryGetValue(presenter, out var handler))
            {
                presenter.OnMonsterKilled -= handler;
                _killHandlers.Remove(presenter);
            }

            Debug.Log($"[Battle]\nMonster Dead\nType : {data.monsterType}\nSpawnIndex : {presenter.View.SpawnIndex}");

            EffectSystem.Instance.SpawnHitParticles(presenter.View.transform.position, presenter.View.GetMonsterColor());
            EffectSystem.Instance.ShakeCamera(0.2f, 0.08f);

            GameObject go      = presenter.View.gameObject;
            Vector3    deathPos = go.transform.position;
            _activeMonsterObjects.Remove(go);
            PoolSystem.Instance.Return(go);
            _activePresenters.Remove(presenter);
            presenter.Dispose();

            StatusEffectManager.Instance?.ProcessActionTurn();
            GameEvents.FireMonsterKilled();

            // Slime split
            if (data.canSplit)
            {
                var splitData = GetMonsterData(data.splitIntoType);
                if (splitData != null)
                    SpawnSplitsAsync(splitData, deathPos, depth, data.splitCount).Forget();
            }

            // Skeleton death debuff
            if (data.hasDeathDebuff && data.deathDebuffEffect != null)
            {
                if (UnityEngine.Random.value < data.deathDebuffChance)
                {
                    StatusEffectManager.Instance?.ApplyMiningPowerDown();
                    Debug.Log("[Battle]\nSkeleton Death Debuff\nMiningPowerDown Applied");
                }
            }

            // Mimic death reward
            if (data.hasDeathReward && data.rewardTable != null)
            {
                var reward = data.rewardTable.PickReward(depth);
                if (reward != null) ApplyMimicReward(reward);
            }

            CheckCombatEnd();
        }

        private async UniTaskVoid SpawnSplitsAsync(MonsterData splitData, Vector3 basePos, int depth, int count)
        {
            _pendingSpawns++;
            try
            {
                var tasks = new List<UniTask>();
                for (int i = 0; i < count; i++)
                {
                    float xOff    = (i - count / 2f + 0.5f) * 1.5f;
                    Vector3 world = basePos + new Vector3(xOff, 0f, 0f);
                    tasks.Add(SpawnMonsterAtWorldPosAsync(splitData, world, depth));
                }
                await UniTask.WhenAll(tasks.ToArray());
                Debug.Log($"[Battle]\nSlime Split\n{splitData.monsterType} x{count} Spawned");
            }
            finally
            {
                _pendingSpawns--;
                CheckCombatEnd();
            }
        }

        private void CheckCombatEnd()
        {
            if (_activePresenters.Count == 0 && _pendingSpawns == 0)
                _combatTcs?.TrySetResult();
        }

        // ── Mimic Reward ─────────────────────────────────────────────────

        private void ApplyMimicReward(MimicRewardEntry reward)
        {
            switch (reward.rewardType)
            {
                case MimicRewardType.Iron:
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemIron, reward.amount);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{reward.amount} 철광석", new Color(0.6f, 0.6f, 0.7f));
                    break;
                case MimicRewardType.Silver:
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemSilver, reward.amount);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{reward.amount} 은", new Color(0.8f, 0.8f, 1f));
                    break;
                case MimicRewardType.Gold:
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemGold, reward.amount);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{reward.amount} 금", new Color(1f, 0.85f, 0.1f));
                    break;
                case MimicRewardType.Diamond:
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemDiamond, reward.amount);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{reward.amount} 다이아", new Color(0.2f, 0.9f, 1f));
                    break;
                case MimicRewardType.Potion:
                    InventoryManager.Instance?.AddItem(AddressableKeys.ItemPotion, reward.amount);
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, $"+{reward.amount} 포션", Color.green);
                    break;
                case MimicRewardType.MiningPowerUp:
                    StatusEffectManager.Instance?.ApplyMiningPowerUp();
                    EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "채굴력 증가!", new Color(0.4f, 1f, 0.4f));
                    break;
            }
            Debug.Log($"[Battle]\nMimic Reward\nType : {reward.rewardType}\nAmount : {reward.amount}");
        }

        // ── Data Loading ─────────────────────────────────────────────────

        private UniTask EnsureDataLoadedAsync()
        {
            if (!_dataLoadStarted)
            {
                _dataLoadStarted = true;
                _dataLoadTask = LoadDataAsync().Preserve();
            }
            return _dataLoadTask;
        }

        private async UniTask LoadDataAsync()
        {
            _spawnTable = await ResourceManager.Instance.LoadAssetAsync<MonsterSpawnTable>(AddressableKeys.MonsterSpawnTableKey);
            if (_spawnTable == null)
                Debug.LogWarning("[CombatSystem] MonsterSpawnTable not found in Addressables.");

            var allData = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterData>(AddressableKeys.LabelMonsterData);
            if (allData != null)
            {
                foreach (var d in allData)
                    if (d != null) _monsterDataCache[d.monsterType] = d;
            }

            var allPatterns = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterPatternData>(AddressableKeys.LabelMonsterPattern);
            if (allPatterns != null)
            {
                foreach (var p in allPatterns)
                    if (p != null) _patternDataCache[p.monsterType] = p;
            }

            Debug.Log($"[CombatSystem]\nData Loaded\nMonster Types : {_monsterDataCache.Count}\nSpawnTable : {(_spawnTable != null ? "OK" : "MISSING")}\nPatterns : {_patternDataCache.Count}");
        }

        public MonsterPatternData GetPatternData(MonsterType type)
        {
            return _patternDataCache.TryGetValue(type, out var data) ? data : null;
        }

        // 3단계 로스터 확장(2026-08): 고유 기믹을 가진 정규 몬스터 4종만 GimmickMonsterPresenter로
        // 감싸고, 나머지는 기존과 동일하게 기본 Battle.MonsterPresenter를 사용한다.
        private static DeepEarth.Battle.MonsterPresenter GimmickWrapperFactory(
            MonsterPresenter cp, DeepEarth.Battle.MonsterPatternModel pattern, DeepEarth.Battle.TurnModel turn)
        {
            switch (cp.Model.Type)
            {
                case MonsterType.OreBurrower:
                case MonsterType.MineMycelium:
                case MonsterType.AbyssMinerBee:
                case MonsterType.GoldVeinSpirit:
                    return new DeepEarth.Battle.GimmickMonsterPresenter(cp, pattern, turn);
                default:
                    return new DeepEarth.Battle.MonsterPresenter(cp, pattern, turn);
            }
        }

        public string GetMonsterNameLocKey(MonsterType type)
        {
            return _monsterDataCache.TryGetValue(type, out var data) ? data.nameLocKey : string.Empty;
        }

        // 조우 리빌 화면에 경고 서브타이틀로 노출할 사망 트리거 설명(예: 슬라임 분열 경고).
        // 설정 안 된 몬스터는 빈 문자열 반환 → 호출부에서 서브타이틀을 표시하지 않는다.
        public string GetMonsterDeathTriggerDescKey(MonsterType type)
        {
            return _monsterDataCache.TryGetValue(type, out var data) ? data.deathTriggerDescKey : string.Empty;
        }

        private MonsterData GetMonsterData(MonsterType type)
        {
            return _monsterDataCache.TryGetValue(type, out var data) ? data : null;
        }

        // ── Cleanup ──────────────────────────────────────────────────────

        // 후퇴(Retreat) 등 외부 트리거로 전투를 즉시 종료시킬 때 사용. 보상 없이 조우를 끝낸다 —
        // RunTurnLoopAsync의 while(monsterSource.HasActiveMonsters) 조건을 다음 체크에서 false로 만든다.
        public void ForceEndCombat()
        {
            ClearActiveMonsters();
        }

        private void ClearActiveMonsters()
        {
            foreach (var pres in _activePresenters)
            {
                if (_killHandlers.TryGetValue(pres, out var h))
                {
                    pres.OnMonsterKilled -= h;
                    _killHandlers.Remove(pres);
                }
                pres.Dispose();
            }
            _activePresenters.Clear();
            _killHandlers.Clear();

            foreach (var obj in _activeMonsterObjects)
                if (obj != null) PoolSystem.Instance.Return(obj);
            _activeMonsterObjects.Clear();

            _pendingSpawns = 0;
        }
    }
}
