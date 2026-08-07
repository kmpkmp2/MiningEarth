using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 유물/소비아이템/포션/추천상품 슬롯 공용 View. 로직 없이 데이터 표시와 클릭 이벤트 전달만 담당.
    public class MerchantItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyLabelText;
        [SerializeField] private GameObject soldOutOverlay;
        [SerializeField] private GameObject legendaryGlow;

        private static readonly Color CommonColor = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color RareColor = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color UniqueColor = new Color(0.64f, 0.21f, 0.93f);
        private static readonly Color LegendaryColor = new Color(1.0f, 0.8f, 0.15f);
        private static readonly Color BtnBuy = new Color(0.12f, 0.26f, 0.52f, 1f);
        private static readonly Color BtnDisabled = new Color(0.20f, 0.20f, 0.25f, 1f);

        public event Action OnBuyClicked;

        public MerchantSlotData SlotData { get; private set; }

        private void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke());
        }

        public void SetData(MerchantSlotData slot, bool canAfford, string name, string desc, string priceLabel)
        {
            SlotData = slot;

            if (nameText != null) nameText.text = name;
            if (descText != null) descText.text = desc;
            if (priceText != null) priceText.text = slot.soldOut ? string.Empty : priceLabel;

            Color rarityColor = GetRarityColor(slot);
            if (borderImage != null) borderImage.color = rarityColor;
            if (legendaryGlow != null) legendaryGlow.SetActive(slot.IsLegendary && !slot.soldOut);

            if (soldOutOverlay != null) soldOutOverlay.SetActive(slot.soldOut);

            if (buyButton == null) return;

            buyButton.gameObject.SetActive(!slot.soldOut);
            buyButton.interactable = !slot.soldOut && canAfford;
            var buttonImage = buyButton.GetComponent<Image>();
            if (buttonImage != null) buttonImage.color = (!slot.soldOut && canAfford) ? BtnBuy : BtnDisabled;
            if (buyLabelText != null) buyLabelText.text = LocalizationManager.Instance.GetTranslation("shop_pickaxe_buy");
        }

        private static Color GetRarityColor(MerchantSlotData slot)
        {
            if (slot.relic != null)
            {
                return slot.relic.rarity switch
                {
                    RelicRarity.Rare => RareColor,
                    RelicRarity.Unique => UniqueColor,
                    RelicRarity.Legendary => LegendaryColor,
                    _ => CommonColor,
                };
            }
            if (slot.item != null)
            {
                return slot.item.rarity switch
                {
                    ItemRarity.Rare => RareColor,
                    ItemRarity.Epic => RareColor,
                    ItemRarity.Legendary => LegendaryColor,
                    _ => CommonColor,
                };
            }
            return CommonColor;
        }
    }
}
