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

        public void SetData(InventorySlotModel slotModel, Sprite iconSprite)
        {
            gameObject.SetActive(true);

            if (slotModel == null)
            {
                if (itemIcon != null) itemIcon.gameObject.SetActive(false);
                if (quantityText != null) quantityText.gameObject.SetActive(false);
                if (itemNameText != null) itemNameText.text = "";
                if (borderOutline != null) borderOutline.color = GetRarityColor(ItemRarity.Common);
                return;
            }

            if (itemIcon != null)
            {
                itemIcon.sprite = iconSprite;
                itemIcon.gameObject.SetActive(iconSprite != null);
            }

            if (quantityText != null)
            {
                quantityText.text = $"x{slotModel.Count}";
                quantityText.gameObject.SetActive(slotModel.Count > 0);
            }

            if (itemNameText != null)
            {
                itemNameText.text = LocalizationManager.Instance.GetTranslation(slotModel.Item.NameKey) ?? slotModel.ItemID;
            }

            if (borderOutline != null)
            {
                borderOutline.color = GetRarityColor(slotModel.Item.Rarity);
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:      return new Color(0.12f, 0.45f, 0.85f, 1f);
                case ItemRarity.Epic:      return new Color(0.6f,  0.15f, 0.75f, 1f);
                case ItemRarity.Legendary: return new Color(1.0f,  0.8f,  0.15f, 1f);
                default:                   return new Color(0.4f,  0.42f, 0.45f, 1f);
            }
        }
    }
}
