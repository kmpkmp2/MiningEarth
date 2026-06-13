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

        public event Action OnCloseClicked;
        public event Action OnLanguageKoClicked;
        public event Action OnLanguageEnClicked;

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
        }

        public void Localize()
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Instance.GetTranslation("settings_title");
            }

            if (closeButtonLabel != null)
            {
                closeButtonLabel.text = LocalizationManager.Instance.GetTranslation("settings_close");
            }

            if (languageTabLabel != null)
            {
                languageTabLabel.text = LocalizationManager.Instance.GetTranslation("settings_lang_label");
            }
        }

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
