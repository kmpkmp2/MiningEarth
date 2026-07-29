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

            // Fast Turn Battle: 배너는 BattlePresenter에서 await 없이(Forget) 호출되어 턴 진행을 막지 않는다.
            float duration = BattleBalanceData.Instance.turnTransitionTime;
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
