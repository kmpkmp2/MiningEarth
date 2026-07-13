using Cysharp.Threading.Tasks;
using DeepEarth.Audio;
using DeepEarth.Core;
using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.UI
{
    public class GameOverUIPresenter
    {
        private readonly GameOverUIView _view;
        private readonly GameManager _gameManager;

        // 런 종료 시점에 인벤토리에서 캡처한 자원 수량 (ClearRunInventory 전)
        private int _runIron, _runSilver, _runGold, _runDiamond;

        public GameOverUIPresenter(GameOverUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            _view.OnRestartClicked += HandleRestart;
            _view.OnResourceItemShown += HandleResourceItemShown;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnRestartClicked -= HandleRestart;
                _view.OnResourceItemShown -= HandleResourceItemShown;
            }

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void UpdateResultsUI()
        {
            // 자원 수량은 여기서 즉시 캡처한다 (RunEnd Step1 시점, ClearRunInventory 전)
            _runIron    = InventoryManager.Instance?.GetItemCount("Item_Iron")    ?? 0;
            _runSilver  = InventoryManager.Instance?.GetItemCount("Item_Silver")  ?? 0;
            _runGold    = InventoryManager.Instance?.GetItemCount("Item_Gold")    ?? 0;
            _runDiamond = InventoryManager.Instance?.GetItemCount("Item_Diamond") ?? 0;

            PlayResultAnimationAsync().Forget();
        }

        private async UniTaskVoid PlayResultAnimationAsync()
        {
            // 한 프레임 대기 → RunEnd Step2(will 계산), Step3(인벤 정리)가 같은 프레임에 완료됨
            await UniTask.Yield(PlayerLoopTiming.Update);

            int depth      = _gameManager.CurrentDepth;
            int willEarned = _gameManager.WillEarnedThisRun;
            int totalWill  = MetaProgressionManager.Instance?.Will ?? 0;
            int bestDepth  = SaveManager.CurrentData.BestDepth;

            Debug.Log($"[GameResult]\nIron : {_runIron}\nSilver : {_runSilver}\nGold : {_runGold}\nDiamond : {_runDiamond}");

            await _view.PlayResultAnimationAsync(
                depth, willEarned, totalWill, bestDepth,
                _runIron, _runSilver, _runGold, _runDiamond);
        }

        private void HandleLanguageChanged()
        {
            int depth      = _gameManager.CurrentDepth;
            int willEarned = _gameManager.WillEarnedThisRun;
            int totalWill  = MetaProgressionManager.Instance?.Will ?? 0;
            int bestDepth  = SaveManager.CurrentData.BestDepth;
            _view.SetResults(depth, willEarned, totalWill, bestDepth);
        }

        private void HandleResourceItemShown()
        {
            AudioManager.Instance?.PlaySFX("Mine_Get");
        }

        private void HandleRestart()
        {
            if (GameManager.Instance != null)
                Object.Destroy(GameManager.Instance.gameObject);
            Debug.Log("[Scene]\nLoad StartMenuScene");
            UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenuScene");
        }
    }
}
