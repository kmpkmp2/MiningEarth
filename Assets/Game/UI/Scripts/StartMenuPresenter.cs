using UnityEngine;
using UnityEngine.SceneManagement;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class StartMenuPresenter
    {
        private readonly StartMenuUIView      _view;
        private readonly CharacterPresenter   _characterPresenter;
        private readonly AchievementPresenter _achievementPresenter;
        private ShopPresenter _shopPresenter;

        public StartMenuPresenter(StartMenuUIView view, GameObject shopSlotPrefab)
        {
            _view = view;

            // Subscribe to UI view events
            _view.OnPlayClicked += HandlePlay;
            _view.OnUpgradeMenuClicked += HandleUpgradeMenu;
            _view.OnShopClicked += HandleShopMenu;
            _view.OnSettingsClicked += HandleSettingsMenu;
            _view.OnBackClicked += HandleBack;
            _view.OnUpgradeStatClicked += HandleUpgradeStat;
            _view.OnLanguageKoClicked += HandleLanguageKo;
            _view.OnLanguageEnClicked += HandleLanguageEn;
            _view.OnCharacterMenuClicked += HandleCharacterMenuClicked;
            _view.OnAchievementMenuClicked += HandleAchievementMenuClicked;

            // Setup Character selection presenter
            if (_view.CharacterPopupView != null)
            {
                _characterPresenter = new CharacterPresenter(_view.CharacterPopupView);
            }

            // Setup Achievement presenter
            if (_view.AchievementPopupView != null)
                _achievementPresenter = new AchievementPresenter(_view.AchievementPopupView);

            // Setup Shop presenter
            if (_view.ShopPanelView != null)
                _shopPresenter = new ShopPresenter(_view.ShopPanelView, shopSlotPrefab);

            // Subscribe to model/manager events
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.OnMetaUpdated += RefreshUpgradeUI;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            // Initialization
            _view.ShowMainMenu();
            _view.Localize();
            if (LocalizationManager.Instance != null)
            {
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            }
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
                _view.OnUpgradeStatClicked -= HandleUpgradeStat;
                _view.OnLanguageKoClicked -= HandleLanguageKo;
                _view.OnLanguageEnClicked -= HandleLanguageEn;
                _view.OnCharacterMenuClicked -= HandleCharacterMenuClicked;
                _view.OnAchievementMenuClicked -= HandleAchievementMenuClicked;
            }

            _characterPresenter?.Dispose();
            _achievementPresenter?.Dispose();
            _shopPresenter?.Dispose();

            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.OnMetaUpdated -= RefreshUpgradeUI;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandlePlay()
        {
            SceneManager.LoadScene("MainGameScene");
        }

        private void HandleUpgradeMenu()
        {
            _view.ShowUpgradeMenu();
            RefreshUpgradeUI();
        }

        private void HandleShopMenu()
        {
            _view.ShowShopMenu();
            _shopPresenter?.Refresh();
            _view.Localize();
        }

        private void HandleSettingsMenu()
        {
            _view.ShowSettingsMenu();
            if (LocalizationManager.Instance != null)
            {
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            }
        }

        private void HandleBack()
        {
            _view.ShowMainMenu();
        }

        private void HandleUpgradeStat(UpgradeType type)
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.Upgrade(type);
            }
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

        private void HandleLanguageChanged()
        {
            _view.Localize();
            if (LocalizationManager.Instance != null)
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            _shopPresenter?.Localize();
            RefreshUpgradeUI();
        }

        private void HandleCharacterMenuClicked()
        {
            _characterPresenter?.OpenPopup();
        }

        private void HandleAchievementMenuClicked()
        {
            _achievementPresenter?.Open();
        }

        private void RefreshUpgradeUI()
        {
            if (MetaProgressionManager.Instance == null) return;

            var meta = MetaProgressionManager.Instance;
            int currentWill = meta.Will;

            _view.SetWill(currentWill);

            int powerCost = meta.GetUpgradeCost(UpgradeType.MiningPower);
            _view.SetUpgradeState(UpgradeType.MiningPower, meta.MiningPowerLevel, powerCost, currentWill >= powerCost);

            int hpCost = meta.GetUpgradeCost(UpgradeType.MaxHP);
            _view.SetUpgradeState(UpgradeType.MaxHP, meta.MaxHPLevel, hpCost, currentWill >= hpCost);

            int invCost = meta.GetUpgradeCost(UpgradeType.InventorySize);
            _view.SetUpgradeState(UpgradeType.InventorySize, meta.InventorySizeLevel, invCost, currentWill >= invCost);
        }
    }
}
