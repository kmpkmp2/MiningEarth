using System;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class UpgradePresenter : IDisposable
    {
        private readonly UpgradePanelView         _view;
        private readonly CharacterSelectorPresenter _selectorPresenter;
        private readonly PassiveUpgradePresenter    _passivePresenter;

        public UpgradePresenter(UpgradePanelView view)
        {
            _view = view;

            if (view.characterSelectorView != null)
            {
                _selectorPresenter = new CharacterSelectorPresenter(view.characterSelectorView);
                _selectorPresenter.OnCharacterChanged += HandleCharacterChanged;
            }

            if (view.characterPassiveRowView != null)
                _passivePresenter = new PassiveUpgradePresenter(view.characterPassiveRowView);

            _view.OnUpgradeClicked += HandleUpgrade;

            if (MetaProgressionManager.Instance != null)
                MetaProgressionManager.Instance.OnMetaUpdated += Refresh;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += Refresh;
        }

        public void Open()
        {
            var charID = CharacterManager.Instance != null
                ? CharacterManager.Instance.SelectedCharacterID
                : CharacterID.Prisoner;

            _selectorPresenter?.SetCharacter(charID);
            _passivePresenter?.SetCharacter(charID);
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null || MetaProgressionManager.Instance == null) return;

            var meta = MetaProgressionManager.Instance;
            int will = meta.Will;

            _view.SetWill(will);

            int powerCost = meta.GetUpgradeCost(UpgradeType.MiningPower);
            _view.SetUpgradeState(UpgradeType.MiningPower, meta.MiningPowerLevel, powerCost, will >= powerCost);

            int hpCost = meta.GetUpgradeCost(UpgradeType.MaxHP);
            _view.SetUpgradeState(UpgradeType.MaxHP, meta.MaxHPLevel, hpCost, will >= hpCost);

            int invCost = meta.GetUpgradeCost(UpgradeType.InventorySize);
            _view.SetUpgradeState(UpgradeType.InventorySize, meta.InventorySizeLevel, invCost, will >= invCost);

            _selectorPresenter?.Refresh();
            _passivePresenter?.Refresh();
        }

        public void Dispose()
        {
            _selectorPresenter?.Dispose();
            _passivePresenter?.Dispose();

            if (_view != null)
                _view.OnUpgradeClicked -= HandleUpgrade;

            if (MetaProgressionManager.Instance != null)
                MetaProgressionManager.Instance.OnMetaUpdated -= Refresh;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }

        private void HandleUpgrade(UpgradeType type)
        {
            MetaProgressionManager.Instance?.Upgrade(type);
        }

        private void HandleCharacterChanged(CharacterID id)
        {
            _passivePresenter?.SetCharacter(id);
        }
    }
}
