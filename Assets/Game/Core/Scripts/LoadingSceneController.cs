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
        [SerializeField] private LoadingPanelView panelView;

        private void Start()
        {
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            var presenter = new LoadingPresenter(panelView);
            await presenter.ExecuteAsync();
        }
    }
}
