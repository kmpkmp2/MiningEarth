using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class SettingsUIView : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI closeButtonLabel;

        [Header("Language Tabs")]
        [SerializeField] private TextMeshProUGUI languageTabLabel;
        [SerializeField] private Button languageKoButton;
        [SerializeField] private Button languageEnButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Exit Run")]
        [SerializeField] private GameObject exitRunRow;
        [SerializeField] private Button exitRunButton;
        [SerializeField] private TextMeshProUGUI exitRunButtonLabel;
        [SerializeField] private ExitRunConfirmPopupView exitRunConfirmPopup;

        public event Action OnCloseClicked;
        public event Action OnLanguageKoClicked;
        public event Action OnLanguageEnClicked;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnBGMVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        public event Action OnExitRunClicked;

        private static readonly Color SelectedColor = new Color(0.12f, 0.46f, 0.61f, 1f); // Premium cyan
        private static readonly Color UnselectedColor = new Color(0.2f, 0.22f, 0.25f, 1f); // Sleek dark gray

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            }

            if (languageKoButton != null)
            {
                languageKoButton.onClick.AddListener(() => OnLanguageKoClicked?.Invoke());
            }

            if (languageEnButton != null)
            {
                languageEnButton.onClick.AddListener(() => OnLanguageEnClicked?.Invoke());
            }

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(v => OnMasterVolumeChanged?.Invoke(v));
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.AddListener(v => OnBGMVolumeChanged?.Invoke(v));
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(v => OnSFXVolumeChanged?.Invoke(v));

            if (exitRunButton != null)
                exitRunButton.onClick.AddListener(() => OnExitRunClicked?.Invoke());
        }

        public void InitSliders(float master, float bgm, float sfx)
        {
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(master);
            if (bgmVolumeSlider != null)    bgmVolumeSlider.SetValueWithoutNotify(bgm);
            if (sfxVolumeSlider != null)    sfxVolumeSlider.SetValueWithoutNotify(sfx);
        }

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;

            if (titleText != null)
                titleText.text = LocalizationManager.Instance.GetTranslation("settings_title");

            if (closeButtonLabel != null)
                closeButtonLabel.text = LocalizationManager.Instance.GetTranslation("settings_close");

            if (languageTabLabel != null)
                languageTabLabel.text = LocalizationManager.Instance.GetTranslation("settings_lang_label");

            if (exitRunButtonLabel != null)
                exitRunButtonLabel.text = LocalizationManager.Instance.GetTranslation("exit_run_btn");

            exitRunConfirmPopup?.Localize();
        }

        public void SetExitRunVisible(bool visible)
        {
            exitRunRow?.SetActive(visible);
        }

        public ExitRunConfirmPopupView GetConfirmPopup() => exitRunConfirmPopup;

        public void UpdateVisuals(string currentLang)
        {
            if (languageKoButton != null)
            {
                bool isKo = currentLang == "ko";
                languageKoButton.interactable = !isKo;
                var img = languageKoButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isKo ? SelectedColor : UnselectedColor;
                }
            }

            if (languageEnButton != null)
            {
                bool isEn = currentLang == "en";
                languageEnButton.interactable = !isEn;
                var img = languageEnButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isEn ? SelectedColor : UnselectedColor;
                }
            }
        }
    }
}
