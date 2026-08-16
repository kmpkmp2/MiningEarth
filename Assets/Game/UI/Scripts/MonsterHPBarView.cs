using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    // 몬스터 머리 위에 남은 체력을 표시하는 월드 추적 UI. IntentView/TargetIndicatorView와 동일한
    // 화면공간 오버레이 + WorldToScreenPoint 추적 패턴을 그대로 재사용한다. 로직 없이 표시 전담.
    public class MonsterHPBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform selfRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);

        private Transform _followTarget;
        private Canvas _rootCanvas;

        private static readonly Color HighColor = new Color(0.25f, 0.85f, 0.25f, 1f);
        private static readonly Color MidColor  = new Color(0.95f, 0.72f, 0.08f, 1f);
        private static readonly Color LowColor  = new Color(0.90f, 0.18f, 0.12f, 1f);

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void SetFollowTarget(Transform target) => _followTarget = target;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetHP(int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
                fillImage.color = ratio > 0.5f ? HighColor : ratio > 0.25f ? MidColor : LowColor;
            }

            if (valueText != null) valueText.text = $"{Mathf.Max(0, current)} / {max}";
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
