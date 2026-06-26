using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // Prefab 기반 슬롯 — Addressables에서 로드 후 Instantiate로 생성
    public class ShopItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image           background;
        [SerializeField] private Image           iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI stat1Text;
        [SerializeField] private TextMeshProUGUI stat2Text;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button          actionButton;
        [SerializeField] private TextMeshProUGUI actionLabel;
        [SerializeField] private Image           selectionHighlight;

        private static readonly Color LockedBg    = new Color(0.10f, 0.10f, 0.14f, 0.95f);
        private static readonly Color UnlockedBg  = new Color(0.10f, 0.18f, 0.10f, 0.95f);
        private static readonly Color EquippedBg  = new Color(0.08f, 0.22f, 0.08f, 0.97f);
        private static readonly Color BtnBuy      = new Color(0.12f, 0.26f, 0.52f, 1f);
        private static readonly Color BtnDisabled = new Color(0.20f, 0.20f, 0.25f, 1f);
        private static readonly Color BtnEquip    = new Color(0.13f, 0.42f, 0.13f, 1f);
        private static readonly Color BtnEquipped = new Color(0.22f, 0.22f, 0.28f, 1f);

        public event Action<ShopItemSlotView> OnSelected;
        public event Action<ShopItemSlotView> OnActionClicked;

        public ShopItemDisplayData DisplayData { get; private set; }

        private void Awake()
        {
            var slotBtn = GetComponent<Button>();
            if (slotBtn != null)
                slotBtn.onClick.AddListener(() => OnSelected?.Invoke(this));
        }

        public void SetData(ShopItemDisplayData data)
        {
            DisplayData = data;

            if (nameText)  nameText.text  = data.name;
            if (stat1Text) stat1Text.text = data.stat1Text;
            if (stat2Text) stat2Text.text = data.stat2Text;
            if (costText)  costText.text  = data.isUnlocked ? "" : data.costText;

            if (background)
                background.color = data.isEquipped ? EquippedBg
                                 : data.isUnlocked  ? UnlockedBg
                                 : LockedBg;

            if (selectionHighlight) selectionHighlight.gameObject.SetActive(false);

            if (actionButton == null || actionLabel == null) return;

            actionButton.onClick.RemoveAllListeners();
            var loc = LocalizationManager.Instance;

            if (data.isEquipped)
            {
                actionLabel.text = loc?.GetTranslation("shop_pickaxe_equipped") ?? "EQUIPPED";
                actionButton.GetComponent<Image>().color = BtnEquipped;
                actionButton.interactable = false;
            }
            else if (data.isUnlocked)
            {
                actionLabel.text = loc?.GetTranslation("shop_pickaxe_equip") ?? "EQUIP";
                actionButton.GetComponent<Image>().color = BtnEquip;
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => OnActionClicked?.Invoke(this));
            }
            else
            {
                actionLabel.text = loc?.GetTranslation("shop_pickaxe_buy") ?? "BUY";
                actionButton.GetComponent<Image>().color = data.canAfford ? BtnBuy : BtnDisabled;
                actionButton.interactable = data.canAfford;
                if (data.canAfford)
                    actionButton.onClick.AddListener(() => OnActionClicked?.Invoke(this));
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight) selectionHighlight.gameObject.SetActive(selected);
        }
    }
}
