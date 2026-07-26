using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    // 떠돌이 상점 팝업 루트 View. UI_Panel_Event와 완전히 분리된 신규 프리팹(UI_Panel_Merchant)에 부착.
    public class MerchantPopupView : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private MerchantQuoteView quoteView;
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private TextMeshProUGUI discountBannerText;
        [SerializeField] private GameObject legendaryShopGlow;

        [Header("Featured Slot")]
        [SerializeField] private TextMeshProUGUI featuredLabelText;
        [SerializeField] private MerchantItemSlotView featuredSlot;

        [Header("Relic Section (4)")]
        [SerializeField] private TextMeshProUGUI relicSectionLabelText;
        [SerializeField] private MerchantItemSlotView[] relicSlots;

        [Header("Consumable Section (2)")]
        [SerializeField] private TextMeshProUGUI consumableSectionLabelText;
        [SerializeField] private MerchantItemSlotView[] consumableSlots;

        [Header("Potion Section (3)")]
        [SerializeField] private TextMeshProUGUI potionSectionLabelText;
        [SerializeField] private MerchantItemSlotView[] potionSlots;

        [Header("Actions")]
        [SerializeField] private Button leaveButton;

        public event Action OnLeaveClicked;

        public MerchantItemSlotView FeaturedSlot => featuredSlot;
        public MerchantItemSlotView[] RelicSlots => relicSlots;
        public MerchantItemSlotView[] ConsumableSlots => consumableSlots;
        public MerchantItemSlotView[] PotionSlots => potionSlots;
        public MerchantQuoteView QuoteView => quoteView;

        private void Awake()
        {
            if (leaveButton != null) leaveButton.onClick.AddListener(() => OnLeaveClicked?.Invoke());
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (popupRoot != null) popupRoot.SetActive(visible);
        }

        public void SetTitle(string text)
        {
            if (titleText != null) titleText.text = text;
        }

        public void SetSectionLabels(string relicLabel, string consumableLabel, string potionLabel, string featuredLabel)
        {
            if (relicSectionLabelText != null) relicSectionLabelText.text = relicLabel;
            if (consumableSectionLabelText != null) consumableSectionLabelText.text = consumableLabel;
            if (potionSectionLabelText != null) potionSectionLabelText.text = potionLabel;
            if (featuredLabelText != null) featuredLabelText.text = featuredLabel;
        }

        public void SetLeaveLabel(string text)
        {
            var label = leaveButton != null ? leaveButton.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (label != null) label.text = text;
        }

        public void SetCurrencyText(string text)
        {
            if (currencyText != null) currencyText.text = text;
        }

        public void SetDiscountBanner(string text)
        {
            if (discountBannerText == null) return;
            discountBannerText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            discountBannerText.text = text;
        }

        public void SetLegendaryGlow(bool active)
        {
            if (legendaryShopGlow != null) legendaryShopGlow.SetActive(active);
        }
    }
}
