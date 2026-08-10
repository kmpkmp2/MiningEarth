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

            // CharacterDatabase는 보통 LoadingScene에서 이미 로드되지만, StartMenuScene에
            // LoadingScene을 거치지 않고 진입하는 경로(에디터 직접 재생 등)에서는 비어있을 수 있다.
            // 이미 로드되어 있으면 즉시 반환되므로 중복 호출해도 안전하다.
            await CharacterDatabase.LoadAsync();

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
