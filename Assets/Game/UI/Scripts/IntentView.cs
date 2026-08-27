using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 몬스터 머리 위에 다음 턴 행동(Intent)을 표시하는 월드 추적 UI. 로직 없이 표시 전담.
    public class IntentView : MonoBehaviour
    {
        [SerializeField] private RectTransform selfRect;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI iconText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        private Transform _followTarget;
        private Canvas _rootCanvas;
        private string _loadedIconKey;

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

        // iconKey: 스프라이트 아이콘(Addressables). glyph: 스프라이트 로드 실패 시 폴백용 이모지 텍스트.
        // 폰트가 이모지 글리프를 지원하지 않아 깨진 네모(□)로 보이는 문제 때문에, 스프라이트를 우선 사용한다.
        public void SetIntent(string iconKey, string glyph, int value, string description)
        {
            if (valueText != null) valueText.text = value > 0 ? value.ToString() : string.Empty;
            if (canvasGroup != null) PlayChangeAnimAsync().Forget();
            gameObject.name = string.IsNullOrEmpty(description) ? gameObject.name : $"IntentView_{description}";

            if (!string.IsNullOrEmpty(iconKey))
            {
                LoadIconAsync(iconKey, glyph).Forget();
            }
            else
            {
                ShowGlyphFallback(glyph);
            }
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid LoadIconAsync(string iconKey, string fallbackGlyph)
        {
            _loadedIconKey = iconKey;
            var sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
            if (this == null) return; // 로드 대기 중 몬스터 사망/풀 반환 등으로 이 View가 파괴됐으면 중단.
            if (_loadedIconKey != iconKey) return; // 로드 도중 다른 아이콘으로 바뀌었으면 이 결과는 버린다.

            if (sprite != null && iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.gameObject.SetActive(true);
                if (iconText != null) iconText.gameObject.SetActive(false);
            }
            else
            {
                ShowGlyphFallback(fallbackGlyph);
            }
        }

        private void ShowGlyphFallback(string glyph)
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (iconText != null)
            {
                iconText.gameObject.SetActive(true);
                iconText.text = glyph;
            }
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid PlayChangeAnimAsync()
        {
            // 이 코루틴이 프레임에 걸쳐 진행되는 동안 몬스터가 죽거나(IntentPresenter.RemoveIntent) 풀로
            // 반환되어 이 GameObject/CanvasGroup이 파괴될 수 있다 — 매 반복 파괴 여부를 확인해서
            // MissingReferenceException(마더 동굴거미처럼 새끼가 자주 죽는 전투에서 반복 발생, 콘솔에
            // 예외가 쌓이며 진행이 눈에 띄게 느려짐/멈춘 것처럼 보임)을 막는다.
            if (this == null || canvasGroup == null) return;

            float duration = BattleBalanceData.Instance.intentAnimationTime;
            float elapsed = 0f;
            Vector3 baseScale = Vector3.one;

            canvasGroup.alpha = 0f;
            transform.localScale = baseScale * 0.6f;

            while (elapsed < duration)
            {
                await Cysharp.Threading.Tasks.UniTask.Yield();
                if (this == null || canvasGroup == null) return;

                float t = duration > 0f ? elapsed / duration : 1f;
                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(baseScale * 0.6f, baseScale, t);
                elapsed += Time.deltaTime;
            }

            if (this == null || canvasGroup == null) return;
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
