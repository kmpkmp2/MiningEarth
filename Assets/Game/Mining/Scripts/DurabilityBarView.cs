using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.Mining
{
    public class DurabilityBarView : MonoBehaviour
    {
        private Image _fillImage;
        private TextMeshProUGUI _durabilityText;
        private Coroutine _fillTween;
        private Vector3 _baseScale;

        // Factory: builds a World Space Canvas as a child of the block.
        public static DurabilityBarView Create(Transform blockTransform)
        {
            var root = new GameObject("DurabilityBarRoot", typeof(RectTransform));
            root.transform.SetParent(blockTransform, false);
            root.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            root.transform.localRotation = Quaternion.identity;
            // Scale: 200px canvas * 0.006 = 1.2 world units wide, 60px * 0.006 = 0.36 tall
            root.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(200f, 60f);

            // Dark background
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.88f);

            // Health fill bar (top 55% of canvas height)
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(root.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.03f, 0.42f);
            fillRt.anchorMax = new Vector2(0.97f, 0.94f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            // Durability text (bottom 40% of canvas height)
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0.44f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = "";
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.fontSize = 16f;
            textComp.color = Color.white;

            var view = root.AddComponent<DurabilityBarView>();
            view._fillImage = fillImg;
            view._durabilityText = textComp;
            view._baseScale = root.transform.localScale;

            root.SetActive(false);
            return view;
        }

        private void Update()
        {
            // Billboard: always face camera
            if (Camera.main != null)
            {
                transform.LookAt(
                    transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up
                );
            }
        }

        public void Show(int current, int max)
        {
            gameObject.SetActive(true);
            SetFillImmediate((float)current / Mathf.Max(max, 1));
            UpdateText(current, max);
            StopAllCoroutines();
            StartCoroutine(CoPopIn());
        }

        public void UpdateDurability(int current, int max)
        {
            UpdateText(current, max);
            float target = (float)current / Mathf.Max(max, 1);
            SetFillColor(target);
            if (_fillTween != null) StopCoroutine(_fillTween);
            _fillTween = StartCoroutine(CoTweenFill(_fillImage.fillAmount, target, 0.15f));
        }

        public void Hide()
        {
            StopAllCoroutines();
            _fillTween = null;
            gameObject.SetActive(false);
        }

        private void SetFillImmediate(float fill)
        {
            _fillImage.fillAmount = fill;
            SetFillColor(fill);
        }

        private void UpdateText(int current, int max)
        {
            if (_durabilityText != null)
                _durabilityText.text = $"{current} / {max}";
        }

        private void SetFillColor(float fill)
        {
            if (_fillImage == null) return;
            if (fill > 0.66f)
                _fillImage.color = new Color(0.2f, 0.85f, 0.2f, 1f);
            else if (fill > 0.33f)
                _fillImage.color = new Color(0.95f, 0.78f, 0.1f, 1f);
            else
                _fillImage.color = new Color(0.9f, 0.2f, 0.15f, 1f);
        }

        private IEnumerator CoPopIn()
        {
            transform.localScale = _baseScale * 0.9f;
            float elapsed = 0f;
            const float duration = 0.1f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = _baseScale * Mathf.Lerp(0.9f, 1.0f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = _baseScale;
        }

        private IEnumerator CoTweenFill(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _fillImage.fillAmount = Mathf.Lerp(from, to, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _fillImage.fillAmount = to;
            _fillTween = null;
        }
    }
}
