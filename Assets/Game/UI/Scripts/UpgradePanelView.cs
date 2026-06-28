using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class UpgradePanelView : MonoBehaviour
    {
        [Header("Will Display")]
        [SerializeField] private TextMeshProUGUI willText;
        [SerializeField] private TextMeshProUGUI topRightGoldText;

        [Header("Mining Power Row")]
        [SerializeField] private TextMeshProUGUI miningPowerLevelText;
        [SerializeField] private TextMeshProUGUI miningPowerCostText;
        [SerializeField] private Button miningPowerButton;

        [Header("Max HP Row")]
        [SerializeField] private TextMeshProUGUI maxHPLevelText;
        [SerializeField] private TextMeshProUGUI maxHPCostText;
        [SerializeField] private Button maxHPButton;

        [Header("Inventory Row")]
        [SerializeField] private TextMeshProUGUI inventoryLevelText;
        [SerializeField] private TextMeshProUGUI inventoryCostText;
        [SerializeField] private Button inventoryButton;

        [Header("Sub Views")]
        [SerializeField] public CharacterSelectorView characterSelectorView;
        [SerializeField] public CharacterPassiveRowView characterPassiveRowView;

        public event Action<UpgradeType> OnUpgradeClicked;

        private void Start()
        {
            if (miningPowerButton != null)
                miningPowerButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.MiningPower));
            if (maxHPButton != null)
                maxHPButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.MaxHP));
            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(UpgradeType.InventorySize));
        }

        public void SetWill(int will)
        {
            if (LocalizationManager.Instance == null) return;
            if (willText != null)
                willText.text = LocalizationManager.Instance.GetFormatted("menu_will", will);
            if (topRightGoldText != null)
                topRightGoldText.text = LocalizationManager.Instance.GetFormatted("menu_gold_topright", will);
        }

        public void SetUpgradeState(UpgradeType type, int level, int cost, bool canAfford)
        {
            if (LocalizationManager.Instance == null) return;

            string levelKey = type switch
            {
                UpgradeType.MiningPower  => "menu_upgrade_power",
                UpgradeType.MaxHP        => "menu_upgrade_hp",
                UpgradeType.InventorySize => "menu_upgrade_inventory",
                _                        => "menu_upgrade_attack"
            };

            string levelStr = LocalizationManager.Instance.GetFormatted(levelKey, level);
            string costStr  = cost == int.MaxValue ? "MAX" : LocalizationManager.Instance.GetFormatted("go_will_cost", cost);

            switch (type)
            {
                case UpgradeType.MiningPower:
                    if (miningPowerLevelText != null) miningPowerLevelText.text = levelStr;
                    if (miningPowerCostText  != null) miningPowerCostText.text  = costStr;
                    if (miningPowerButton    != null) miningPowerButton.interactable = canAfford;
                    break;
                case UpgradeType.MaxHP:
                    if (maxHPLevelText != null) maxHPLevelText.text = levelStr;
                    if (maxHPCostText  != null) maxHPCostText.text  = costStr;
                    if (maxHPButton    != null) maxHPButton.interactable = canAfford;
                    break;
                case UpgradeType.InventorySize:
                    if (inventoryLevelText != null) inventoryLevelText.text = levelStr;
                    if (inventoryCostText  != null) inventoryCostText.text  = costStr;
                    if (inventoryButton    != null) inventoryButton.interactable = cost != int.MaxValue && canAfford;
                    break;
            }
        }
    }
}
