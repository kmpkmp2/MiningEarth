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

            // Determine Boss type: 50=StoneGolem, 100=MotherCaveSpider, 150=SkeletonWarlord, 200+=AllMetalColossus
            BossID bossId;
            if      (depth == 50)  bossId = BossID.StoneGolem;
            else if (depth == 100) bossId = BossID.MotherCaveSpider;
            else if (depth == 150) bossId = BossID.SkeletonWarlord;
            else                   bossId = BossID.AllMetalColossus;

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

        private void OnDestroy()
        {
            _bossPresenter?.Dispose();
            _bossRewardPresenter?.Dispose();
        }
    }
}
