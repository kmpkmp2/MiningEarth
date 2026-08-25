using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Combat;
using DeepEarth.Map;
using DeepEarth.UI;
using DeepEarth.Core;

namespace DeepEarth.Core
{
    /// <summary>
    /// Orchestrates the full Elite combat flow:
    ///   1. Pick an elite type from EliteSpawnTable based on depth.
    ///   2. Spawn the elite prefab.
    ///   3. Run EliteMonsterPresenter (skills run autonomously).
    ///   4. On defeat: distribute rewards from EliteRewardTable.
    ///   5. For CursedKnight/CursedPriest: show a relic-choice card (reuses BossRewardView).
    /// </summary>
    public class EliteCombatSystem : MonoBehaviour
    {
        private static EliteCombatSystem _instance;
        public static EliteCombatSystem Instance => _instance;

        [SerializeField] private Transform spawnPoint;

        private EliteSpawnTable _spawnTable;
        private readonly Dictionary<MonsterType, MonsterData> _eliteDataCache = new();
        private readonly Dictionary<MonsterType, MonsterPatternData> _patternDataCache = new();
        private UniTask _loadTask;
        private bool _loadStarted;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        public void Initialize(Transform monsterSpawnPoint)
        {
            spawnPoint = monsterSpawnPoint;
            EnsureDataLoadedAsync().Forget();
        }

        // ── Public Entry Point ───────────────────────────────────────────────────

        public async UniTask StartEliteCombatAsync(int depth)
        {
            await EnsureDataLoadedAsync();

            MonsterType eliteType = _spawnTable != null
                ? _spawnTable.PickElite(depth)
                : MonsterType.BigSlime;

            if (!_eliteDataCache.TryGetValue(eliteType, out var data))
            {
                Debug.LogError($"[EliteCombatSystem] MonsterData not found for {eliteType}. Skipping elite.");
                return;
            }

            string eliteName = LocalizationManager.Instance.GetTranslation(data.nameLocKey);

            // Visual alarm
            EffectSystem.Instance.FlashScreen(new Color(0.8f, 0.5f, 0f, 0.4f), 0.4f);
            EffectSystem.Instance.ShakeCamera(0.4f, 0.15f);
            EffectSystem.Instance.SpawnDamageText(
                Camera.main.transform.position + Camera.main.transform.forward * 1.5f,
                $"Elite! {eliteName}", new Color(1f, 0.7f, 0f));

            // 사망 시 트리거 경고(예: 빅슬라임 분열) — 일반 몬스터의 리빌 서브타이틀과 같은 목적,
            // 엘리트는 전용 리빌 팝업이 없어 같은 위치에 두 번째 플로팅 텍스트로 대체한다.
            if (!string.IsNullOrEmpty(data.deathTriggerDescKey))
            {
                string warning = LocalizationManager.Instance.GetTranslation(data.deathTriggerDescKey);
                if (!string.IsNullOrEmpty(warning))
                {
                    EffectSystem.Instance.SpawnDamageText(
                        Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.down * 0.4f,
                        warning, new Color(1f, 0.75f, 0.3f));
                }
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSpawn\nName : {eliteName}\nDepth : {depth}");
#endif

            // Encounter instant damage (from relics/curses)
            int instantDmg = StatManager.Instance.GetEncounterInstantDamage();
            if (instantDmg > 0)
            {
                StatManager.Instance.TakeDamage(instantDmg);
                EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.2f);
            }

            var source = new MonsterSource();
            await SpawnEliteBodyAsync(data, depth, source);

            // 일반 몬스터와 동일한 공유 턴 루프(BattleView/TurnPresenter/IntentPresenter 포함)를 사용한다.
            await CombatSystem.Instance.SharedBattlePresenter.RunTurnLoopAsync(source, GetPatternData, EliteWrapperFactory);

            EffectSystem.Instance.FlashScreen(new Color(0.8f, 0.6f, 0f, 0.3f), 0.3f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            // Give rewards
            if (data.eliteRewardTable != null)
                await GiveRewardsAsync(data.eliteRewardTable, depth);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nDefeated\nReward : {(data.eliteRewardTable != null ? data.eliteRewardTable.name : "none")}");
#endif
        }

        // ── Spawn ────────────────────────────────────────────────────────────────

        private async UniTask SpawnEliteBodyAsync(MonsterData data, int depth, MonsterSource source)
        {
            GameObject eliteGo = await PoolSystem.Instance.GetAsync(data.addressableKey, spawnPoint);
            if (eliteGo == null)
            {
                eliteGo = CreateFallbackElite(data.monsterType);
                eliteGo.transform.position = spawnPoint.position;
            }
            else
            {
                eliteGo.transform.position = spawnPoint.position;
                eliteGo.transform.rotation = spawnPoint.rotation;
            }

            var view = eliteGo.GetComponent<MonsterView>();
            if (view == null) view = eliteGo.AddComponent<MonsterView>();
            view.InitializeSpawn(0);

            if (!string.IsNullOrEmpty(data.spriteOverrideKey))
                ApplySpriteOverrideAsync(view, data.spriteOverrideKey).Forget();

            var model     = new EliteMonsterModel(data, depth);
            var presenter = new EliteMonsterPresenter(model, view);

            presenter.OnMonsterKilled += _ =>
            {
                PoolSystem.Instance.Return(eliteGo);
                presenter.Dispose();

                // BigSlime: 사망 시 일반 슬라임 3마리로 분열 — 기존 CombatSystem.HandleMonsterKilled의
                // 슬라임 분열 패턴과 동일하게 사망 이벤트에서 처리한다.
                if (data.eliteSkillType == EliteSkillType.SplitOnDeath)
                {
                    // 일반 슬라임 분열과 동일하게, 방금 태어난 미니 슬라임이 같은 라운드에
                    // 바로 공격하지 않도록 몬스터 턴을 건너뛰고 플레이어에게 턴을 돌려준다.
                    source.NotifySplit();
                    SpawnSplitSlimesAsync(depth, source).Forget();
                }
            };

            source.Add(presenter);
        }

        // 공유 프리팹(예: Combat_Monster_Slime)을 쓰는 엘리트가 전용 스프라이트로 표시되도록
        // 스폰 직후 비동기로 덮어쓴다. 실패해도 프리팹 기본 스프라이트로 그대로 보이므로 안전.
        private async UniTaskVoid ApplySpriteOverrideAsync(MonsterView view, string spriteKey)
        {
            Sprite sprite;
            try { sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(spriteKey); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Elite]\nSprite override load failed: {spriteKey}\n{e.Message}");
                return;
            }
            if (view != null) view.SetSprite(sprite);
        }

