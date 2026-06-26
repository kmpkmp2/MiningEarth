using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class AchievementPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentParent;
        [SerializeField] private Transform poolRoot;

        private readonly Queue<AchievementListItemView> _pool = new Queue<AchievementListItemView>();
        private readonly List<AchievementListItemView> _activeItems = new List<AchievementListItemView>();
        private bool _initialized;

        public event Action OnCloseClicked;
        public event Action<Vector2> OnScrollValueChanged;

        private void Start() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            if (closeButton) closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            if (scrollRect)  scrollRect.onValueChanged.AddListener(pos => OnScrollValueChanged?.Invoke(pos));
        }

        // ── Factory ──────────────────────────────────────────────────────────
        public static AchievementPopupView Create(Transform canvasParent)
        {
            // Full-screen backdrop
            var root = new GameObject("AchievementPopup", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            // Panel box (centered)
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot     = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720f, 640f);
            panel.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.11f, 0.98f);

            // Title
            var titleTmp = MakeTMP(panel.transform, "Title",
                new Vector2(0f, 0.89f), new Vector2(0.72f, 1f),
                new Vector4(18f, 0f, 0f, -8f),
                24f, true, Color.white, TMPro.TextAlignmentOptions.Left);

            // Count
            var countTmp = MakeTMP(panel.transform, "Count",
                new Vector2(0.72f, 0.89f), new Vector2(1f, 1f),
                new Vector4(0f, 0f, -18f, -8f),
                16f, false, new Color(0.72f, 0.72f, 0.72f), TMPro.TextAlignmentOptions.Right);

            // Close button
            var (closeBtn, _) = MakeButton(panel.transform, "CloseBtn",
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f), size: new Vector2(52f, 38f), pos: Vector2.zero,
                bgColor: new Color(0.45f, 0.09f, 0.09f, 0.95f),
                labelText: "X", labelSize: 20f);

            // Scroll view
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 0.89f);
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, 0f);
            scrollGo.AddComponent<Image>().color = Color.clear;
            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;

            // Viewport
            var vpGo = new GameObject("Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(scrollGo.transform, false);
            Stretch(vpGo.GetComponent<RectTransform>());
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scrollView.viewport = vpGo.GetComponent<RectTransform>();

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth     = true;
            vlg.childControlHeight    = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollView.content = contentRt;

            // Pool root (active but invisible — items are individually deactivated)
            var poolGo = new GameObject("AchievementItemPool", typeof(RectTransform));
            poolGo.transform.SetParent(canvasParent, false);
            poolGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            root.SetActive(false);

            var view           = root.AddComponent<AchievementPopupView>();
            view.popupRoot     = root;
            view.closeButton   = closeBtn;
            view.titleText     = titleTmp;
            view.countText     = countTmp;
            view.scrollRect    = scrollView;
            view.contentParent = contentGo.transform;
            view.poolRoot      = poolGo.transform;

            view.EnsureInitialized();
            return view;
        }

        // ── Public interface ─────────────────────────────────────────────────
        public void SetVisible(bool visible) => popupRoot.SetActive(visible);

        public void SetTitle(string title)
        {
            if (titleText) titleText.text = title;
        }

        public void SetCount(int completed, int total)
        {
            if (countText) countText.text = $"{completed} / {total}";
        }

        public void ResetScroll()
        {
            if (scrollRect) scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        public void AddItem(string name, string desc, string progress, bool isCompleted)
        {
            var item = GetFromPool();
            item.transform.SetParent(contentParent, false);
            item.transform.SetAsLastSibling();
            item.SetData(name, desc, progress, isCompleted);
            item.gameObject.SetActive(true);
            _activeItems.Add(item);
        }

        public void ClearItems()
        {
            foreach (var item in _activeItems)
                ReturnToPool(item);
            _activeItems.Clear();
        }

        // ── Pool ─────────────────────────────────────────────────────────────
        private AchievementListItemView GetFromPool()
        {
            if (_pool.Count > 0) return _pool.Dequeue();
            return AchievementListItemView.CreateTemplate(poolRoot ?? transform);
        }

        private void ReturnToPool(AchievementListItemView item)
        {
            item.gameObject.SetActive(false);
            item.transform.SetParent(poolRoot ?? transform, false);
            _pool.Enqueue(item);
        }

        // ── Build helpers ────────────────────────────────────────────────────
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI MakeTMP(Transform parent, string goName,
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
            tmp.fontSize  = size;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static (Button btn, TextMeshProUGUI label) MakeButton(Transform parent, string goName,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos,
            Color bgColor, string labelText, float labelSize)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.AddComponent<Image>().color = bgColor;
            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = labelText;
            tmp.fontSize  = labelSize;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            return (btn, tmp);
        }
    }
}
