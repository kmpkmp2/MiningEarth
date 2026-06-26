using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    public class ShopContentView : MonoBehaviour
    {
        [SerializeField] private ScrollRect    scrollRect;
        [SerializeField] private RectTransform content;

        public Transform Content => content;

        public void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        public void ScrollToTop()
        {
            if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
