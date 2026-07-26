using System;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    // 상점 슬롯 데이터 바인딩 + 슬롯 View 이벤트 구독 전담.
    public class MerchantShopPresenter
    {
        private readonly MerchantPopupView _view;
        private readonly PurchasePresenter _purchasePresenter;

        private MerchantInventoryModel _inventory;

        public event Action<MerchantSlotData> OnSlotPurchased;

        public MerchantShopPresenter(MerchantPopupView view, PurchasePresenter purchasePresenter)
        {
            _view = view;
            _purchasePresenter = purchasePresenter;

            SubscribeSlot(_view.FeaturedSlot);
            foreach (var s in _view.RelicSlots) SubscribeSlot(s);
            foreach (var s in _view.ConsumableSlots) SubscribeSlot(s);
            foreach (var s in _view.PotionSlots) SubscribeSlot(s);
        }

        private void SubscribeSlot(MerchantItemSlotView slotView)
        {
            if (slotView != null) slotView.OnBuyClicked += () => HandleBuyClicked(slotView);
        }

        public void BindInventory(MerchantInventoryModel inventory)
        {
            _inventory = inventory;

            BindSlot(_view.FeaturedSlot, inventory.FeaturedSlot);
            BindSlotList(_view.RelicSlots, inventory.RelicSlots);
            BindSlotList(_view.ConsumableSlots, inventory.ConsumableSlots);
            BindSlotList(_view.PotionSlots, inventory.PotionSlots);

            RefreshCurrencyDisplay();
            RefreshDiscountBanner();
        }

        private void BindSlotList(MerchantItemSlotView[] views, System.Collections.Generic.List<MerchantSlotData> data)
        {
            for (int i = 0; i < views.Length; i++)
                BindSlot(views[i], i < data.Count ? data[i] : null);
        }

        private void BindSlot(MerchantItemSlotView slotView, MerchantSlotData data)
        {
            if (slotView == null) return;

            if (data == null)
            {
                slotView.gameObject.SetActive(false);
                return;
            }

            slotView.gameObject.SetActive(true);

            var loc = LocalizationManager.Instance;
            string name = loc.GetTranslation(data.NameLocKey);
            string desc = loc.GetTranslation(data.DescLocKey);
            string priceLabel = BuildPriceLabel(data);
            bool canAfford = _purchasePresenter.CanAfford(data);

            slotView.SetData(data, canAfford, name, desc, priceLabel);
        }

        private string BuildPriceLabel(MerchantSlotData data)
        {
            var currencyTemplate = InventoryManager.Instance?.GetTemplate(data.priceCurrencyItemId);
            string currencyName = currencyTemplate != null
                ? LocalizationManager.Instance.GetTranslation(currencyTemplate.nameLocKey)
                : data.priceCurrencyItemId;

            return $"{data.finalPrice} {currencyName}";
        }

        private void HandleBuyClicked(MerchantItemSlotView slotView)
        {
            if (slotView.SlotData == null) return;

            bool success = _purchasePresenter.TryPurchase(slotView.SlotData);
            RefreshCurrencyDisplay();
            RefreshAllAffordability();

            if (success) OnSlotPurchased?.Invoke(slotView.SlotData);
        }

        private void RefreshAllAffordability()
        {
            RebindSlotAffordability(_view.FeaturedSlot);
            foreach (var s in _view.RelicSlots) RebindSlotAffordability(s);
            foreach (var s in _view.ConsumableSlots) RebindSlotAffordability(s);
            foreach (var s in _view.PotionSlots) RebindSlotAffordability(s);
        }

        private void RebindSlotAffordability(MerchantItemSlotView slotView)
        {
            if (slotView == null || slotView.SlotData == null) return;
            BindSlot(slotView, slotView.SlotData);
        }

        private void RefreshCurrencyDisplay()
        {
            var inv = InventoryManager.Instance;
            int silver = inv?.GetItemCount(AddressableKeys.ItemSilver) ?? 0;
            int gold = inv?.GetItemCount(AddressableKeys.ItemGold) ?? 0;
            int diamond = inv?.GetItemCount(AddressableKeys.ItemDiamond) ?? 0;
            int root = inv?.GetItemCount(AddressableKeys.ItemWood) ?? 0;

            _view.SetCurrencyText(LocalizationManager.Instance.GetFormatted("merchant_currency_fmt", silver, gold, diamond, root));
        }

        private void RefreshDiscountBanner()
        {
            if (_inventory == null || !_inventory.HasDiscountEvent)
            {
                _view.SetDiscountBanner(string.Empty);
                return;
            }

            var balance = MerchantBalanceData.Instance;
            string key = _inventory.IsAllDiscount ? "merchant_discount_banner_all_fmt" : "merchant_discount_banner_relic_fmt";
            int percent = Mathf.RoundToInt((_inventory.IsAllDiscount ? balance.discountAllPercent : balance.discountRareRelicPercent) * 100f);
            _view.SetDiscountBanner(LocalizationManager.Instance.GetFormatted(key, percent));
        }
    }
}
