using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class StartMenuPresenter
    {
        private readonly StartMenuUIView      _view;
        private readonly AchievementPresenter _achievementPresenter;
        private ShopPresenter     _shopPresenter;
        private RunSetupPresenter _runSetupPresenter;
        private UpgradePresenter  _upgradePresenter;

        public StartMenuPresenter(StartMenuUIView view, GameObject shopSlotPrefab)
        {
            _view = view;

            _view.OnPlayClicked += HandlePlay;
            _view.OnUpgradeMenuClicked += HandleUpgradeMenu;
            _view.OnShopClicked += HandleShopMenu;
            _view.OnSettingsClicked += HandleSettingsMenu;
            _view.OnBackClicked += HandleBack;
            _view.OnLanguageKoClicked += HandleLanguageKo;
            _view.OnLanguageEnClicked += HandleLanguageEn;
            _view.OnCharacterMenuClicked += HandleCharacterMenuClicked;
            _view.OnAchievementMenuClicked += HandleAchievementMenuClicked;

            if (_view.AchievementPopupView != null)
                _achievementPresenter = new AchievementPresenter(_view.AchievementPopupView);

            if (_view.ShopPanelView != null)
                _shopPresenter = new ShopPresenter(_view.ShopPanelView, shopSlotPrefab);

            if (_view.RunSetupPanelView != null)
            {
                _runSetupPresenter = new RunSetupPresenter(_view.RunSetupPanelView);
                _view.RunSetupPanelView.OnBackClicked += HandleBack;
            }

            if (_view.UpgradePanelView != null)
                _upgradePresenter = new UpgradePresenter(_view.UpgradePanelView);

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;

            _view.ShowMainMenu();
            _view.Localize();
            if (LocalizationManager.Instance != null)
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
        }

        public void Dispose()
        {
            if (_view != null)
            {
                _view.OnPlayClicked -= HandlePlay;
                _view.OnUpgradeMenuClicked -= HandleUpgradeMenu;
                _view.OnShopClicked -= HandleShopMenu;
                _view.OnSettingsClicked -= HandleSettingsMenu;
                _view.OnBackClicked -= HandleBack;
                _view.OnLanguageKoClicked -= HandleLanguageKo;
                _view.OnLanguageEnClicked -= HandleLanguageEn;
                _view.OnCharacterMenuClicked -= HandleCharacterMenuClicked;
                _view.OnAchievementMenuClicked -= HandleAchievementMenuClicked;
            }

            _achievementPresenter?.Dispose();
            _shopPresenter?.Dispose();
            _runSetupPresenter?.Dispose();
            _upgradePresenter?.Dispose();

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandlePlay()
        {
            _view.ShowRunSetupPanel();
            _runSetupPresenter?.Open();
        }

        private void HandleUpgradeMenu()
        {
            _view.ShowUpgradeMenu();
            _upgradePresenter?.Open();
        }

        private void HandleShopMenu()
        {
            _view.ShowShopMenu();
            _shopPresenter?.SelectCategory(ShopCategory.Pickaxe);
            _view.Localize();
        }

        private void HandleSettingsMenu()
        {
            _view.ShowSettingsMenu();
            if (LocalizationManager.Instance != null)
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
        }

        private void HandleBack()
        {
            _view.ShowMainMenu();
        }

        private void HandleLanguageKo()
        {
            LocalizationManager.Instance?.SetLanguage("ko");
        }

        private void HandleLanguageEn()
        {
            LocalizationManager.Instance?.SetLanguage("en");
        }

        private void HandleLanguageChanged()
        {
            _view.Localize();
            if (LocalizationManager.Instance != null)
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            _shopPresenter?.Localize();
        }

        private void HandleCharacterMenuClicked()
        {
            _view.ShowShopMenu();
            _shopPresenter?.SelectCategory(ShopCategory.Character);
            _view.Localize();
        }

        private void HandleAchievementMenuClicked()
        {
            _achievementPresenter?.Open();
        }
    }
}
