using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    public class ResourceItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;

        private CanvasGroup _cg;
        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            ResetState();
        }

        public void ResetState()
        {
            if (_cg == null) _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (_rt == null) _rt = GetComponent<RectTransform>();
            _cg.alpha = 0f;
            _rt.localScale = Vector3.one * 0.8f;
            if (countText != null) countText.text = "0";
        }

        public async UniTask ShowAsync(int count, CancellationToken ct = default)
        {
            // Scale 0.8 → 1.1 → 1.0, Alpha 0 → 1 (0.25s)
            const float animDur = 0.25f;
            float elapsed = 0f;
            _cg.alpha = 0f;
            _rt.localScale = Vector3.one * 0.8f;

            while (elapsed < animDur)
            {
                if (ct.IsCancellationRequested) return;
                float t = elapsed / animDur;
                float scale = t < 0.7f
                    ? Mathf.Lerp(0.8f, 1.1f, t / 0.7f)
                    : Mathf.Lerp(1.1f, 1.0f, (t - 0.7f) / 0.3f);
                _rt.localScale = Vector3.one * scale;
                _cg.alpha = Mathf.Min(1f, t / 0.6f);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _rt.localScale = Vector3.one;
            _cg.alpha = 1f;

            CountUpAsync(count, ct).Forget();
        }

        private async UniTaskVoid CountUpAsync(int target, CancellationToken ct)
        {
            const float dur = 0.5f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                if (ct.IsCancellationRequested) return;
                float t = elapsed / dur;
                float eased = 1f - (1f - t) * (1f - t);
                if (countText != null) countText.text = Mathf.RoundToInt(Mathf.Lerp(0, target, eased)).ToString();
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            if (countText != null) countText.text = target.ToString();
        }
    }
}
