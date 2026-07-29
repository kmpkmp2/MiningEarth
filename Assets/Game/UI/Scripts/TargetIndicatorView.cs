using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // Target Select 상태에서 몬스터 머리 위에 표시되는 선택 가능/선택됨 링. IntentView와 동일한 월드 추적 패턴.
    public class TargetIndicatorView : MonoBehaviour
    {
        [SerializeField] private RectTransform selfRect;
        [SerializeField] private Image ringImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.4f, 0f);

        private Transform _followTarget;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void SetFollowTarget(Transform target) => _followTarget = target;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // 선택 가능 상태로 처음 표시될 때의 등장 연출(가벼운 Scale-in).
        public async UniTask PlayScaleInAsync()
        {
            float duration = BattleBalanceData.Instance.targetSelectScaleInTime;
            transform.localScale = Vector3.one * 0.7f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = duration > 0f ? elapsed / duration : 1f;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one, t);
                if (canvasGroup != null) canvasGroup.alpha = t;
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            transform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        // 플레이어가 이 몬스터를 최종 선택했을 때: Scale 펀치 + 밝기 Flash.
        public async UniTask PlayConfirmAsync()
        {
            float scaleTime = BattleBalanceData.Instance.targetSelectScaleInTime;
            float flashTime = BattleBalanceData.Instance.targetSelectFlashTime;

            float elapsed = 0f;
            while (elapsed < scaleTime)
            {
                float t = scaleTime > 0f ? elapsed / scaleTime : 1f;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.25f, t);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            transform.localScale = Vector3.one * 1.25f;

            if (ringImage != null)
            {
                Color baseColor = ringImage.color;
                Color flashColor = Color.white;
                elapsed = 0f;
                while (elapsed < flashTime)
                {
                    float t = flashTime > 0f ? elapsed / flashTime : 1f;
                    ringImage.color = Color.Lerp(flashColor, baseColor, t);
                    elapsed += Time.deltaTime;
                    await UniTask.Yield();
                }
                ringImage.color = baseColor;
            }
        }

        public void SetRingSprite(Sprite sprite)
        {
            if (ringImage != null && sprite != null) ringImage.sprite = sprite;
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