        // ── Rewards ──────────────────────────────────────────────────────────────

        private async UniTask GiveRewardsAsync(EliteRewardTable table, int depth)
        {
            // 유물: 심연의 핵 — 엘리트 보상 배율
            float eliteMultiplier = RelicManager.Instance?.GetEliteRewardMultiplier() ?? 1f;
            int mult = Mathf.Max(1, Mathf.RoundToInt(eliteMultiplier));

            // Will reward
            if (table.willReward > 0)
                GameManager.Instance?.AddRunWill(table.willReward * mult);

            // Auto drops
            foreach (var drop in table.autoDrops)
                GiveItem(drop.itemId, drop.amount * mult);

            // Random drop (pick one)
            if (table.hasRandomDrop && table.randomDropOptions.Count > 0)
            {
                var pick = table.randomDropOptions[UnityEngine.Random.Range(0, table.randomDropOptions.Count)];
                GiveItem(pick.itemId, pick.amount * mult);
            }

            // Depth-scaled drop (MerchantMimic)
            if (table.useDepthScaledDrops)
            {
                var entry = table.PickDepthDrop(depth);
                if (entry != null) GiveItem(entry.itemId, entry.amount * mult);
            }

            // Relic choice
            if (table.giveRelicChoice)
                await ShowRelicChoiceAsync(table.relicChoiceCount);

            // 유물: 엘리트 헌터 훈장 — 엘리트 처치 시 유물 추가 선택
            if (RelicManager.Instance?.HasEliteKillRelicReward() ?? false)
                await ShowRelicChoiceAsync(1);
        }

        private void GiveItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
            InventoryManager.Instance.AddItem(itemId, amount);

            string label = LocalizationManager.Instance.GetTranslation($"item_{itemId.ToLower()}_name");
            if (string.IsNullOrEmpty(label)) label = itemId;
            EffectSystem.Instance.SpawnDamageText(
                spawnPoint.position + Vector3.up,
                $"+{amount} {label}", Color.green);
        }

        // ── Relic Choice (reuses BossRewardView) ─────────────────────────────────
        // public — CombatSystem(일반 몬스터 전투)도 동일한 유물 선택 UI를 재사용한다.

        public async UniTask ShowRelicChoiceAsync(int count)
        {
            var relics = RelicManager.Instance?.GetRandomRelicChoices(count);
            if (relics == null || relics.Count == 0) return;

            var rewardView = BossManager.Instance?.RewardView;
            if (rewardView == null)
            {
                // Fallback: auto-pick the first relic
                if (relics.Count > 0) RelicManager.Instance.AddRelic(relics[0]);
                return;
            }

            // Build BossRewardChoice cards from relic data
            var choices = new List<BossRewardChoice>();
            foreach (var relic in relics)
                choices.Add(new BossRewardChoice(relic.nameLocKey, relic.descLocKey));

            rewardView.SetVisible(true);
            rewardView.PopulateRewards(choices);

            int chosen = -1;
            var selectionTcs = new UniTaskCompletionSource();
            Action<int> handler = idx => { chosen = idx; selectionTcs.TrySetResult(); };
            rewardView.OnRewardSelected += handler;

            await selectionTcs.Task;

            rewardView.OnRewardSelected -= handler;
            rewardView.SetVisible(false);

            if (chosen >= 0 && chosen < relics.Count)
            {
                RelicManager.Instance.AddRelic(relics[chosen]);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[Elite]\nRelic Chosen\n{relics[chosen].relicID}");
#endif
            }
        }

        // ── BigSlime split ────────────────────────────────────────────────────────

