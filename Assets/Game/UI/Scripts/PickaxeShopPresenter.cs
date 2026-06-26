using System;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class PickaxeShopPresenter
    {
        private readonly ShopContentView _contentView;
        private readonly PickaxeShopView _view;
        private bool _isActive;

        public event Action<ShopItemDisplayData> OnItemSelected;

        public PickaxeShopPresenter(ShopContentView contentView, GameObject slotPrefab)
        {
            _contentView = contentView;
            _view        = new PickaxeShopView(contentView.Content, slotPrefab);

            _view.OnItemSelected += data => OnItemSelected?.Invoke(data);
            _view.OnItemAction   += HandleItemAction;

            if (PickaxeManager.Instance != null)
                PickaxeManager.Instance.OnPickaxeStateChanged += OnStateChanged;
        }

        public void Activate()
        {
            _isActive = true;
            Refresh();
        }

        public void Deactivate()
        {
            _isActive = false;
        }

        public void Refresh()
        {
            if (!_isActive || PickaxeManager.Instance == null) return;

            _view.Refresh(
                PickaxeManager.Instance.AllPickaxes,
                SaveManager.CurrentData.EquippedPickaxeID,
                SaveManager.CurrentData);

            _contentView.ScrollToTop();
        }

        public void Dispose()
        {
            _view.ClearSlotRefs();
            if (PickaxeManager.Instance != null)
                PickaxeManager.Instance.OnPickaxeStateChanged -= OnStateChanged;
        }

        private void OnStateChanged() => Refresh();

        private void HandleItemAction(ShopItemDisplayData data)
        {
            if (data.tag is not PickaxeData pickaxeData) return;

            bool isUnlocked = SaveManager.CurrentData.UnlockedPickaxeIDs?.Contains(pickaxeData.pickaxeID) ?? false;
            if (isUnlocked || pickaxeData.isDefault)
                PickaxeManager.Instance?.Equip(pickaxeData);
            else
                PickaxeManager.Instance?.Purchase(pickaxeData);
        }
    }
}
