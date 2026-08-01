using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    public class MiningRewardIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null) iconImage.sprite = sprite;
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

            const float dur = 0.45f;
            Vector3 startPos = _rt.position;
            Vector3 targetPos = target.position;
            Vector3 startScale = _rt.localScale;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                if (ct.IsCancellationRequested) return;
                float t = elapsed / dur;
                float eased = t * t;
                _rt.position = Vector3.Lerp(startPos, targetPos, eased);
                _rt.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
    }
}
