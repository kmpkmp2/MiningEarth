using UnityEngine;

namespace DeepEarth.UI
{
    // 몬스터 아래쪽에 상태 아이콘 줄을 띄우는 월드 추적 UI. MonsterHPBarView와 동일한
    // 화면공간 오버레이 + WorldToScreenPoint 추적 패턴을 그대로 재사용한다. 로직 없이 위치 추적만 전담.
    public class MonsterEffectHUDTracker : MonoBehaviour
    {
        [SerializeField] private RectTransform selfRect;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.1f, 0f);

        private Transform _followTarget;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void SetFollowTarget(Transform target) => _followTarget = target;

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

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
