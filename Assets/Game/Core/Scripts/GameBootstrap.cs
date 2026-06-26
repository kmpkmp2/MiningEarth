using UnityEngine;
using UnityEngine.UI;
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

        [Header("Map References")]
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform floorParent;
        [SerializeField] private Transform leftWallParent;
        [SerializeField] private Transform rightWallParent;
        [SerializeField] private Transform ceilingParent;

        private void Awake()
        {
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // Initialize Core Managers in Scene if not present
            if (FindAnyObjectByType<GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }

            if (FindAnyObjectByType<MiningSystem>() == null)
            {
                var go = new GameObject("MiningSystem");
                var sys = go.AddComponent<MiningSystem>();
                sys.Initialize(blockSpawnPoint);
            }

            if (FindAnyObjectByType<CombatSystem>() == null)
            {
                var go = new GameObject("CombatSystem");
                var sys = go.AddComponent<CombatSystem>();
                sys.Initialize(monsterSpawnPoint);
            }

            if (FindAnyObjectByType<EventManager>() == null)
            {
                var go = new GameObject("EventManager");
                go.AddComponent<EventManager>();
            }

            if (FindAnyObjectByType<BossManager>() == null)
            {
                var go = new GameObject("BossManager");
                go.AddComponent<BossManager>();
            }

            if (FindAnyObjectByType<EffectManager>() == null)
            {
                var go = new GameObject("EffectManager");
                go.AddComponent<EffectManager>();
            }

            if (FindAnyObjectByType<StatusEffectManager>() == null)
            {
                var go = new GameObject("StatusEffectManager");
                go.AddComponent<StatusEffectManager>();
            }

            if (FindAnyObjectByType<RelicManager>() == null)
            {
                var go = new GameObject("RelicManager");
                go.AddComponent<RelicManager>();
            }

            if (FindAnyObjectByType<PickaxeManager>() == null)
            {
                var go = new GameObject("PickaxeManager");
                go.AddComponent<PickaxeManager>();
            }

            if (FindAnyObjectByType<PickaxeDurabilityManager>() == null)
            {
                var go = new GameObject("PickaxeDurabilityManager");
                go.AddComponent<PickaxeDurabilityManager>();
            }

            if (FindAnyObjectByType<AchievementManager>() == null)
            {
                var go = new GameObject("AchievementManager");
                go.AddComponent<AchievementManager>();
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
                var go = new GameObject("PoolSystem");
                go.AddComponent<DeepEarth.Map.PoolSystem>();
            }

            // Ensure MapGenerator exists
            var generator = FindAnyObjectByType<DeepEarth.Map.MapGenerator>();
            if (generator == null)
            {
                var go = new GameObject("MapGenerator");
                generator = go.AddComponent<DeepEarth.Map.MapGenerator>();
            }
            generator.Initialize(mapRoot, floorParent, leftWallParent, rightWallParent, ceilingParent);

            // Ensure MapView exists
            var mapView = FindAnyObjectByType<DeepEarth.Map.MapView>();
            if (mapView == null)
            {
                var go = new GameObject("MapView");
                mapView = go.AddComponent<DeepEarth.Map.MapView>();
            }
            mapView.Initialize(mapRoot);

            // Create Models and Presenters
            var depthModel = new DeepEarth.Map.DepthData();
            new DeepEarth.Map.MapPresenter(depthModel, mapView, generator);

            // Load and initialize ThemeManager from Addressables
            LoadThemeManagerAsync(depthModel).Forget();
        }

        private async UniTaskVoid LoadThemeManagerAsync(DeepEarth.Map.DepthData depthModel)
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
            // Wait a frame for everything to settle
            await UniTask.Yield();

            if (StatusEffectManager.Instance != null)
            {
                await StatusEffectManager.Instance.InitializeAsync();
            }

            if (RelicManager.Instance != null)
            {
                await RelicManager.Instance.InitializeAsync();
            }

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

            if (GameManager.Instance != null)
            {
                await GameManager.Instance.InitializeUIAsync(canvas, flashOverlay, particlePrefab);
            }
            else
            {
                Debug.LogError("GameManager Instance is missing during bootstrap!");
            }

            if (BossManager.Instance != null)
            {
                await BossManager.Instance.InitializeUIAsync(canvas.transform);
            }
        }
    }
}
