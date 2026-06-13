using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class GameUIView : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI inventorySizeText;

        [Header("Settings")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private TextMeshProUGUI settingsButtonLabel;

        [Header("Relic / Effects Button")]
        [SerializeField] private Button relicButton;
        [SerializeField] private TextMeshProUGUI relicButtonLabel;

        [Header("Inventory Button")]
        [SerializeField] private Button inventoryButton;
        [SerializeField] private TextMeshProUGUI inventoryButtonLabel;

        [Header("Effect HUD Layout")]
        [SerializeField] private Transform effectIconContainer;

        public event Action OnSettingsClicked;
        public event Action OnRelicClicked;
        public event Action OnInventoryClicked;

        private void Start()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
            }

            if (relicButton != null)
            {
                relicButton.onClick.AddListener(() => OnRelicClicked?.Invoke());
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.AddListener(() => OnInventoryClicked?.Invoke());
            }
        }

        public void LocalizeSettingsButton(string text)
        {
            if (settingsButtonLabel != null)
            {
                settingsButtonLabel.text = text;
            }
        }

        public void LocalizeRelicButton(string text)
        {
            if (relicButtonLabel != null)
            {
                relicButtonLabel.text = text;
            }
        }

        public void LocalizeInventoryButton(string text)
        {
            if (inventoryButtonLabel != null)
            {
                inventoryButtonLabel.text = text;
            }
        }

        public Transform GetEffectIconContainer()
        {
            return effectIconContainer;
        }

        [Header("Resource Counters")]
        [SerializeField] private TextMeshProUGUI ironText;
        [SerializeField] private TextMeshProUGUI silverText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI diamondText;

        public void SetHP(int current, int max)
        {
            if (hpText != null)
            {
                hpText.text = LocalizationManager.Instance.GetFormatted("hud_hp", current, max);
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = current;
            }
        }

        public void SetDepth(int depth, string difficultyKey)
        {
            if (depthText != null)
            {
                depthText.text = LocalizationManager.Instance.GetFormatted("hud_depth", depth);
            }

            if (difficultyText != null)
            {
                difficultyText.text = LocalizationManager.Instance.GetTranslation(difficultyKey);
            }
        }

        public void SetInventorySize(int current, int max)
        {
            if (inventorySizeText != null)
            {
                inventorySizeText.text = LocalizationManager.Instance.GetFormatted("hud_bag", current, max);
            }
        }

        public void SetResources(int iron, int silver, int gold, int diamond)
        {
            if (ironText != null) ironText.text = LocalizationManager.Instance.GetFormatted("hud_iron", iron);
            if (silverText != null) silverText.text = LocalizationManager.Instance.GetFormatted("hud_silver", silver);
            if (goldText != null) goldText.text = LocalizationManager.Instance.GetFormatted("hud_gold", gold);
            if (diamondText != null) diamondText.text = LocalizationManager.Instance.GetFormatted("hud_diamond", diamond);
        }
    }
}
