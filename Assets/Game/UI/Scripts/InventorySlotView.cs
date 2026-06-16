using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class InventorySlotView : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image borderOutline;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private Button selectButton;

        public event Action OnClicked;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => OnClicked?.Invoke());
            }
        }

        public void SetData(InventorySlotData slotData, Sprite iconSprite)
        {
            gameObject.SetActive(true);

            if (slotData == null)
            {
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(false);
                }
                if (quantityText != null)
                {
                    quantityText.gameObject.SetActive(false);
                }
                if (itemNameText != null)
                {
                    itemNameText.text = "";
                }
                if (borderOutline != null)
                {
                    borderOutline.color = GetRarityColor(ItemRarity.Common);
                }
                return;
            }

            // Icon
            if (itemIcon != null)
            {
                itemIcon.sprite = iconSprite;
                itemIcon.gameObject.SetActive(iconSprite != null);
            }

            // Quantity
            if (quantityText != null)
            {
                quantityText.text = $"x{slotData.Quantity}";
                quantityText.gameObject.SetActive(slotData.Quantity > 0);
            }

            // Name
            if (itemNameText != null)
            {
                itemNameText.text = LocalizationManager.Instance.GetTranslation(slotData.Item.NameKey) ?? slotData.Item.Id;
            }

            // Rarity Border Color
            if (borderOutline != null)
            {
                borderOutline.color = GetRarityColor(slotData.Item.Rarity);
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return new Color(0.12f, 0.45f, 0.85f, 1f); // Blue
                case ItemRarity.Epic:
                    return new Color(0.6f, 0.15f, 0.75f, 1f); // Purple
                case ItemRarity.Legendary:
                    return new Color(1.0f, 0.8f, 0.15f, 1f); // Gold
                case ItemRarity.Common:
                default:
                    return new Color(0.4f, 0.42f, 0.45f, 1f); // Default Slate grey
            }
        }
    }
}
