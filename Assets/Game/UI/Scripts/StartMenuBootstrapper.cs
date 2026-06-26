using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.UI;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class StartMenuBootstrapper : MonoBehaviour
    {
        [SerializeField] private StartMenuUIView view;

        private StartMenuPresenter _presenter;

        private void Start()
        {
            BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            if (DeepEarth.Core.PickaxeManager.Instance == null)
            {
                var go = new GameObject("PickaxeManager");
                go.AddComponent<DeepEarth.Core.PickaxeManager>();
            }
            await DeepEarth.Core.PickaxeManager.Instance.InitializeAsync();

            // AchievementManager may not exist when game starts at StartMenuScene
            if (AchievementManager.Instance == null)
            {
                var go = new GameObject("AchievementManager");
                go.AddComponent<AchievementManager>();
                await AchievementManager.Instance.InitializeAsync();
            }

            if (view == null)
                view = FindAnyObjectByType<StartMenuUIView>();

            if (view != null)
            {
                // ShopItemSlot 프리팹 사전 로드 (Addressables)
                var slotPrefab = await ResourceManager.Instance
                    .LoadAssetAsync<GameObject>(AddressableKeys.ShopItemSlot);

                _presenter = new StartMenuPresenter(view, slotPrefab);
            }
            else
            {
                Debug.LogError("StartMenuBootstrapper: StartMenuUIView not found in scene!");
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }
    }
}
