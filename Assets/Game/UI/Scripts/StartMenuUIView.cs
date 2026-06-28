using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private GameObject runSetupPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        [Header("Sub-Panel Back Buttons")]
        [SerializeField] private Button upgradeBackButton;
        [SerializeField] private Button shopBackButton;
        [SerializeField] private Button settingsBackButton;

        [Header("Upgrade Panel")]
        [SerializeField] private UpgradePanelView upgradePanelView;

        [Header("Settings UI Elements")]
        [SerializeField] private Button langKoButton;
        [SerializeField] private Button langEnButton;
        [SerializeField] private TextMeshProUGUI settingsTitleText;
        [SerializeField] private TextMeshProUGUI settingsLangLabelText;
        [SerializeField] private TextMeshProUGUI settingsCloseLabelText;

        [Header("RunSetup UI Elements")]
        [SerializeField] private RunSetupPanelView runSetupPanelView;

        [Header("Shop UI Elements")]
        [SerializeField] private TextMeshProUGUI shopTitleText;
        [SerializeField] private TextMeshProUGUI shopDescText;
        [SerializeField] private TextMeshProUGUI shopCloseLabelText;
        [SerializeField] private ShopPanelView   shopPanelView;

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

        [Header("Achievement UI Elements")]
        [SerializeField] private Button achievementButton;
        [SerializeField] private TextMeshProUGUI achievementButtonLabel;
        [SerializeField] private AchievementPopupView achievementPopupView;

        public UpgradePanelView     UpgradePanelView     => upgradePanelView;
        public CharacterPopupView   CharacterPopupView   => characterPopupView;
        public AchievementPopupView AchievementPopupView => achievementPopupView;
        public ShopPanelView        ShopPanelView        => shopPanelView;
        public RunSetupPanelView    RunSetupPanelView    => runSetupPanelView;
        public Transform            ShopPanelTransform   => shopPanel != null ? shopPanel.transform : null;

        // Events
        public event Action OnPlayClicked;
        public event Action OnUpgradeMenuClicked;
        public event Action OnShopClicked;
        public event Action OnSettingsClicked;
        public event Action OnBackClicked;
        public event Action OnLanguageKoClicked;
        public event Action OnLanguageEnClicked;
        public event Action OnCharacterMenuClicked;
        public event Action OnAchievementMenuClicked;

        private void Start()
        {
            if (playButton     != null) playButton.onClick.AddListener(()     => OnPlayClicked?.Invoke());
            if (upgradeButton  != null) upgradeButton.onClick.AddListener(()  => OnUpgradeMenuClicked?.Invoke());
            if (shopButton     != null) shopButton.onClick.AddListener(()     => OnShopClicked?.Invoke());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());

            if (upgradeBackButton  != null) upgradeBackButton.onClick.AddListener(()  => OnBackClicked?.Invoke());
            if (shopBackButton     != null) shopBackButton.onClick.AddListener(()     => OnBackClicked?.Invoke());
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            if (characterSelectionButton != null) characterSelectionButton.onClick.AddListener(() => OnCharacterMenuClicked?.Invoke());
            if (achievementButton        != null) achievementButton.onClick.AddListener(()        => OnAchievementMenuClicked?.Invoke());

            if (langKoButton != null) langKoButton.onClick.AddListener(() => OnLanguageKoClicked?.Invoke());
            if (langEnButton != null) langEnButton.onClick.AddListener(() => OnLanguageEnClicked?.Invoke());
        }

        public void ShowPanel(GameObject panelToShow)
        {
            if (menuPanel    != null) menuPanel.SetActive(menuPanel     == panelToShow);
            if (upgradePanel != null) upgradePanel.SetActive(upgradePanel == panelToShow);
            if (shopPanel    != null) shopPanel.SetActive(shopPanel     == panelToShow);
            if (settingsPanel!= null) settingsPanel.SetActive(settingsPanel == panelToShow);
            if (runSetupPanel!= null) runSetupPanel.SetActive(runSetupPanel == panelToShow);
        }

        public void ShowMainMenu()     => ShowPanel(menuPanel);
        public void ShowUpgradeMenu()  => ShowPanel(upgradePanel);
        public void ShowShopMenu()     => ShowPanel(shopPanel);
        public void ShowSettingsMenu() => ShowPanel(settingsPanel);
        public void ShowRunSetupPanel()=> ShowPanel(runSetupPanel);

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;
            var loc = LocalizationManager.Instance;

            if (menuTitleText      != null) menuTitleText.text      = loc.GetTranslation("menu_title");
            if (playButtonLabel    != null) playButtonLabel.text    = loc.GetTranslation("menu_play");
            if (upgradeButtonLabel != null) upgradeButtonLabel.text = loc.GetTranslation("menu_upgrade");
            if (shopButtonLabel    != null) shopButtonLabel.text    = loc.GetTranslation("menu_shop");
            if (settingsButtonLabel!= null) settingsButtonLabel.text= loc.GetTranslation("menu_settings");

            string backText = loc.GetTranslation("menu_back");
            if (upgradeBackButtonLabel != null) upgradeBackButtonLabel.text = backText;
            if (shopBackButtonLabel    != null) shopBackButtonLabel.text    = backText;
            if (settingsCloseLabelText != null) settingsCloseLabelText.text = backText;

            if (settingsTitleText    != null) settingsTitleText.text    = loc.GetTranslation("settings_title");
            if (settingsLangLabelText!= null) settingsLangLabelText.text= loc.GetTranslation("settings_lang_label");

            if (shopTitleText != null) shopTitleText.text = loc.GetTranslation("menu_shop");
            if (shopDescText != null)
            {
                bool hasShop = shopPanelView != null
                            || (ShopPanelTransform != null && ShopPanelTransform.Find("ShopPanelView") != null);
                shopDescText.gameObject.SetActive(!hasShop);
                if (!hasShop) shopDescText.text = loc.GetTranslation("menu_shop_coming_soon");
            }
            if (shopCloseLabelText != null) shopCloseLabelText.text = backText;

            if (characterButtonLabel  != null) characterButtonLabel.text  = loc.GetTranslation("char_btn_open");
            if (achievementButtonLabel!= null) achievementButtonLabel.text= loc.GetTranslation("achievement_btn_open");
        }

        public void UpdateLanguageVisuals(string languageCode)
        {
            if (langKoButton != null)
                langKoButton.GetComponent<Image>().color = (languageCode == "ko") ? Color.green : new Color(0.2f, 0.22f, 0.25f);
            if (langEnButton != null)
                langEnButton.GetComponent<Image>().color = (languageCode == "en") ? Color.green : new Color(0.2f, 0.22f, 0.25f);
        }
    }
}
