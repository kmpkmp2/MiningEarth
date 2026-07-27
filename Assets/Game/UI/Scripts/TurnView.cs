using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // Player/Monster Turn 배너 표시 전담. 로직 없이 텍스트+페이드만 담당.
    public class TurnView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI bannerText;

        public async UniTask PlayTurnBannerAsync(string text)
        {
            if (bannerText != null) bannerText.text = text;
            if (canvasGroup == null) return;

            // 요청서: 턴 변경 시 0.2초 페이드. BattleBalanceData.intentAnimationTime을 공용 연출 기준값으로 재사용한다.
            float duration = BattleBalanceData.Instance.intentAnimationTime;
            await FadeAsync(0f, 1f, duration);
            await FadeAsync(1f, 0f, duration);
        }

        private async UniTask FadeAsync(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                canvasGroup.alpha = Mathf.Lerp(from, to, duration > 0f ? elapsed / duration : 1f);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            canvasGroup.alpha = to;
        }
    }
}
