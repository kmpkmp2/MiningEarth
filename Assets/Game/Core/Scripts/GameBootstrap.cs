using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DeepEarth.Mining;
using DeepEarth.Combat;
using DeepEarth.Event;
using DeepEarth.Common;
using Cysharp.Threading.Tasks;

namespace DeepEarth.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private GameObject particlePrefab;
        [SerializeField] private Transform blockSpawnPoint;
        [SerializeField] private Transform monsterSpawnPoint;
        [SerializeField] private Camera mainCamera;

        [Header("SafeBox")]
        [SerializeField] private RectTransform safeBoxUIRoot;
        [SerializeField] private Camera safeBoxBackgroundCamera;

        private SceneReadyModel _readyModel;
        public SceneReadyModel ReadyModel => _readyModel;

        private UniTask _themeManagerTask = UniTask.CompletedTask;

        [Header("Route Map Config")]
        [SerializeField] private DeepEarth.Map.DefaultGridTemplate  gridTemplate;
        [SerializeField] private DeepEarth.Map.RoomGenerationConfig roomConfig;

        [Header("Map References")]
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform floorParent;
        [SerializeField] private Transform leftWallParent;
        [SerializeField] private Transform rightWallParent;
        [SerializeField] private Transform ceilingParent;

        private void Awake()
        {
            _readyModel = new SceneReadyModel();

            // Camera/AudioListener는 LoadingScene과의 핸드오프(§ActivateSceneCamera)가 끝나기
            // 전까지 비활성 상태로 둔다 — Camera.main 모호성 및 이중 AudioListener 경고 방지.
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
                var listener = mainCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            InitializeSystems();
        }

        // LoadingPresenter가 자신의 카메라/오디오리스너를 먼저 비활성화한 뒤 호출한다.
        public void ActivateSceneCamera()
        {
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
                var listener = mainCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
            _readyModel.Mark(SceneReadyModel.ReadyFlag.CameraReady);
            _readyModel.Mark(SceneReadyModel.ReadyFlag.AudioConnected);
        }

        // Additive 로드 중에는 SceneManager.SetActiveScene()이 아직 이 씬을 활성 씬으로
        // 받아들이지 못해 예외를 던진다(로드 완료 시점과 Awake() 실행 시점의 차이).
        // 대신 생성한 오브젝트를 개별적으로 이 씬으로 옮겨 LoadingScene에 잘못 배치되는 것을 막는다.
        private GameObject CreateSceneObject(string name)
        {
            var go = new GameObject(name);
            if (go.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            return go;
        }

        private void InitializeSystems()
        {
            if (FindAnyObjectByType<SafeBoxManager>() == null)
            {
                var go = CreateSceneObject("SafeBoxManager");
                go.AddComponent<SafeBoxManager>();
            }
            SafeBoxManager.Instance?.Initialize(mainCamera, safeBoxBackgroundCamera, safeBoxUIRoot);

            // Initialize Core Managers in Scene if not present
            if (FindAnyObjectByType<GameManager>() == null)
            {
                var go = CreateSceneObject("GameManager");
                go.AddComponent<GameManager>();
            }
            FindAnyObjectByType<GameManager>()?.SetMapConfig(gridTemplate, roomConfig);

            if (FindAnyObjectByType<MiningSystem>() == null)
            {
                var go = CreateSceneObject("MiningSystem");
                var sys = go.AddComponent<MiningSystem>();
                sys.Initialize(blockSpawnPoint);
            }

            if (FindAnyObjectByType<CombatSystem>() == null)
            {
                var go = CreateSceneObject("CombatSystem");
                var sys = go.AddComponent<CombatSystem>();
                sys.Initialize(monsterSpawnPoint, canvas != null ? canvas.transform : null);
            }

            if (FindAnyObjectByType<EliteCombatSystem>() == null)
            {
                var go = CreateSceneObject("EliteCombatSystem");
                var sys = go.AddComponent<EliteCombatSystem>();
                sys.Initialize(monsterSpawnPoint);
            }

            if (FindAnyObjectByType<EventManager>() == null)
            {
                var go = CreateSceneObject("EventManager");
                go.AddComponent<EventManager>();
            }

            if (FindAnyObjectByType<BossManager>() == null)
            {
                var go = CreateSceneObject("BossManager");
                go.AddComponent<BossManager>();
            }

            if (FindAnyObjectByType<EffectManager>() == null)
            {
                var go = CreateSceneObject("EffectManager");
                go.AddComponent<EffectManager>();
            }

            if (FindAnyObjectByType<StatusEffectManager>() == null)
            {
                var go = CreateSceneObject("StatusEffectManager");
                go.AddComponent<StatusEffectManager>();
            }

            if (FindAnyObjectByType<RelicManager>() == null)
            {
                var go = CreateSceneObject("RelicManager");
                go.AddComponent<RelicManager>();
            }

            if (FindAnyObjectByType<PickaxeManager>() == null)
            {
                var go = CreateSceneObject("PickaxeManager");
                go.AddComponent<PickaxeManager>();
            }

            if (FindAnyObjectByType<PickaxeDurabilityManager>() == null)
            {
                var go = CreateSceneObject("PickaxeDurabilityManager");
                go.AddComponent<PickaxeDurabilityManager>();
            }

            if (FindAnyObjectByType<AchievementManager>() == null)
            {
                var go = CreateSceneObject("AchievementManager");
                go.AddComponent<AchievementManager>();
            }

            if (FindAnyObjectByType<NodeEventManager>() == null)
            {
                var go = CreateSceneObject("NodeEventManager");
                go.AddComponent<NodeEventManager>();
            }

            // Initialize Map and Theme systems
            InitializeMapSystem();

            // Start the asynchronous boot sequence
            BootSequenceAsync().Forget();
        }

        private void InitializeMapSystem()
        {
            // Ensure PoolSystem exists
            if (FindAnyObjectByType<DeepEarth.Map.PoolSystem>() == null)
            {
                var go = CreateSceneObject("PoolSystem");
                go.AddComponent<DeepEarth.Map.PoolSystem>();
            }

            // Ensure TunnelGenerator exists
            var generator = FindAnyObjectByType<DeepEarth.Map.TunnelGenerator>();
            if (generator == null)
            {
                var go = CreateSceneObject("TunnelGenerator");
                generator = go.AddComponent<DeepEarth.Map.TunnelGenerator>();
            }
            generator.Initialize(mapRoot, floorParent, leftWallParent, rightWallParent, ceilingParent);

            // Ensure MapView exists
            var mapView = FindAnyObjectByType<DeepEarth.Map.MapView>();
            if (mapView == null)
            {
                var go = CreateSceneObject("MapView");
                mapView = go.AddComponent<DeepEarth.Map.MapView>();
            }
            mapView.Initialize(mapRoot);

            // Create Models and Presenters
            var depthModel = new DeepEarth.Map.DepthData();
            new DeepEarth.Map.MapPresenter(depthModel, mapView, generator);

            // Load and initialize ThemeManager from Addressables
            _themeManagerTask = LoadThemeManagerAsync(depthModel);
        }

        private async UniTask LoadThemeManagerAsync(DeepEarth.Map.DepthData depthModel)
        {
            GameObject themeManagerGo = await ResourceManager.Instance.InstantiateAsync(AddressableKeys.ThemeManager);
            if (themeManagerGo != null)
            {
                var themeManager = themeManagerGo.GetComponent<DeepEarth.Map.ThemeManager>();
                if (themeManager != null)
                {
                    themeManager.Initialize(depthModel);
                }
            }
        }

        private async UniTaskVoid BootSequenceAsync()
        {
            try
            {
                // Wait a frame for everything to settle
                await UniTask.Yield();

                if (StatusEffectManager.Instance != null)
                {
                    await StatusEffectManager.Instance.InitializeAsync();
                }

                if (RelicManager.Instance != null)
                {
                    await RelicManager.Instance.InitializeAsync();
                    var save = SaveManager.CurrentData;
                    if (save?.ActiveRelicIDs != null && save.ActiveRelicIDs.Count > 0)
                        RelicManager.Instance.RestoreRelicsFromSave(save.ActiveRelicIDs);
                }
                _readyModel.Mark(SceneReadyModel.ReadyFlag.SaveDataApplied);

                await InventoryManager.Instance.InitializeAsync();
                _readyModel.Mark(SceneReadyModel.ReadyFlag.InventoryReady);

                await GameBalanceData.LoadAsync();

                if (PickaxeManager.Instance != null)
                {
                    await PickaxeManager.Instance.InitializeAsync();
                }

                if (PickaxeDurabilityManager.Instance != null)
                {
                    await PickaxeDurabilityManager.Instance.InitializeAsync();
                }

                if (AchievementManager.Instance != null)
                {
                    await AchievementManager.Instance.InitializeAsync();
                }

                if (GameManager.Instance == null)
                    throw new InvalidOperationException("GameManager Instance is missing during bootstrap!");

                await GameManager.Instance.InitializeUIAsync(mainCamera, canvas, flashOverlay, particlePrefab);
                _readyModel.Mark(SceneReadyModel.ReadyFlag.HUDReady);

                var bossUITask = BossManager.Instance != null
                    ? BossManager.Instance.InitializeUIAsync(canvas.transform)
                    : UniTask.CompletedTask;
                var battleUITask = CombatSystem.Instance != null
                    ? CombatSystem.Instance.BattleUIReadyTask
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(_themeManagerTask, battleUITask, bossUITask);
                _readyModel.Mark(SceneReadyModel.ReadyFlag.BattleSystemReady);
                _readyModel.Mark(SceneReadyModel.ReadyFlag.AddressablesInstantiateComplete);

                // 나머지 모든 초기화가 끝난 뒤에만 실제 게임을 시작한다(Map 생성 + CurrentState 전환).
                // 이러면 기존 CurrentState 기반 입력 게이트들이 자연히 "Ready 이후에만 입력 허용"이 된다.
                GameManager.Instance.StartGame();
                _readyModel.Mark(SceneReadyModel.ReadyFlag.PlayerReady);
                _readyModel.Mark(SceneReadyModel.ReadyFlag.MapReady);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Boot sequence failed: {ex}");
                _readyModel.Fail(ex);
            }
        }
    }
}
