using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

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
                PickaxeDurabilityManager.Instance.OnDurabilityWarning += HandleDurabilityWarning;
            }

            if (_view != null) _view.OnEmergencyRepairClicked += HandleEmergencyRepairClicked;

            UpdateDisplay();
        }

        public void Dispose()
        {
            if (PickaxeDurabilityManager.Instance != null)
            {
                PickaxeDurabilityManager.Instance.OnDurabilityChanged -= UpdateDisplay;
                PickaxeDurabilityManager.Instance.OnPickaxeBroken -= HandlePickaxeBroken;
                PickaxeDurabilityManager.Instance.OnPickaxeRepaired -= HandlePickaxeRepaired;
                PickaxeDurabilityManager.Instance.OnDurabilityWarning -= HandleDurabilityWarning;
            }

            if (_view != null) _view.OnEmergencyRepairClicked -= HandleEmergencyRepairClicked;
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

        private void HandleDurabilityWarning()
        {
            _view?.ShowWarningAlert();
        }

        private void HandleEmergencyRepairClicked()
        {
            if (PickaxeDurabilityManager.Instance == null) return;

            var result = PickaxeDurabilityManager.Instance.TryEmergencyRepair();
            string msgKey = result switch
            {
                PickaxeDurabilityManager.EmergencyRepairResult.CombatBlocked  => "pickaxe_emergency_repair_combat_only_blocked",
                PickaxeDurabilityManager.EmergencyRepairResult.AlreadyFull    => "pickaxe_emergency_repair_full",
                PickaxeDurabilityManager.EmergencyRepairResult.NoUsesLeft     => "pickaxe_emergency_repair_no_uses",
                PickaxeDurabilityManager.EmergencyRepairResult.NotEnoughHp    => "pickaxe_emergency_repair_low_hp",
                _ => null
            };

            Vector3 pos = Camera.main != null
                ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f
                : Vector3.zero;

            if (msgKey != null)
            {
                EffectSystem.Instance.SpawnDamageText(pos, LocalizationManager.Instance.GetTranslation(msgKey), Color.gray);
                return;
            }

            string successMsg = LocalizationManager.Instance.GetFormatted("pickaxe_emergency_repair_success_fmt", GameSettings.EmergencyRepairDurabilityGain);
            EffectSystem.Instance.SpawnDamageText(pos, successMsg, Color.green);
        }
    }
}
