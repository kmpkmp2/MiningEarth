using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class EffectIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI stackText;
        [SerializeField] private TooltipTrigger tooltipTrigger;

        public TooltipTrigger Trigger => tooltipTrigger;

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
            }
        }

        public void SetStack(string text)
        {
            if (stackText != null)
            {
                stackText.text = text;
            }
        }
    }
}
