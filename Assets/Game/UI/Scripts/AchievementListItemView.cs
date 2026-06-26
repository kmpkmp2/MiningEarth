using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class AchievementListItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject completeMark;
        [SerializeField] private CanvasGroup canvasGroup;

        private static readonly Color IncompleteColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        private static readonly Color CompleteColor   = new Color(0.09f, 0.19f, 0.09f, 0.95f);

        public void SetData(string name, string desc, string progress, bool isCompleted)
        {
            if (nameText)     nameText.text     = name;
            if (descText)     descText.text     = desc;
            if (progressText) progressText.text = progress;

            if (background)   background.color = isCompleted ? CompleteColor : IncompleteColor;
            if (completeMark) completeMark.SetActive(isCompleted);
            if (canvasGroup)  canvasGroup.alpha = isCompleted ? 0.55f : 1.0f;
        }

        // Creates an inactive template item ready for pooling.
        public static AchievementListItemView CreateTemplate(Transform poolParent)
        {
            var go = new GameObject("AchievementListItem", typeof(RectTransform));
            go.transform.SetParent(poolParent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 90f);

            var bg = go.AddComponent<Image>();
            bg.color = IncompleteColor;

            var cg = go.AddComponent<CanvasGroup>();

            // Name — top-left
            var nameTmp = AddTMP(go.transform, "Name",
                new Vector2(0f, 0.56f), new Vector2(0.72f, 1f),
                new Vector4(12f, 0f, -4f, -7f),
                17f, true, Color.white, TMPro.TextAlignmentOptions.Left);

            // Desc — middle-left
            var descTmp = AddTMP(go.transform, "Desc",
                new Vector2(0f, 0.22f), new Vector2(0.72f, 0.58f),
                new Vector4(12f, 0f, -4f, 0f),
                13f, false, new Color(0.78f, 0.78f, 0.78f), TMPro.TextAlignmentOptions.Left);

            // Progress — right side top
            var progTmp = AddTMP(go.transform, "Progress",
                new Vector2(0.72f, 0.42f), new Vector2(1f, 0.96f),
                new Vector4(0f, 0f, -10f, -4f),
                14f, false, new Color(0.72f, 0.72f, 0.72f), TMPro.TextAlignmentOptions.Right);

            // Complete mark — right side bottom
            var cmGo = new GameObject("CompleteMark", typeof(RectTransform));
            cmGo.transform.SetParent(go.transform, false);
            var cmRt = cmGo.GetComponent<RectTransform>();
            cmRt.anchorMin = new Vector2(0.72f, 0.05f);
            cmRt.anchorMax = new Vector2(1f, 0.45f);
            cmRt.offsetMin = new Vector2(0f, 2f);
            cmRt.offsetMax = new Vector2(-10f, 0f);
            var cmTmp = cmGo.AddComponent<TextMeshProUGUI>();
            cmTmp.text      = "COMPLETE";
            cmTmp.fontSize  = 13f;
            cmTmp.fontStyle = TMPro.FontStyles.Bold;
            cmTmp.color     = new Color(0.25f, 1f, 0.42f);
            cmTmp.alignment = TMPro.TextAlignmentOptions.Right;

            // Bottom separator
            var sepGo = new GameObject("Sep", typeof(RectTransform));
            sepGo.transform.SetParent(go.transform, false);
            var sepRt = sepGo.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0f);
            sepRt.anchorMax = new Vector2(1f, 0.04f);
            sepRt.offsetMin = Vector2.zero;
            sepRt.offsetMax = Vector2.zero;
            sepGo.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f, 0.6f);

            go.SetActive(false); // starts inactive in pool

            var view          = go.AddComponent<AchievementListItemView>();
            view.background   = bg;
            view.nameText     = nameTmp;
            view.descText     = descTmp;
            view.progressText = progTmp;
            view.completeMark = cmGo;
            view.canvasGroup  = cg;

            return view;
        }

        private static TextMeshProUGUI AddTMP(Transform parent, string goName,
            Vector2 anchorMin, Vector2 anchorMax, Vector4 offset,
            float size, bool bold, Color color, TMPro.TextAlignmentOptions align)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(offset.x, offset.y);
            rt.offsetMax = new Vector2(offset.z, offset.w);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = "";
            tmp.fontSize  = size;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            return tmp;
        }
    }
}
