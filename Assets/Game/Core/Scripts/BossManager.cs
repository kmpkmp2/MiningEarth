using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Combat;
using DeepEarth.UI;
using DeepEarth.Map;
using DeepEarth.Mining;

namespace DeepEarth.Core
{
    public class BossManager : MonoBehaviour
    {
        private static BossManager _instance;
        public static BossManager Instance => _instance;

        private GameObject _bossRoomUIObject;
        private GameObject _bossRewardUIObject;

        private BossView _bossView;
        private BossRewardView _bossRewardView;

        public DeepEarth.UI.BossRewardView RewardView => _bossRewardView;

        // GameManager의 런 포기(Abandon) 리플렉션 정리 코드가 필드명 "_bossPresenter"로 접근하므로
        // 타입만 IDisposable로 바뀌었을 뿐 필드명은 그대로 유지한다(하위 호환).
        private IDisposable _bossPresenter;
        private BossRewardPresenter _bossRewardPresenter;

        private GameObject _spawnedBossObject;

        private readonly Dictionary<MonsterType, MonsterPatternData> _bossPatternCache = new();
        private UniTask _patternLoadTask;
        private bool _patternLoadStarted;

        // 소환형 보스(전금속 거인)의 광석 코어 정리 + 소환 미니언 정리를 함께 처리하는 처분 핸들.
        private sealed class BossEncounterHandle : IDisposable
        {
            private readonly Combat.MonsterSource _source;
            private readonly Combat.MonsterPresenter _bodyPresenter;

            public BossEncounterHandle(Combat.MonsterSource source, Combat.MonsterPresenter bodyPresenter)
            {
                _source = source;
                _bodyPresenter = bodyPresenter;
            }

