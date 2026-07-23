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
        [SerializeField] private Button discardAllButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI autoUseText;

        public event Action OnUseClicked;
        public event Action OnDropClicked;
        public event Action OnDiscardAllClicked;
        public event Action OnCloseClicked;

        private void Awake()
        {
            if (useButton != null)
                useButton.onClick.AddListener(() => OnUseClicked?.Invoke());
            if (dropButton != null)
                dropButton.onClick.AddListener(() => OnDropClicked?.Invoke());
            if (discardAllButton != null)
                discardAllButton.onClick.AddListener(() => OnDiscardAllClicked?.Invoke());
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetItem(InventorySlotModel slotModel, Sprite iconSprite, bool useEnabled = true)
        {
            if (slotModel == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            if (itemIcon != null)
            {
                itemIcon.sprite = iconSprite;
                itemIcon.gameObject.SetActive(iconSprite != null);
            }

            string translatedName = LocalizationManager.Instance.GetTranslation(slotModel.Item.nameLocKey);
            string translatedDesc = LocalizationManager.Instance.GetTranslation(slotModel.Item.descLocKey);

            if (itemNameText != null)
            {
                itemNameText.text = translatedName;
                itemNameText.color = GetRarityColor(slotModel.Item.rarity);
            }

            if (itemDescriptionText != null)
                itemDescriptionText.text = translatedDesc;

            if (itemQuantityText != null)
                itemQuantityText.text = $"{slotModel.Count}";

            bool isConsumable = slotModel.Item.type == ItemType.Consumable;
            bool isAutoUseOnly = slotModel.Item.autoUseOnDeath;

            if (useButton != null)
            {
                useButton.gameObject.SetActive(isConsumable && !isAutoUseOnly);
                useButton.interactable = useEnabled;
            }

            if (autoUseText != null)
            {
                autoUseText.gameObject.SetActive(isConsumable && isAutoUseOnly);
                if (isAutoUseOnly)
                    autoUseText.text = LocalizationManager.Instance.GetTranslation("item_auto_use");
            }

            if (dropButton != null)
                dropButton.gameObject.SetActive(true);

            if (discardAllButton != null)
                discardAllButton.gameObject.SetActive(true);
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:      return new Color(0.2f, 0.6f,  1.0f);
                case ItemRarity.Epic:      return new Color(0.75f, 0.25f, 0.9f);
                case ItemRarity.Legendary: return new Color(1.0f, 0.85f, 0.2f);
                default:                   return Color.white;
            }
        }
    }
}
