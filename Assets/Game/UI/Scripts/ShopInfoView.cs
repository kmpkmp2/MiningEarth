using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class ShopInfoView : MonoBehaviour
    {
        [SerializeField] private Image           iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI stat1Text;
        [SerializeField] private TextMeshProUGUI stat2Text;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject      placeholderGroup;

        public void Show(ShopItemDisplayData data)
        {
            if (placeholderGroup) placeholderGroup.SetActive(false);

            var loc = LocalizationManager.Instance;
            if (nameText)  nameText.text  = data.name;
            if (descText)  descText.text  = data.description;
            if (stat1Text) stat1Text.text = data.stat1Text;
            if (stat2Text) stat2Text.text = data.stat2Text;
            if (costText)  costText.text  = data.isUnlocked ? "" : data.costText;

            if (statusText)
            {
                if (data.isUnlocked)
                {
                    statusText.text  = loc?.GetTranslation("shop_owned") ?? "보유중";
                    statusText.color = new Color(0.5f, 0.8f, 0.5f);
                }
                else
                {
                    statusText.text = "";
                }
            }
        }

        public void ShowPlaceholder()
        {
            if (placeholderGroup) placeholderGroup.SetActive(true);
            if (nameText)   nameText.text   = "";
            if (descText)   descText.text   = "";
            if (stat1Text)  stat1Text.text  = "";
            if (stat2Text)  stat2Text.text  = "";
            if (costText)   costText.text   = "";
            if (statusText) statusText.text = "";
        }

        private static string GetLabel(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
