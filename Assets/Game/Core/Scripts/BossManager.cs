using System;
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

        private BossPresenter _bossPresenter;
        private BossRewardPresenter _bossRewardPresenter;

        private GameObject _spawnedBossObject;

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
                _bossRoomUIObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelBossRoom, canvasTransform);
                _bossRewardUIObject = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.UIPanelBossReward, canvasTransform);

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

            // Determine Boss type
            int tier = depth / 50;
            int cycleIndex = (tier - 1) % 5;
            BossID bossId = (BossID)cycleIndex;

            // Addressable Key
            string bossKey = bossId switch
            {
                BossID.CaveRat => AddressableKeys.MonsterBossRat,
                BossID.QueenSpider => AddressableKeys.MonsterBossSpider,
                BossID.RockGolem => AddressableKeys.MonsterBossGolem,
                BossID.LavaWorm => AddressableKeys.MonsterBossWorm,
                BossID.CrystalTitan => AddressableKeys.MonsterBossTitan,
                _ => AddressableKeys.MonsterBossRat
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

                // Scale 안전장치: 프리팹 또는 풀 재사용으로 인한 비정상 Scale 보정
                Vector3 scale = _spawnedBossObject.transform.localScale;
                if (scale != Vector3.one)
                {
                    Debug.Log($"[Boss]\nInvalid Scale Detected\nBefore : {scale}\nAfter : (1,1,1)");
                    _spawnedBossObject.transform.localScale = Vector3.one;
                }

                string bossDisplayName = bossId switch
                {
                    BossID.CaveRat      => "Cave Rat King",
                    BossID.QueenSpider  => "Queen Spider",
                    BossID.RockGolem    => "Rock Golem",
                    BossID.LavaWorm     => "Lava Worm",
                    BossID.CrystalTitan => "Crystal Titan",
                    _                   => bossId.ToString()
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

            // Construct Boss Data & Presenter
            var bossData = new BossData(bossId, depth);
            var bossDefeatedTcs = new UniTaskCompletionSource();

            _bossPresenter = new BossPresenter(bossData, _bossView, monsterView, spawnPoint);
            _bossPresenter.OnBossDefeated += () => bossDefeatedTcs.TrySetResult();

            // Achievement: subscribe before waiting
            _bossPresenter.OnBossDefeated += () =>
                DeepEarth.Common.GameEvents.FireBossKilled(bossId.ToString());

            // Wait until Boss is defeated
            await bossDefeatedTcs.Task;

            // Clear Boss 3D object
            if (_spawnedBossObject != null)
            {
                PoolSystem.Instance.Return(_spawnedBossObject);
                _spawnedBossObject = null;
            }

            _bossPresenter.Dispose();
            _bossPresenter = null;

            _bossView?.SetVisible(false);

            // Visual feedback on defeat
            EffectSystem.Instance.FlashScreen(new Color(0f, 0.8f, 1f, 0.3f), 0.3f);
            EffectSystem.Instance.ShakeCamera(0.3f, 0.1f);

            // Defeat heal reward (35% + drop chance modifier)
            float healChance = 0.35f + StatManager.Instance.BossHealDropChanceModifier;
            if (UnityEngine.Random.value < healChance)
            {
                StatManager.Instance.Heal(5);
                EffectSystem.Instance.SpawnDamageText(spawnPoint.position + Vector3.up, "+5 HP", Color.green);
            }

            // Open Boss Reward Selection UI
            typeof(GameManager).GetProperty("CurrentState").SetValue(gameMgr, GameState.BossReward);
            onGameDataChanged?.Invoke();

            var rewardCompletedTcs = new UniTaskCompletionSource();
            _bossRewardPresenter = new BossRewardPresenter(_bossRewardView, () => rewardCompletedTcs.TrySetResult());

            await rewardCompletedTcs.Task;

            _bossRewardPresenter = null;

            // Resume Standard Progression
            typeof(GameManager).GetProperty("CurrentState").SetValue(gameMgr, GameState.Playing);
            onGameDataChanged?.Invoke();

            MiningSystem.Instance.SpawnNextBlockAsync().Forget();
        }

        private void OnDestroy()
        {
            _bossPresenter?.Dispose();
            _bossRewardPresenter?.Dispose();
        }
    }
}
