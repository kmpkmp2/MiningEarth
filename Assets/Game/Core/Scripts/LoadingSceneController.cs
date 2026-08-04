using UnityEngine;
using Cysharp.Threading.Tasks;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    /// <summary>
    /// LoadingScene의 MonoBehaviour 진입점.
    /// LoadingPresenter에 뷰를 주입하고 ExecuteAsync()를 실행한다.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] private LoadingPanelView          panelView;
        [SerializeField] private LoadingFadeView           fadeView;
        [SerializeField] private LoadingFailurePopupView   failureView;
        [SerializeField] private Camera                    mainCamera;

        private void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            var presenter = new LoadingPresenter(panelView, fadeView, failureView, mainCamera);
            await presenter.ExecuteAsync();
        }
    }
}
