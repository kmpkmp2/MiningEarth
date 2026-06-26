using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.Mining
{
    public class DurabilityBarView : MonoBehaviour
    {
        private const int MaxIcons   = 10;
        private const float IconGap  = 3f;
        private const float PopDuration = 0.12f;

        private readonly List<Image> _icons = new List<Image>();
        private readonly List<Coroutine> _popCoroutines = new List<Coroutine>();
        private TextMeshProUGUI _text;
        private Vector3 _baseScale;
        private int _maxHits;
        private int _totalDisplayIcons;
        private Coroutine _popInCoroutine;

        // ── Factory ─────────────────────────────────────────────────────────
        public static DurabilityBarView Create(Transform blockTransform)
        {
            var root = new GameObject("DurabilityBarRoot", typeof(RectTransform));
            root.transform.SetParent(blockTransform, false);
            root.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale    = new Vector3(0.006f, 0.006f, 0.006f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(220f, 52f);

            // Background
            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.9f);

            // Icon container (upper 60%)
            var containerGo = new GameObject("IconContainer", typeof(RectTransform));
            containerGo.transform.SetParent(root.transform, false);
            var cRt = containerGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.38f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.offsetMin = new Vector2(5f, -4f);
            cRt.offsetMax = new Vector2(-5f, -4f);

            // Text (lower 35%)
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0.40f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = ""; tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            var view = root.AddComponent<DurabilityBarView>();
            view._iconContainer = containerGo.GetComponent<RectTransform>();
            view._text          = tmp;
            view._baseScale     = root.transform.localScale;
            root.SetActive(false);
            return view;
        }

        [SerializeField] private RectTransform _iconContainer; // set by Create()

        // ── Billboard ────────────────────────────────────────────────────────
        private void Update()
        {
            if (Camera.main != null)
                transform.LookAt(
                    transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
        }

        // ── Public API (unchanged from original) ─────────────────────────────
        public void Show(int current, int max)
        {
            _maxHits            = Mathf.Max(max, 1);
            _totalDisplayIcons  = Mathf.Min(max, MaxIcons);

            BuildIcons(_totalDisplayIcons);
            RefreshIcons(current);
            UpdateText(current, max);

            gameObject.SetActive(true);
            if (_popInCoroutine != null) StopCoroutine(_popInCoroutine);
            _popInCoroutine = StartCoroutine(CoPopIn());
        }

        public void UpdateDurability(int current, int max)
        {
            RefreshIcons(current);
            UpdateText(current, max);
        }

        public void Hide()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        // ── Icon management ──────────────────────────────────────────────────
        private void BuildIcons(int count)
        {
            // Clear old icons
            foreach (var img in _icons)
                if (img != null) Destroy(img.gameObject);
            _icons.Clear();
            _popCoroutines.Clear();

            if (count <= 0) return;

            float containerW = _iconContainer.rect.width;
            // Fallback if layout not computed yet
            if (containerW < 1f) containerW = 210f;

            float iconW = Mathf.Floor((containerW - IconGap * (count - 1)) / count);
            float iconH = _iconContainer.rect.height;
            if (iconH < 1f) iconH = 28f;

            float totalW = iconW * count + IconGap * (count - 1);
            float startX = -totalW * 0.5f + iconW * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Icon_{i}", typeof(RectTransform));
                go.transform.SetParent(_iconContainer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(iconW, iconH);
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(startX + i * (iconW + IconGap), 0f);

                var img = go.AddComponent<Image>();
                img.color = FullColor();

                _icons.Add(img);
                _popCoroutines.Add(null);
            }
        }

        private void RefreshIcons(int currentHits)
        {
            int active = ActiveIconCount(currentHits);
            Color col = IconColor((float)currentHits / _maxHits);

            for (int i = 0; i < _icons.Count; i++)
            {
                if (i < active)
                {
                    _icons[i].gameObject.SetActive(true);
                    _icons[i].color = col;
                }
                else if (_icons[i].gameObject.activeSelf)
                {
                    // Cancel ongoing pop coroutine for this slot
                    if (_popCoroutines[i] != null) StopCoroutine(_popCoroutines[i]);
                    int captured = i;
                    _popCoroutines[i] = StartCoroutine(CoPopOut(_icons[captured].gameObject, captured));
                }
            }
        }

        // Returns how many icons should be visible given current HP
        private int ActiveIconCount(int currentHits)
        {
            if (currentHits <= 0 || _totalDisplayIcons == 0) return 0;
            return Mathf.Max(1,
                Mathf.CeilToInt((float)currentHits / _maxHits * _totalDisplayIcons));
        }

        // ── Text ─────────────────────────────────────────────────────────────
        private void UpdateText(int current, int max)
        {
            if (_text != null) _text.text = $"{current} / {max}";
        }

        // ── Colors ───────────────────────────────────────────────────────────
        private static Color FullColor() => new Color(0.25f, 0.85f, 0.25f, 1f);

        private static Color IconColor(float ratio)
        {
            if (ratio > 0.65f) return new Color(0.25f, 0.85f, 0.25f, 1f); // green
            if (ratio > 0.33f) return new Color(0.95f, 0.72f, 0.08f, 1f); // orange
            return new Color(0.90f, 0.18f, 0.12f, 1f);                     // red
        }

        // ── Animations ───────────────────────────────────────────────────────
        private IEnumerator CoPopIn()
        {
            transform.localScale = _baseScale * 0.80f;
            float e = 0f;
            while (e < PopDuration)
            {
                transform.localScale = Vector3.Lerp(_baseScale * 0.80f, _baseScale, e / PopDuration);
                e += Time.deltaTime;
                yield return null;
            }
            transform.localScale = _baseScale;
            _popInCoroutine = null;
        }

        private IEnumerator CoPopOut(GameObject icon, int slot)
        {
            var rt = icon.GetComponent<RectTransform>();
            Vector3 from = rt.localScale;
            float e = 0f;
            while (e < PopDuration)
            {
                float t = e / PopDuration;
                rt.localScale = Vector3.Lerp(from, Vector3.zero, t * t); // ease-in
                e += Time.deltaTime;
                yield return null;
            }
            icon.SetActive(false);
            rt.localScale = from; // reset so reuse works
            if (slot < _popCoroutines.Count) _popCoroutines[slot] = null;
        }
    }
}
