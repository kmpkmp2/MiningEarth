using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class GameUIPresenter
    {
        private readonly GameUIView _view;
        private readonly GameManager _gameManager;

        private readonly EffectHUDPresenter _effectHUDPresenter;

        public GameUIPresenter(GameUIView view, GameManager gameManager)
        {
            _view = view;
            _gameManager = gameManager;

            // Subscribe to state change events
            StatManager.Instance.OnHPChanged += UpdateHP;
            StatManager.Instance.OnStatsUpdated += UpdateStats;
            _gameManager.OnGameDataChanged += UpdateAll;

            // Subscribe to settings panel trigger
            _view.OnSettingsClicked += HandleSettingsClicked;
            _view.OnRelicClicked += HandleRelicClicked;
            _view.OnInventoryClicked += HandleInventoryClicked;

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateAll;
            }

            // Bind Effect HUD view
            var iconContainer = _view.GetEffectIconContainer();
            if (iconContainer != null)
            {
                var hudEffectView = iconContainer.GetComponent<EffectHUDView>();
                if (hudEffectView == null)
                {
                    hudEffectView = iconContainer.gameObject.AddComponent<EffectHUDView>();
                }
                _effectHUDPresenter = new EffectHUDPresenter(hudEffectView);
            }

            UpdateAll();
        }

        public void Dispose()
        {
            if (StatManager.Instance != null)
            {
                StatManager.Instance.OnHPChanged -= UpdateHP;
                StatManager.Instance.OnStatsUpdated -= UpdateStats;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameDataChanged -= UpdateAll;
            }

            if (_view != null)
            {
                _view.OnSettingsClicked -= HandleSettingsClicked;
                _view.OnRelicClicked -= HandleRelicClicked;
                _view.OnInventoryClicked -= HandleInventoryClicked;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdateAll;
            }

            _effectHUDPresenter?.Dispose();
        }

        private void HandleSettingsClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenSettings();
            }
        }

        private void HandleRelicClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenRelicPopup();
            }
        }

        private void HandleInventoryClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.OpenInventoryPopup();
            }
        }

        private void UpdateHP()
        {
            _view.SetHP(StatManager.Instance.CurrentHP, StatManager.Instance.GetMaxHP());
        }

        private void UpdateStats()
        {
            UpdateHP();
            UpdateInventoryCapacity();
        }

        private void UpdateInventoryCapacity()
        {
            int currentItems = _gameManager.IronCount + _gameManager.SilverCount + _gameManager.GoldCount + _gameManager.DiamondCount;
            _view.SetInventorySize(currentItems, StatManager.Instance.GetInventorySize());
        }

        private void UpdateAll()
        {
            UpdateHP();
            UpdateInventoryCapacity();
            _view.SetDepth(_gameManager.CurrentDepth, _gameManager.DifficultyName);
            _view.SetResources(_gameManager.IronCount, _gameManager.SilverCount, _gameManager.GoldCount, _gameManager.DiamondCount);
            
            if (LocalizationManager.Instance != null)
            {
                _view.LocalizeSettingsButton(LocalizationManager.Instance.GetTranslation("hud_settings_btn"));
                _view.LocalizeRelicButton(LocalizationManager.Instance.GetTranslation("hud_relic_btn"));
                _view.LocalizeInventoryButton(LocalizationManager.Instance.GetTranslation("hud_inventory_btn"));
            }
        }
    }
}
