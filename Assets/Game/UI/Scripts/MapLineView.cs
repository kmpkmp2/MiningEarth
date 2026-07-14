using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    public class MapLineView : MonoBehaviour
    {
        [SerializeField] private Image lineImage;
        [SerializeField] private float lineThickness = 4f;

        public void Connect(Vector2 fromPos, Vector2 toPos, Color color)
        {
            if (lineImage != null)
                lineImage.color = color;

            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            Vector2 dir    = toPos - fromPos;
            float   length = dir.magnitude;
            if (length < 0.01f) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = (fromPos + toPos) * 0.5f;
            rt.sizeDelta        = new Vector2(length, lineThickness);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
