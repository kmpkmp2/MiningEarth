using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class SettingsUIPresenter
    {
        private readonly SettingsUIView _view;
        private readonly GameManager _gameManager;

        public SettingsUIPresenter(SettingsUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            // Subscribe to view events
            _view.OnCloseClicked += HandleClose;
            _view.OnLanguageKoClicked += HandleLanguageKo;
            _view.OnLanguageEnClicked += HandleLanguageEn;

            // Subscribe to localization changes
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += RefreshUI;
            }

            RefreshUI();
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnCloseClicked -= HandleClose;
                _view.OnLanguageKoClicked -= HandleLanguageKo;
                _view.OnLanguageEnClicked -= HandleLanguageEn;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshUI;
            }
        }

        private void HandleClose()
        {
            _gameManager.CloseSettings();
        }

        private void HandleLanguageKo()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage("ko");
            }
        }

        private void HandleLanguageEn()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage("en");
            }
        }

        private void RefreshUI()
        {
            _view.Localize();
            if (LocalizationManager.Instance != null)
            {
                _view.UpdateVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            }
        }
    }
}
