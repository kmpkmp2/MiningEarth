using System.Linq;
using Cysharp.Threading.Tasks;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    // 떠돌이 상점 진입점 — GameManager가 RoomType.Merchant 노드에서 소유/호출한다.
    // 신규 Singleton Manager를 두지 않고 GameManager가 보유하는 일반 객체로 생성한다.
    public class MerchantPresenter
    {
        private readonly MerchantPopupView _view;
        private readonly PurchasePresenter _purchasePresenter;
        private readonly MerchantShopPresenter _shopPresenter;

        private MerchantQuoteData _quoteData;
        private MerchantModel _model;
        private UniTaskCompletionSource _closeTcs;

        public MerchantPresenter(MerchantPopupView view)
        {
            _view = view;
            _purchasePresenter = new PurchasePresenter();
            _shopPresenter = new MerchantShopPresenter(view, _purchasePresenter);

            _view.OnLeaveClicked += HandleLeaveClicked;
            _purchasePresenter.OnPurchaseFailed += HandlePurchaseFailed;
            _shopPresenter.OnSlotPurchased += HandleSlotPurchased;
        }

        public void Dispose()
        {
            _view.OnLeaveClicked -= HandleLeaveClicked;
            _purchasePresenter.OnPurchaseFailed -= HandlePurchaseFailed;
            _shopPresenter.OnSlotPurchased -= HandleSlotPurchased;
        }

        public async UniTask OpenAsync(int depth)
        {
            await EnsureLoadedAsync();

            GameManager.Instance.PauseForEvent();

            var inventory = _model.GenerateShop(depth);
            _shopPresenter.BindInventory(inventory);

            var loc = LocalizationManager.Instance;
            _view.SetTitle(loc.GetTranslation("merchant_title"));
            _view.SetLeaveLabel(loc.GetTranslation("merchant_leave_button"));
            _view.SetSectionLabels(
                loc.GetTranslation("merchant_relic_section_label"),
                loc.GetTranslation("merchant_consumable_section_label"),
                loc.GetTranslation("merchant_potion_section_label"),
                loc.GetTranslation("merchant_featured_label"));
            _view.QuoteView?.SetName(loc.GetTranslation("merchant_title"));

            ApplyLegendaryPresentation(inventory);

            _view.SetVisible(true);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.Log($"[Merchant]\nShop Opened\nDepth : {depth}");
#endif

            _closeTcs = new UniTaskCompletionSource();
            await _closeTcs.Task;

            _view.SetVisible(false);
            GameManager.Instance.ResumeAfterEvent();
        }

        private async UniTask EnsureLoadedAsync()
        {
            await MerchantBalanceData.LoadAsync();

            if (_quoteData == null)
                _quoteData = await ResourceManager.Instance.LoadAssetAsync<MerchantQuoteData>(AddressableKeys.MerchantQuoteDataKey);

            _model = new MerchantModel(MerchantBalanceData.Instance, _quoteData);
        }

        private void ApplyLegendaryPresentation(MerchantInventoryModel inventory)
        {
            bool hasLegendaryRelic = inventory.RelicSlots.Any(s => s.relic.rarity == RelicRarity.Legendary)
                || (inventory.FeaturedSlot != null && inventory.FeaturedSlot.relic.rarity == RelicRarity.Legendary);
            bool hasLegendaryConsumable = inventory.ConsumableSlots.Any(s => s.item.rarity == ItemRarity.Legendary);

            _view.SetLegendaryGlow(hasLegendaryRelic);

            string quoteKey;
            if (hasLegendaryConsumable && _quoteData != null) quoteKey = _quoteData.legendaryItemQuoteKey;
            else if (hasLegendaryRelic && _quoteData != null) quoteKey = _quoteData.legendaryRelicQuoteKey;
            else quoteKey = inventory.MerchantQuoteKey;

            ShowQuote(quoteKey);
        }

        private void ShowQuote(string quoteKey)
        {
            if (string.IsNullOrEmpty(quoteKey)) return;
            _view.QuoteView?.SetQuote(LocalizationManager.Instance.GetTranslation(quoteKey));
        }

        private void HandleLeaveClicked() => _closeTcs?.TrySetResult();

        private void HandlePurchaseFailed(string reasonKey)
        {
            if (reasonKey == "merchant_quote_no_funds")
                ShowQuote(_quoteData != null ? _quoteData.noFundsQuoteKey : reasonKey);
        }

        private void HandleSlotPurchased(MerchantSlotData slot)
        {
            if (!slot.IsLegendary || _quoteData == null) return;
            ShowQuote(slot.item != null ? _quoteData.legendaryItemQuoteKey : _quoteData.legendaryRelicQuoteKey);
        }
    }
}
