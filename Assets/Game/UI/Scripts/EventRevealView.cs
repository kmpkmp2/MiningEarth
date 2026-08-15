using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace DeepEarth.UI
{
    public class EventRevealView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI eventNameText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform panelRoot;

        public void SetEventName(string name)
        {
            if (eventNameText != null)
                eventNameText.text = name;
        }

        // 몬스터 사망 트리거 경고(예: "처치 시 분열합니다!") 등, 조우 이름 아래 보여줄 부가 설명.
        // 빈 문자열이면 서브타이틀 텍스트 자체를 숨긴다.
        public void SetSubtitle(string subtitle)
        {
            if (subtitleText == null) return;
            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            subtitleText.gameObject.SetActive(hasSubtitle);
            subtitleText.text = hasSubtitle ? subtitle : string.Empty;
        }

        public async UniTask PlayShowAsync()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panelRoot != null) panelRoot.localScale = Vector3.zero;

            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (canvasGroup != null) canvasGroup.alpha = t;
                if (panelRoot != null) panelRoot.localScale = Vector3.one * t;
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (panelRoot != null) panelRoot.localScale = Vector3.one;
        }

        public async UniTask PlayHideAsync()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (canvasGroup != null) canvasGroup.alpha = t;
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }

            gameObject.SetActive(false);
        }
    }
}
