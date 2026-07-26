using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    // 상인 초상/이름/대사 표시 전용 View.
    public class MerchantQuoteView : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI quoteText;

        public void SetName(string name)
        {
            if (nameText != null) nameText.text = name;
        }

        public void SetQuote(string quote)
        {
            if (quoteText != null) quoteText.text = quote;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null) return;
            portraitImage.sprite = sprite;
            portraitImage.gameObject.SetActive(sprite != null);
        }
    }
}