        private async UniTaskVoid SpawnSplitSlimesAsync(int depth, MonsterSource source)
        {
            if (!_eliteDataCache.TryGetValue(MonsterType.Slime, out var slimeData))
            {
                Debug.LogWarning("[EliteCombatSystem] Slime MonsterData not found for BigSlime split.");
                return;
            }

            const int splitCount = 3;
            var tasks = new List<UniTask>();
            for (int i = 0; i < splitCount; i++)
            {
                float xOff = (i - splitCount / 2f + 0.5f) * 1.5f;
                Vector3 pos = spawnPoint.position + new Vector3(xOff, 0f, 0f);
                tasks.Add(SpawnTemporarySlimeAsync(slimeData, pos, depth, source));
            }
            await UniTask.WhenAll(tasks);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Elite]\nSkill Activated\nBigSlime Split : {splitCount} Slimes Spawned");
#endif
        }

        private async UniTask SpawnTemporarySlimeAsync(MonsterData data, Vector3 worldPos, int depth, MonsterSource source)
        {
            GameObject go = await PoolSystem.Instance.GetAsync(data.addressableKey, spawnPoint);
            if (go == null) return;

            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * 0.7f; // 70% scale per spec

            var view = go.GetComponent<MonsterView>();
            if (view == null) view = go.AddComponent<MonsterView>();
            view.InitializeSpawn(0);

            // Slime has 50% HP
            int hp = Mathf.Max(1, (data.baseMaxHP + data.hpPerDifficultyLevel) / 2);
            var model     = new MonsterModel(data.monsterType, hp, data.baseDamage);
            var presenter = new MonsterPresenter(model, view);

            presenter.OnMonsterKilled += _ =>
            {
                PoolSystem.Instance.Return(go);
                presenter.Dispose();
            };

            source.Add(presenter);
        }

        // ── Data Loading ─────────────────────────────────────────────────────────

        private UniTask EnsureDataLoadedAsync()
        {
            if (!_loadStarted)
            {
                _loadStarted = true;
                _loadTask = LoadDataAsync().Preserve();
            }
            return _loadTask;
        }

        private async UniTask LoadDataAsync()
        {
            _spawnTable = await ResourceManager.Instance.LoadAssetAsync<EliteSpawnTable>(
                AddressableKeys.EliteSpawnTableKey);
            if (_spawnTable == null)
                Debug.LogWarning("[EliteCombatSystem] EliteSpawnTable not found in Addressables.");

            // Load all MonsterData tagged with EliteData label
            var eliteDataList = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterData>(
                AddressableKeys.LabelEliteData);
            if (eliteDataList != null)
                foreach (var d in eliteDataList)
                    if (d != null) _eliteDataCache[d.monsterType] = d;

            // Also load Slime from standard MonsterData label (for BigSlime split)
            var allMonsterData = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterData>(
                AddressableKeys.LabelMonsterData);
            if (allMonsterData != null)
                foreach (var d in allMonsterData)
                    if (d != null && !_eliteDataCache.ContainsKey(d.monsterType))
                        _eliteDataCache[d.monsterType] = d;

            // Elite 패턴(6종) 로드
            var elitePatterns = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterPatternData>(
                AddressableKeys.LabelElitePattern);
            if (elitePatterns != null)
                foreach (var p in elitePatterns)
                    if (p != null) _patternDataCache[p.monsterType] = p;

            // 일반 몬스터 패턴도 로드(BigSlime 분열로 생기는 일반 Slime의 패턴 조회용)
            var regularPatterns = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterPatternData>(
                AddressableKeys.LabelMonsterPattern);
            if (regularPatterns != null)
                foreach (var p in regularPatterns)
                    if (p != null && !_patternDataCache.ContainsKey(p.monsterType))
                        _patternDataCache[p.monsterType] = p;

            Debug.Log($"[EliteCombatSystem]\nData Loaded\nElite Types : {_eliteDataCache.Count}\nSpawnTable : {(_spawnTable != null ? "OK" : "MISSING")}\nPatterns : {_patternDataCache.Count}");
        }

        public MonsterPatternData GetPatternData(MonsterType type)
        {
            return _patternDataCache.TryGetValue(type, out var data) ? data : null;
        }

        // 엘리트 본체는 Battle.EliteMonsterPresenter(스킬 훅 포함)로, 분열된 일반 Slime 등은
        // 기본 Battle.MonsterPresenter로 감싼다.
        private static DeepEarth.Battle.MonsterPresenter EliteWrapperFactory(
            MonsterPresenter cp, DeepEarth.Battle.MonsterPatternModel pattern, DeepEarth.Battle.TurnModel turn)
        {
            if (cp is EliteMonsterPresenter elite)
                return new DeepEarth.Battle.EliteMonsterPresenter(elite, pattern, turn);
            return new DeepEarth.Battle.MonsterPresenter(cp, pattern, turn);
        }

        // ── Fallback ─────────────────────────────────────────────────────────────

        private GameObject CreateFallbackElite(MonsterType type)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"FallbackElite_{type}";

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1f, 0.5f, 0f);
                renderer.sharedMaterial = mat;
            }
            return go;
        }
    }
}
