using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterPassiveRowView : MonoBehaviour
    {
        [Header("Passive Info")]
        [SerializeField] private TextMeshProUGUI passiveNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI currentEffectText;
        [SerializeField] private TextMeshProUGUI nextEffectText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("Actions")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;

        [Header("No Passive")]
        [SerializeField] private GameObject noPassiveLabel;

        public event Action OnUpgradeClicked;

        private void Start()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke());
        }

        public void SetNoPassive()
        {
            SetPassiveContentActive(false);
            if (noPassiveLabel != null) noPassiveLabel.SetActive(true);
        }

        public void SetPassiveData(
            string passiveName,
            int currentLevel,
            int maxLevel,
            string currentEffectDesc,
            string nextEffectDesc,
            int upgradeCost,
            bool canAfford)
        {
            SetPassiveContentActive(true);
            if (noPassiveLabel != null) noPassiveLabel.SetActive(false);

            if (passiveNameText  != null) passiveNameText.text  = passiveName;
            if (levelText        != null) levelText.text        = $"Lv {currentLevel} / {maxLevel}";
            if (currentEffectText!= null) currentEffectText.text= currentEffectDesc;
            if (nextEffectText   != null) nextEffectText.text   = nextEffectDesc;

            bool isMaxLevel = currentLevel >= maxLevel;
            string costDisplay = isMaxLevel ? "MAX" : (LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetFormatted("go_will_cost", upgradeCost)
                : upgradeCost.ToString());

            if (costText != null) costText.text = costDisplay;

            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(!isMaxLevel);
                upgradeButton.interactable = canAfford;
            }
            if (upgradeButtonText != null && LocalizationManager.Instance != null)
                upgradeButtonText.text = LocalizationManager.Instance.GetTranslation("upgrade_passive_btn") ?? "강화";
        }

        private void SetPassiveContentActive(bool active)
        {
            if (passiveNameText  != null) passiveNameText.gameObject.SetActive(active);
            if (levelText        != null) levelText.gameObject.SetActive(active);
            if (currentEffectText!= null) currentEffectText.gameObject.SetActive(active);
            if (nextEffectText   != null) nextEffectText.gameObject.SetActive(active);
            if (costText         != null) costText.gameObject.SetActive(active);
            if (upgradeButton    != null) upgradeButton.gameObject.SetActive(active);
        }
    }
}
