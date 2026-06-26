using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class LoadingPanelView : MonoBehaviour
    {
        [SerializeField] private Image           progressBarFill;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private RectTransform   loadingIcon;

        private const float SpinSpeed = 180f;

        private void Update()
        {
            if (loadingIcon != null)
                loadingIcon.Rotate(0f, 0f, -SpinSpeed * Time.deltaTime);
        }

        public void SetProgress(float normalizedValue, string text)
        {
            if (progressBarFill != null)
                progressBarFill.fillAmount = Mathf.Clamp01(normalizedValue);
            if (loadingText != null)
                loadingText.text = text;
        }

        public void SetTip(string tip)
        {
            if (tipText != null)
                tipText.text = tip;
        }
    }
}
