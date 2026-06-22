using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class PickaxeDurabilityPresenter
    {
        private readonly PickaxeDurabilityView _view;

        public PickaxeDurabilityPresenter(PickaxeDurabilityView view)
        {
            _view = view;

            if (PickaxeDurabilityManager.Instance != null)
            {
                PickaxeDurabilityManager.Instance.OnDurabilityChanged += UpdateDisplay;
                PickaxeDurabilityManager.Instance.OnPickaxeBroken += HandlePickaxeBroken;
                PickaxeDurabilityManager.Instance.OnPickaxeRepaired += HandlePickaxeRepaired;
            }

            UpdateDisplay();
        }

        public void Dispose()
        {
            if (PickaxeDurabilityManager.Instance != null)
            {
                PickaxeDurabilityManager.Instance.OnDurabilityChanged -= UpdateDisplay;
                PickaxeDurabilityManager.Instance.OnPickaxeBroken -= HandlePickaxeBroken;
                PickaxeDurabilityManager.Instance.OnPickaxeRepaired -= HandlePickaxeRepaired;
            }
        }

        private void UpdateDisplay()
        {
            if (_view == null || PickaxeDurabilityManager.Instance == null) return;

            int current = PickaxeDurabilityManager.Instance.CurrentDurability;
            int max = PickaxeDurabilityManager.Instance.MaxDurability;
            bool broken = PickaxeDurabilityManager.Instance.IsBroken;
            _view.SetDurability(current, max, broken);
        }

        private void HandlePickaxeBroken()
        {
            _view?.ShowBrokenAlert();
            UpdateDisplay();
        }

        private void HandlePickaxeRepaired()
        {
            _view?.HideBrokenAlert();
            UpdateDisplay();
        }
    }
}