            public void Dispose()
            {
                foreach (var p in new List<Combat.MonsterPresenter>(_source.ActivePresenters))
                {
                    if (ReferenceEquals(p, _bodyPresenter)) { p.Dispose(); continue; }
                    if (p.View != null)
                    {
                        if (p.Model.Type == MonsterType.BossCore) UnityEngine.Object.Destroy(p.View.gameObject);
                        else PoolSystem.Instance?.Return(p.View.gameObject);
                    }
                    p.Dispose();
                }
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async UniTask InitializeUIAsync(Transform canvasTransform)
        {
            try
            {
                // Instantiate UI Views from Addressables
                // 각 패널은 로드 직후 즉시 비활성화한다 — 다음 패널의 비동기 로드가 진행되는 동안
                // 화면에 노출되어 깜빡이는 것을 방지하기 위함(GameManager.InitializeUIAsync와 동일 패턴).
                _bossRoomUIObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelBossRoom, canvasTransform);
                _bossRoomUIObject?.SetActive(false);
                _bossRewardUIObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelBossReward, canvasTransform);
                _bossRewardUIObject?.SetActive(false);

                if (_bossRoomUIObject != null)
                {
                    _bossView = _bossRoomUIObject.GetComponent<BossView>();
                    _bossRoomUIObject.SetActive(false);
                }

                if (_bossRewardUIObject != null)
                {
                    _bossRewardView = _bossRewardUIObject.GetComponent<BossRewardView>();
                    _bossRewardUIObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"BossManager: Error initializing UI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async UniTask StartBossSequenceAsync(int depth)
        {
            // Transition State to BossCombat
            var gameMgr = GameManager.Instance;
            typeof(GameManager).GetProperty("CurrentState").SetValue(gameMgr, GameState.BossCombat);

            // Notify UI updates
            var onGameDataChanged = (Action)typeof(GameManager).GetField("OnGameDataChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gameMgr);
            onGameDataChanged?.Invoke();

            // Determine Boss type by MapIndex (0-based map progression counter).
            // Route Map mode increments MapIndex after each boss defeat, so each 50-floor
            // segment gets a unique boss regardless of CurrentDepth node-counter value.
            int mapIndex = SaveManager.CurrentData?.MapSaveData?.MapIndex ?? 0;
            BossID bossId = mapIndex switch
            {
                0 => BossID.StoneGolem,
                1 => BossID.MotherCaveSpider,
                2 => BossID.SkeletonWarlord,
                _ => BossID.AllMetalColossus
            };

            // Addressable Key
            string bossKey = bossId switch
            {
                BossID.StoneGolem       => AddressableKeys.MonsterBossStoneGolem,
                BossID.MotherCaveSpider => AddressableKeys.MonsterBossMotherSpider,
                BossID.SkeletonWarlord  => AddressableKeys.MonsterBossSkeletonWarlord,
                BossID.AllMetalColossus => AddressableKeys.MonsterBossAllMetalColossus,
                _                       => AddressableKeys.MonsterBossStoneGolem
            };

            // Retrieve Spawn Point via reflection
            Transform spawnPoint = null;
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap != null)
            {
                spawnPoint = (Transform)typeof(GameBootstrap).GetField("monsterSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bootstrap);
            }

            if (spawnPoint == null)
            {
                Debug.LogError("BossManager: Could not find monsterSpawnPoint from GameBootstrap!");
                return;
            }

            // Spawn Boss 3D Object
            _spawnedBossObject = await PoolSystem.Instance.GetAsync(bossKey, spawnPoint);
            if (_spawnedBossObject == null)
            {
                Debug.Log($"[BOSS]\nAddressable Boss Asset Not Found\n\nFallback Boss Generated\n\nKey : {bossKey}");

                _spawnedBossObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _spawnedBossObject.name = "FallbackBoss";
                _spawnedBossObject.transform.position = spawnPoint.position;
                _spawnedBossObject.transform.rotation = spawnPoint.rotation;
                _spawnedBossObject.transform.localScale = Vector3.one;

                var renderer = _spawnedBossObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    var mat = new Material(shader);
                    mat.color = Color.red;
                    renderer.sharedMaterial = mat;
                }
            }
            else
            {
                _spawnedBossObject.transform.position = spawnPoint.position;
                _spawnedBossObject.transform.rotation = spawnPoint.rotation;

                string bossDisplayName = bossId switch
                {
                    BossID.StoneGolem       => "Stone Golem",
                    BossID.MotherCaveSpider => "Mother Cave Spider",
                    BossID.SkeletonWarlord  => "Skeleton Warlord",
                    BossID.AllMetalColossus => "All Metal Colossus",
                    _                       => bossId.ToString()
                };
                Debug.Log($"[Boss]\nSpawned\nBoss : {bossDisplayName}\nScale : (1,1,1)");
            }

            var monsterView = _spawnedBossObject.GetComponent<MonsterView>();
            if (monsterView == null)
            {
                monsterView = _spawnedBossObject.AddComponent<MonsterView>();
            }
            monsterView.InitializeSpawn(0);

            // Screen visual alarm
            EffectSystem.Instance.FlashScreen(new Color(1f, 0f, 0f, 0.4f), 0.4f);
            EffectSystem.Instance.ShakeCamera(0.4f, 0.15f);
            EffectSystem.Instance.SpawnDamageText(Camera.main.transform.position + Camera.main.transform.forward * 1.5f, LocalizationManager.Instance.GetTranslation("combat_monster_encounter"), Color.red);

            // ── 턴제 전투: 보스 본체 + (전금속 거인의 경우) 광석 코어 4개를 Combat.MonsterModel/Presenter로 구성 ──
            await EnsureBossPatternsLoadedAsync();

            var bossData = new BossData(bossId, depth); // 스탯 공식 소스로만 유지
            MonsterType bodyType = bossId switch
            {
                BossID.StoneGolem       => MonsterType.StoneGolemBoss,
                BossID.MotherCaveSpider => MonsterType.MotherCaveSpiderBoss,
                BossID.SkeletonWarlord  => MonsterType.SkeletonWarlordBoss,
                BossID.AllMetalColossus => MonsterType.AllMetalColossusBoss,
                _                       => MonsterType.StoneGolemBoss
            };

            var bodyModel = new Combat.MonsterModel(bodyType, bossData.MaxHP, bossData.Damage);
            var bodyPresenter = new Combat.MonsterPresenter(bodyModel, monsterView);
            bodyModel.OnHPChanged += (cur, max) => _bossView?.SetHP(cur, max);

            _bossView?.Initialize();
            _bossView?.SetBossName(bossData.BossNameKey);
            _bossView?.SetHP(bodyModel.CurrentHP, bodyModel.MaxHP);
            _bossView?.SetVisible(true);

            var source = new Combat.MonsterSource();
            var coreOreMap = new Dictionary<Combat.MonsterPresenter, BlockType>();
            DeepEarth.Battle.MultiPartBossCoordinator coordinator = null;

            if (bossId == BossID.AllMetalColossus)
            {
                coordinator = new DeepEarth.Battle.MultiPartBossCoordinator();
                coordinator.RegisterBody(monsterView.transform);
                SpawnOreCores(bossData, spawnPoint, source, coordinator, coreOreMap);
                EffectSystem.Instance.SpawnDamageText(monsterView.transform.position + Vector3.up * 0.8f, "DESTROY THE CORES!", new Color(1f, 0.8f, 0f));
            }

            source.Add(bodyPresenter);

            // 성취/보상 트리거는 "본체 사망" 시점(원본 OnBossDefeated와 동일 타이밍)에 발동.
            bodyPresenter.OnMonsterKilled += _ => DeepEarth.Common.GameEvents.FireBossKilled(bossId.ToString());

            var thisBossId = bossId; // 클로저 캡처용
            DeepEarth.Battle.MonsterPresenter BossWrapperFactory(Combat.MonsterPresenter cp, DeepEarth.Battle.MonsterPatternModel pattern, DeepEarth.Battle.TurnModel turn)
            {
                if (ReferenceEquals(cp, bodyPresenter))
                {
                    var role = thisBossId switch
                    {
                        BossID.MotherCaveSpider => DeepEarth.Battle.BossMonsterPresenter.Role.MotherCaveSpider,
                        BossID.SkeletonWarlord  => DeepEarth.Battle.BossMonsterPresenter.Role.SkeletonWarlord,
                        BossID.AllMetalColossus => DeepEarth.Battle.BossMonsterPresenter.Role.ColossusBody,
                        _                       => DeepEarth.Battle.BossMonsterPresenter.Role.StoneGolem
                    };
                    return new DeepEarth.Battle.BossMonsterPresenter(cp, pattern, turn, role, source, spawnPoint, coordinator);
                }
                if (cp.Model.Type == MonsterType.BossCore)
                {
                    var ore = coreOreMap.TryGetValue(cp, out var o) ? o : BlockType.Iron;
                    return new DeepEarth.Battle.BossMonsterPresenter(cp, pattern, turn, DeepEarth.Battle.BossMonsterPresenter.Role.ColossusCore, source, spawnPoint, coordinator, ore);
                }
                // 전투 중 소환되는 아기 거미/해골 미니언 — 기본 래퍼로 충분(고유 스킬 없음).
                return new DeepEarth.Battle.MonsterPresenter(cp, pattern, turn);
            }

            _bossPresenter = new BossEncounterHandle(source, bodyPresenter);

            // 일반 몬스터/엘리트와 동일한 공유 턴 루프 사용.
            await CombatSystem.Instance.SharedBattlePresenter.RunTurnLoopAsync(source, GetBossPatternData, BossWrapperFactory);

            // Clear Boss 3D object
            if (_spawnedBossObject != null)
            {
                PoolSystem.Instance.Return(_spawnedBossObject);
                _spawnedBossObject = null;
            }

            _bossPresenter?.Dispose();
            _bossPresenter = null;

            _bossView?.SetVisible(false);

            // Visual feedback on defeat
            EffectSystem.Instance.FlashScreen(new Color(0f, 0.8f, 1f, 0.3f), 0.3f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            // 유물: 시간의 모래시계 — 보스 처치 시 HP 전부 회복
            if (RelicManager.Instance?.HasBossKillFullHeal() ?? false)
            {
                StatManager.Instance.Heal(StatManager.Instance.GetMaxHP());
                EffectSystem.Instance.SpawnDamageText(
                    gameMgr.transform.position + Vector3.up, "HP 전부 회복!", Color.green);
            }
            else
            {
                // 유물: 광산 도시락 — 보스 처치 후 HP +5
                int bossHeal = RelicManager.Instance?.GetPostBossHealBonus() ?? 0;
                if (bossHeal > 0)
                {
                    StatManager.Instance.Heal(bossHeal);
                    EffectSystem.Instance.SpawnDamageText(
                        gameMgr.transform.position + Vector3.up, $"+{bossHeal} HP", Color.green);
                }
            }

            // Stone Golem: guaranteed Iron ×3 drop
            if (bossId == BossID.StoneGolem)
            {
                InventoryManager.Instance.AddItem("Item_Iron", 3);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up * 0.5f, "Iron ×3", new Color(0.7f, 0.7f, 0.75f));
                Debug.Log("[Boss]\nStone Golem Drop\nIron : 3");
            }

            // Open Boss Reward Selection UI
            typeof(GameManager).GetProperty("CurrentState").SetValue(gameMgr, GameState.BossReward);
            onGameDataChanged?.Invoke();

            var rewardCompletedTcs = new UniTaskCompletionSource();
            _bossRewardPresenter = new BossRewardPresenter(_bossRewardView, () => rewardCompletedTcs.TrySetResult());

            await rewardCompletedTcs.Task;

            _bossRewardPresenter = null;

            // Route Map / 기존 선형 모드 분기 진입점
            gameMgr.OnBossSequenceComplete();
        }

        // ── All Metal Colossus: 광석 코어 4개를 Combat.MonsterPresenter로 스폰 ─────────
        // 기존 BossPresenter.SpawnOreCores와 동일한 위치/색상 배치를 유지하되, BossCoreView(OnMouseDown)
        // 대신 MonsterView(OnTouched, Target Select 대응)를 사용한다.
        private void SpawnOreCores(BossData bossData, Transform spawnPoint, Combat.MonsterSource source,
            DeepEarth.Battle.MultiPartBossCoordinator coordinator, Dictionary<Combat.MonsterPresenter, BlockType> coreOreMap)
        {
            var oreTypes = new BlockType[] { BlockType.Iron, BlockType.Silver, BlockType.Gold, BlockType.Diamond };
            for (int i = 3; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (oreTypes[i], oreTypes[j]) = (oreTypes[j], oreTypes[i]);
            }

            Vector3[] offsets =
            {
                new Vector3(-1.2f, 0f,  1.0f),
                new Vector3( 1.2f, 0f,  1.0f),
                new Vector3(-1.2f, 0f, -0.3f),
                new Vector3( 1.2f, 0f, -0.3f)
            };

            int coreHp = 20 + Mathf.Max(0, (bossData.SpawnDepth / 50) - 4) * 10;

            for (int i = 0; i < 4; i++)
            {
                var coreGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                coreGo.name = $"BossCore_{oreTypes[i]}";
                coreGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                coreGo.transform.position = spawnPoint.position + spawnPoint.TransformDirection(offsets[i]);
                coreGo.transform.rotation = spawnPoint.rotation;

                var renderer = coreGo.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    var mat = new Material(shader);
                    mat.color = oreTypes[i] switch
                    {
                        BlockType.Iron    => new Color(0.55f, 0.55f, 0.6f),
                        BlockType.Silver  => new Color(0.8f,  0.8f,  0.9f),
                        BlockType.Gold    => new Color(1.0f,  0.84f, 0.0f),
                        BlockType.Diamond => new Color(0.0f,  0.9f,  1.0f),
                        _                 => Color.white
                    };
                    renderer.sharedMaterial = mat;
                }

                var coreView = coreGo.AddComponent<MonsterView>();
                coreView.InitializeSpawn(i);

                var coreModel = new Combat.MonsterModel(MonsterType.BossCore, maxHp: coreHp, damage: 0);
                var corePresenter = new Combat.MonsterPresenter(coreModel, coreView);
                coreOreMap[corePresenter] = oreTypes[i];
                coordinator.RegisterPart();

                // 코어는 Addressables/Pool 경유가 아닌 원시 primitive라 파괴 시 직접 Destroy한다.
                corePresenter.OnMonsterKilled += _ =>
                {
                    if (coreGo != null) UnityEngine.Object.Destroy(coreGo);
                    corePresenter.Dispose();
                };

                source.Add(corePresenter);

                Debug.Log($"[Boss]\nOre Core Spawned\nOre : {oreTypes[i]}\nHP : {coreHp}");
            }
        }

        // ── Pattern Data Loading ─────────────────────────────────────────────
        private UniTask EnsureBossPatternsLoadedAsync()
        {
            if (!_patternLoadStarted)
            {
                _patternLoadStarted = true;
                _patternLoadTask = LoadBossPatternsAsync().Preserve();
            }
            return _patternLoadTask;
        }

        private async UniTask LoadBossPatternsAsync()
        {
            var bossPatterns = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterPatternData>(AddressableKeys.LabelBossPattern);
            if (bossPatterns != null)
                foreach (var p in bossPatterns)
                    if (p != null) _bossPatternCache[p.monsterType] = p;

            // 소환 미니언(아기 거미/해골)은 일반 몬스터 패턴을 그대로 재사용.
            var regularPatterns = await ResourceManager.Instance.LoadAllByLabelAsync<MonsterPatternData>(AddressableKeys.LabelMonsterPattern);
            if (regularPatterns != null)
                foreach (var p in regularPatterns)
                    if (p != null && !_bossPatternCache.ContainsKey(p.monsterType)) _bossPatternCache[p.monsterType] = p;
        }

        private MonsterPatternData GetBossPatternData(MonsterType type) =>
            _bossPatternCache.TryGetValue(type, out var data) ? data : null;

        private void OnDestroy()
        {
            _bossPresenter?.Dispose();
            _bossRewardPresenter?.Dispose();
        }
    }
}
