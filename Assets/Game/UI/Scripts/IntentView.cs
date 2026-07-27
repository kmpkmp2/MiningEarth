using UnityEngine;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 몬스터 머리 위에 다음 턴 행동(Intent)을 표시하는 월드 추적 UI. 로직 없이 표시 전담.
    public class IntentView : MonoBehaviour
    {
        [SerializeField] private RectTransform selfRect;
        [SerializeField] private TextMeshProUGUI iconText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        private Transform _followTarget;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetIntent(string glyph, int value, string description)
        {
            if (iconText != null) iconText.text = glyph;
            if (valueText != null) valueText.text = value > 0 ? value.ToString() : string.Empty;
            if (canvasGroup != null) PlayChangeAnimAsync().Forget();
            gameObject.name = string.IsNullOrEmpty(description) ? gameObject.name : $"IntentView_{description}";
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid PlayChangeAnimAsync()
        {
            float duration = BattleBalanceData.Instance.intentAnimationTime;
            float elapsed = 0f;
            Vector3 baseScale = Vector3.one;

            canvasGroup.alpha = 0f;
            transform.localScale = baseScale * 0.6f;

            while (elapsed < duration)
            {
                float t = duration > 0f ? elapsed / duration : 1f;
                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(baseScale * 0.6f, baseScale, t);
                elapsed += Time.deltaTime;
                await Cysharp.Threading.Tasks.UniTask.Yield();
            }

            canvasGroup.alpha = 1f;
            transform.localScale = baseScale;
        }

        private void LateUpdate()
        {
            if (_followTarget == null || selfRect == null || _rootCanvas == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPoint = cam.WorldToScreenPoint(_followTarget.position + worldOffset);
            var canvasRect = _rootCanvas.transform as RectTransform;
            if (canvasRect == null) return;

            Camera uiCam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCam, out var localPoint))
                selfRect.anchoredPosition = localPoint;
        }
    }
}
