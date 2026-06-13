using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class StartMenuUIView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        [Header("Sub-Panel Back Buttons")]
        [SerializeField] private Button upgradeBackButton;
        [SerializeField] private Button shopBackButton;
        [SerializeField] private Button settingsBackButton;

        [Header("Upgrade UI Elements")]
        [SerializeField] private TextMeshProUGUI willText;
        [SerializeField] private TextMeshProUGUI topRightGoldText;
        [SerializeField] private TextMeshProUGUI miningPowerLevelText;
        [SerializeField] private TextMeshProUGUI miningPowerCostText;
        [SerializeField] private Button miningPowerButton;
        [SerializeField] private TextMeshProUGUI maxHPLevelText;
        [SerializeField] private TextMeshProUGUI maxHPCostText;
        [SerializeField] private Button maxHPButton;
        [SerializeField] private TextMeshProUGUI inventoryLevelText;
        [SerializeField] private TextMeshProUGUI inventoryCostText;
        [SerializeField] private Button inventoryButton;

        [Header("Settings UI Elements")]
        [SerializeField] private Button langKoButton;
        [SerializeField] private Button langEnButton;
        [SerializeField] private TextMeshProUGUI settingsTitleText;
        [SerializeField] private TextMeshProUGUI settingsLangLabelText;
        [SerializeField] private TextMeshProUGUI settingsCloseLabelText;

        [Header("Shop UI Elements")]
        [SerializeField] private TextMeshProUGUI shopTitleText;
        [SerializeField] private TextMeshProUGUI shopDescText;
        [SerializeField] private TextMeshProUGUI shopCloseLabelText;

        [Header("Menu Text Labels")]
        [SerializeField] private TextMeshProUGUI menuTitleText;
        [SerializeField] private TextMeshProUGUI playButtonLabel;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabel;
        [SerializeField] private TextMeshProUGUI shopButtonLabel;
        [SerializeField] private TextMeshProUGUI settingsButtonLabel;
        [SerializeField] private TextMeshProUGUI upgradeBackButtonLabel;
        [SerializeField] private TextMeshProUGUI shopBackButtonLabel;

        [Header("Character Selection UI Elements")]
        [SerializeField] private Button characterSelectionButton;
        [SerializeField] private TextMeshProUGUI characterButtonLabel;
        [SerializeField] private CharacterPopupView characterPopupView;

        public CharacterPopupView CharacterPopupView => characterPopupView;

        // Events
        public event Action OnPlayClicked;
        public event Action OnUpgradeMenuClicked;
        public event Action OnShopClicked;
        public event Action OnSettingsClicked;
        public event Action OnBackClicked;
        public event Action<UpgradeType> OnUpgradeStatClicked;
        public event Action OnLanguageKoClicked;
        public event Action OnLanguageEnClicked;
        public event Action OnCharacterMenuClicked;

        private void Start()
        {
            // Bind core menu buttons
            if (playButton != null) playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
            if (upgradeButton != null) upgradeButton.onClick.AddListener(() => OnUpgradeMenuClicked?.Invoke());
            if (shopButton != null) shopButton.onClick.AddListener(() => OnShopClicked?.Invoke());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());

            // Bind back buttons
            if (upgradeBackButton != null) upgradeBackButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            if (shopBackButton != null) shopBackButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            // Bind upgrade stat buttons
            if (miningPowerButton != null) miningPowerButton.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke(UpgradeType.MiningPower));
            if (maxHPButton != null) maxHPButton.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke(UpgradeType.MaxHP));
            if (inventoryButton != null) inventoryButton.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke(UpgradeType.Attack));

            // Bind character menu click
            if (characterSelectionButton != null) characterSelectionButton.onClick.AddListener(() => OnCharacterMenuClicked?.Invoke());

            // Bind settings language buttons
            if (langKoButton != null) langKoButton.onClick.AddListener(() => OnLanguageKoClicked?.Invoke());
            if (langEnButton != null) langEnButton.onClick.AddListener(() => OnLanguageEnClicked?.Invoke());
        }

        public void ShowPanel(GameObject panelToShow)
        {
            if (menuPanel != null) menuPanel.SetActive(menuPanel == panelToShow);
            if (upgradePanel != null) upgradePanel.SetActive(upgradePanel == panelToShow);
            if (shopPanel != null) shopPanel.SetActive(shopPanel == panelToShow);
            if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == panelToShow);
        }

        public void ShowMainMenu() => ShowPanel(menuPanel);
        public void ShowUpgradeMenu() => ShowPanel(upgradePanel);
        public void ShowShopMenu() => ShowPanel(shopPanel);
        public void ShowSettingsMenu() => ShowPanel(settingsPanel);

        public void SetWill(int will)
        {
            if (willText != null && LocalizationManager.Instance != null)
            {
                willText.text = LocalizationManager.Instance.GetFormatted("menu_will", will);
            }
            if (topRightGoldText != null && LocalizationManager.Instance != null)
            {
                topRightGoldText.text = LocalizationManager.Instance.GetFormatted("menu_gold_topright", will);
            }
        }

        public void SetUpgradeState(UpgradeType type, int level, int cost, bool canAfford)
        {
            if (LocalizationManager.Instance == null) return;

            string levelStr = LocalizationManager.Instance.GetFormatted(
                type == UpgradeType.MiningPower ? "menu_upgrade_power" :
                type == UpgradeType.MaxHP ? "menu_upgrade_hp" : "menu_upgrade_attack", level);
            string costStr = LocalizationManager.Instance.GetFormatted("go_will_cost", cost);

            switch (type)
            {
                case UpgradeType.MiningPower:
                    if (miningPowerLevelText != null) miningPowerLevelText.text = levelStr;
                    if (miningPowerCostText != null) miningPowerCostText.text = costStr;
                    if (miningPowerButton != null) miningPowerButton.interactable = canAfford;
                    break;
                case UpgradeType.MaxHP:
                    if (maxHPLevelText != null) maxHPLevelText.text = levelStr;
                    if (maxHPCostText != null) maxHPCostText.text = costStr;
                    if (maxHPButton != null) maxHPButton.interactable = canAfford;
                    break;
                case UpgradeType.Attack:
                    if (inventoryLevelText != null) inventoryLevelText.text = levelStr;
                    if (inventoryCostText != null) inventoryCostText.text = costStr;
                    if (inventoryButton != null) inventoryButton.interactable = canAfford;
                    break;
            }
        }

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;

            var loc = LocalizationManager.Instance;

            if (topRightGoldText != null && MetaProgressionManager.Instance != null)
            {
                topRightGoldText.text = loc.GetFormatted("menu_gold_topright", MetaProgressionManager.Instance.Will);
            }
            if (willText != null && MetaProgressionManager.Instance != null)
            {
                willText.text = loc.GetFormatted("menu_will", MetaProgressionManager.Instance.Will);
            }

            if (menuTitleText != null) menuTitleText.text = loc.GetTranslation("menu_title");
            if (playButtonLabel != null) playButtonLabel.text = loc.GetTranslation("menu_play");
            if (upgradeButtonLabel != null) upgradeButtonLabel.text = loc.GetTranslation("menu_upgrade");
            if (shopButtonLabel != null) shopButtonLabel.text = loc.GetTranslation("menu_shop");
            if (settingsButtonLabel != null) settingsButtonLabel.text = loc.GetTranslation("menu_settings");

            string backText = loc.GetTranslation("menu_back");
            if (upgradeBackButtonLabel != null) upgradeBackButtonLabel.text = backText;
            if (shopBackButtonLabel != null) shopBackButtonLabel.text = backText;
            if (settingsCloseLabelText != null) settingsCloseLabelText.text = backText;

            if (settingsTitleText != null) settingsTitleText.text = loc.GetTranslation("settings_title");
            if (settingsLangLabelText != null) settingsLangLabelText.text = loc.GetTranslation("settings_lang_label");

            if (shopTitleText != null) shopTitleText.text = loc.GetTranslation("menu_shop");
            if (shopDescText != null) shopDescText.text = loc.GetTranslation("menu_shop_coming_soon");
            if (shopCloseLabelText != null) shopCloseLabelText.text = backText;

            if (characterButtonLabel != null) characterButtonLabel.text = loc.GetTranslation("char_btn_open");
        }

        public void UpdateLanguageVisuals(string languageCode)
        {
            if (langKoButton != null)
            {
                langKoButton.GetComponent<Image>().color = (languageCode == "ko") ? Color.green : new Color(0.2f, 0.22f, 0.25f);
            }
            if (langEnButton != null)
            {
                langEnButton.GetComponent<Image>().color = (languageCode == "en") ? Color.green : new Color(0.2f, 0.22f, 0.25f);
            }
        }
    }
}
