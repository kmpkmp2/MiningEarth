using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    public class GameOverUIPresenter
    {
        private readonly GameOverUIView _view;
        private readonly GameManager _gameManager;

        public GameOverUIPresenter(GameOverUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            // Subscribe to UI clicks
            _view.OnRestartClicked += HandleRestart;

            // Subscribe to language change
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            // Display results
            UpdateResultsUI();
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnRestartClicked -= HandleRestart;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged()
        {
            UpdateResultsUI();
        }

        private void UpdateResultsUI()
        {
            int depth = _gameManager.CurrentDepth;
            int willEarned = _gameManager.WillEarnedThisRun;
            int totalWill = MetaProgressionManager.Instance.Will;
            int bestDepth = SaveManager.CurrentData.BestDepth;

            _view.SetResults(depth, willEarned, totalWill, bestDepth);
        }

        private void HandleRestart()
        {
            if (GameManager.Instance != null)
            {
                UnityEngine.Object.Destroy(GameManager.Instance.gameObject);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenuScene");
        }
    }
}
