using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class ItemDetailPanelView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemQuantityText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;
        [SerializeField] private Button closeButton;

        public event Action OnUseClicked;
        public event Action OnDropClicked;
        public event Action OnCloseClicked;

        private void Awake()
        {
            if (useButton != null)
            {
                useButton.onClick.AddListener(() => OnUseClicked?.Invoke());
            }
            if (dropButton != null)
            {
                dropButton.onClick.AddListener(() => OnDropClicked?.Invoke());
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetItem(InventorySlotData slotData, Sprite iconSprite)
        {
            if (slotData == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            // Icon
            if (itemIcon != null)
            {
                itemIcon.sprite = iconSprite;
                itemIcon.gameObject.SetActive(iconSprite != null);
            }

            // Translation support
            string translatedName = LocalizationManager.Instance.GetTranslation(slotData.Item.NameKey);
            string translatedDesc = LocalizationManager.Instance.GetTranslation(slotData.Item.DescriptionKey);

            if (itemNameText != null)
            {
                itemNameText.text = translatedName;
                itemNameText.color = GetRarityColor(slotData.Item.Rarity);
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = translatedDesc;
            }

            if (itemQuantityText != null)
            {
                // Format quantity string
                itemQuantityText.text = $"{LocalizationManager.Instance.GetTranslation("hud_quantity_label") ?? "Quantity:"} {slotData.Quantity}";
            }

            // Enable/Disable UseButton based on ItemType
            if (useButton != null)
            {
                useButton.gameObject.SetActive(true);
                useButton.interactable = (slotData.Item.Type == ItemType.Consumable);
                // Adjust text or transparency to denote disabled state clearly
                var btnText = useButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.color = useButton.interactable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                }
            }

            // DropButton is always interactable for items (per drop requirements, unless special?)
            if (dropButton != null)
            {
                dropButton.gameObject.SetActive(true);
                dropButton.interactable = true;
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return new Color(0.2f, 0.6f, 1.0f); // Bright blue
                case ItemRarity.Epic:
                    return new Color(0.75f, 0.25f, 0.9f); // Bright purple
                case ItemRarity.Legendary:
                    return new Color(1.0f, 0.85f, 0.2f); // Golden yellow
                case ItemRarity.Common:
                default:
                    return Color.white;
            }
        }
    }
}
