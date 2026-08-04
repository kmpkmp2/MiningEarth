using UnityEngine;
using Cysharp.Threading.Tasks;

namespace DeepEarth.UI
{
    // LoadingScene 전체 Canvas를 덮는 CanvasGroup 페이드아웃 전담 View.
    public class LoadingFadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeOutDuration = 0.4f;

        public async UniTask FadeOutAsync()
        {
            if (canvasGroup == null) return;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                await UniTask.Yield();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
