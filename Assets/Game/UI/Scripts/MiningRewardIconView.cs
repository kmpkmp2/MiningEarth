using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    public class MiningRewardIconView : MonoBehaviour
    {
        [SerializeField] private Image glowImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private CanvasGroup canvasGroup;

        // 파괴 이펙트(파티클/카메라 흔들림)와 겹치지 않도록 제자리에 머무는 시간.
        private const float HoldDuration = 0.25f;
        // 목표(HUD 인벤토리 버튼)까지 날아가는 시간.
        private const float FlyDuration = 0.55f;
        // 이동 경로상 위로 튀어 오르는 정점 높이(스크린 픽셀).
        private const float ArcHeight = 60f;

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null) iconImage.sprite = sprite;
            if (glowImage != null) glowImage.enabled = sprite != null;
        }

        public void SetAmount(int amount)
        {
            if (amountText == null) return;
            amountText.gameObject.SetActive(amount > 1);
            amountText.text = $"x{amount}";
        }

        public async UniTask FlyToAsync(RectTransform target, CancellationToken ct = default)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null || target == null) return;

            Vector3 startPos = _rt.position;
            Vector3 startScale = _rt.localScale;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // 정지 구간: 확대된 채로 잠깐 머물며 알파를 페이드인 — 파괴 이펙트와 타이밍을 분리.
            float holdElapsed = 0f;
            while (holdElapsed < HoldDuration)
            {
                if (ct.IsCancellationRequested) return;
                float t = holdElapsed / HoldDuration;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / 0.4f);
                _rt.localScale = Vector3.Lerp(startScale, startScale * 1.15f, Mathf.Sin(t * Mathf.PI));
                holdElapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            _rt.position = startPos;
            _rt.localScale = startScale;

            // 이동 구간: 위로 튀는 포물선 경로. 스케일 축소와 알파 페이드아웃은 후반부에만 적용해
            // 목표에 도달하기 훨씬 전부터 흐려지지 않도록 한다.
            Vector3 targetPos = target.position;
            float flyElapsed = 0f;
            while (flyElapsed < FlyDuration)
            {
                if (ct.IsCancellationRequested) return;
                float t = flyElapsed / FlyDuration;
                float eased = t * t;

                Vector3 linear = Vector3.Lerp(startPos, targetPos, eased);
                float arc = Mathf.Sin(t * Mathf.PI) * ArcHeight;
                _rt.position = linear + new Vector3(0f, arc, 0f);

                float scaleT = Mathf.Clamp01((t - 0.5f) / 0.5f);
                _rt.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);

                if (canvasGroup != null)
                {
                    float alphaT = Mathf.Clamp01((t - 0.75f) / 0.25f);
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, alphaT);
                }

                flyElapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }
}
