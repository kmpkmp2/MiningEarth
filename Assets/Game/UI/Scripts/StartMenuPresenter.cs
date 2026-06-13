using UnityEngine.SceneManagement;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class StartMenuPresenter
    {
        private readonly StartMenuUIView _view;
        private readonly CharacterPresenter _characterPresenter;

        public StartMenuPresenter(StartMenuUIView view)
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

            // Setup Character selection presenter
            if (_view.CharacterPopupView != null)
            {
                _characterPresenter = new CharacterPresenter(_view.CharacterPopupView);
            }

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
            }

            _characterPresenter?.Dispose();

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
            {
                _view.UpdateLanguageVisuals(LocalizationManager.Instance.CurrentLanguageCode);
            }
            RefreshUpgradeUI();
        }

        private void HandleCharacterMenuClicked()
        {
            _characterPresenter?.OpenPopup();
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

            int attackCost = meta.GetUpgradeCost(UpgradeType.Attack);
            _view.SetUpgradeState(UpgradeType.Attack, meta.AttackLevel, attackCost, currentWill >= attackCost);
        }
    }
}
