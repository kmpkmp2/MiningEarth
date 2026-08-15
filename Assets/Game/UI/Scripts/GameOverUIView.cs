using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Common;
using DeepEarth.Core;
using DeepEarth.Audio;

namespace DeepEarth.UI
{
    public class GameOverUIView : MonoBehaviour
    {
        [Header("Run Summary")]
        [SerializeField] private TextMeshProUGUI resultDepthText;
        [SerializeField] private TextMeshProUGUI willEarnedText;
        [SerializeField] private TextMeshProUGUI totalWillText;
        [SerializeField] private TextMeshProUGUI bestDepthText;

        [Header("Resource Row")]
        [SerializeField] private ResourceEarnedRowView resourceEarnedRow;

        [Header("Will Conversion")]
        [SerializeField] private Image willIcon;

        [Header("Actions")]
        [SerializeField] private Button restartButton;

        [Header("Localization")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI restartButtonLabel;

        public event Action OnRestartClicked;
        public event Action OnResourceItemShown;

        private CanvasGroup _restartBtnCG;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _restartBtnCG = restartButton?.GetComponent<CanvasGroup>();
            if (restartButton != null && _restartBtnCG == null)
                _restartBtnCG = restartButton.gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
            if (resourceEarnedRow != null)
                resourceEarnedRow.OnItemShown += () => OnResourceItemShown?.Invoke();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void LocalizeStaticTexts(bool isVictory = false)
        {
            if (LocalizationManager.Instance == null) return;
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.GetTranslation(isVictory ? "go_victory_title" : "go_title");
            if (restartButtonLabel != null)
                restartButtonLabel.text = LocalizationManager.Instance.GetTranslation(isVictory ? "go_victory_restart_btn" : "go_restart_btn");
        }

        public void SetResults(int depth, int willEarned, int totalWill, int bestDepth, bool isVictory = false)
        {
            LocalizeStaticTexts(isVictory);
            ShowAllTexts();
            if (resultDepthText != null)
                resultDepthText.text = LocalizationManager.Instance.GetFormatted("go_depth", depth);
            if (willEarnedText != null)
                willEarnedText.text = LocalizationManager.Instance.GetFormatted("go_will_earned", willEarned);
            if (totalWillText != null)
                totalWillText.text = LocalizationManager.Instance.GetFormatted("go_total_will", totalWill);
            if (bestDepthText != null)
                bestDepthText.text = LocalizationManager.Instance.GetFormatted("go_best_depth", bestDepth);
        }

        public async UniTask PlayResultAnimationAsync(
            int depth, int willEarned, int totalWill, int bestDepth,
            int iron, int silver, int gold, int diamond, int totalWillBefore, bool isVictory = false)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                LocalizeStaticTexts(isVictory);

                // 초기 상태: 모두 숨김
                SetTextActive(resultDepthText, false);
                SetTextActive(willEarnedText, false);
                SetTextActive(bestDepthText, false);
                SetTextActive(totalWillText, false);
                if (resourceEarnedRow != null) resourceEarnedRow.gameObject.SetActive(false);
                ResetWillIcon();
                if (_restartBtnCG != null)
                {
                    _restartBtnCG.alpha = 0f;
                    _restartBtnCG.interactable = false;
                    _restartBtnCG.blocksRaycasts = false;
                }

                resourceEarnedRow?.ResetAll();

                await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: ct);

                if (resultDepthText != null)
                {
                    resultDepthText.text = LocalizationManager.Instance.GetFormatted("go_depth", depth);
                    SetTextActive(resultDepthText, true);
                }

                await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: ct);

                // ①~④ 철/은/금/다이아 카운트 연출
                if (resourceEarnedRow != null)
                {
                    resourceEarnedRow.gameObject.SetActive(true);
                    await resourceEarnedRow.AnimateAsync(iron, silver, gold, diamond, ct);
                }

                // ⑤ 잠시 대기
                await UniTask.Delay(300, ignoreTimeScale: true, cancellationToken: ct);

                // ⑥ 광물이 Will 아이콘으로 흡수되는 연출
                if (resourceEarnedRow != null && willIcon != null)
                {
                    AudioManager.Instance?.PlaySFX("Will_Convert");
                    await resourceEarnedRow.AnimateAbsorbToAsync(willIcon.rectTransform, ct);
                }

                // ⑦ Will 아이콘 등장 (확대 → Glow → 반짝임)
                if (willIcon != null)
                {
                    await ShowWillIconAsync(ct);
                }

                // ⑧ 획득 Will 0 → 최종값 카운트업
                if (willEarnedText != null)
                {
                    SetTextActive(willEarnedText, true);
                    AudioManager.Instance?.PlaySFX("Will_CountUp");
                    await CountUpTextAsync(willEarnedText, "go_will_earned", 0, willEarned, 0.8f, ct);
                }

                // ⑨ 총 보유 Will 기존값 → 새로운값 카운트업
                if (totalWillText != null)
                {
                    SetTextActive(totalWillText, true);
                    await CountUpTextAsync(totalWillText, "go_total_will", totalWillBefore, totalWill, 0.8f, ct);
                }

                AudioManager.Instance?.PlaySFX("Will_Complete");

                await UniTask.Delay(150, ignoreTimeScale: true, cancellationToken: ct);

                if (bestDepthText != null)
                {
                    bestDepthText.text = LocalizationManager.Instance.GetFormatted("go_best_depth", bestDepth);
                    SetTextActive(bestDepthText, true);
                }

                await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: ct);

                // RestartButton 페이드 인
                if (_restartBtnCG != null)
                {
                    await FadeInAsync(_restartBtnCG, 0.3f, ct);
                    _restartBtnCG.interactable = true;
                    _restartBtnCG.blocksRaycasts = true;
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ResetWillIcon()
        {
            if (willIcon == null) return;
            willIcon.rectTransform.localScale = Vector3.one;
            willIcon.gameObject.SetActive(false);
        }

        private async UniTask ShowWillIconAsync(CancellationToken ct)
        {
            willIcon.gameObject.SetActive(true);
            var rt = willIcon.rectTransform;
            Color baseColor = willIcon.color;
            const float dur = 0.35f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                if (ct.IsCancellationRequested) return;
                float t = elapsed / dur;
                float scale = t < 0.6f
                    ? Mathf.Lerp(0.5f, 1.3f, t / 0.6f)
                    : Mathf.Lerp(1.3f, 1.0f, (t - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * scale;
                float glow = t < 0.6f ? t / 0.6f : 1f - (t - 0.6f) / 0.4f;
                willIcon.color = Color.Lerp(baseColor, Color.white, glow);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            rt.localScale = Vector3.one;
            willIcon.color = baseColor;
        }

        private static async UniTask CountUpTextAsync(TextMeshProUGUI text, string locKey, int from, int to, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;
                float t = elapsed / duration;
                float eased = 1f - (1f - t) * (1f - t);
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, eased));
                text.text = LocalizationManager.Instance.GetFormatted(locKey, value);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            text.text = LocalizationManager.Instance.GetFormatted(locKey, to);
        }

        private static void SetTextActive(TextMeshProUGUI text, bool active)
        {
            if (text != null) text.gameObject.SetActive(active);
        }

        private void ShowAllTexts()
        {
            SetTextActive(resultDepthText, true);
            SetTextActive(willEarnedText, true);
            SetTextActive(bestDepthText, true);
            SetTextActive(totalWillText, true);
            if (resourceEarnedRow != null) resourceEarnedRow.gameObject.SetActive(true);
            if (_restartBtnCG != null)
            {
                _restartBtnCG.alpha = 1f;
                _restartBtnCG.interactable = true;
                _restartBtnCG.blocksRaycasts = true;
            }
        }

        private static async UniTask FadeInAsync(CanvasGroup cg, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            cg.alpha = 0f;
            while (elapsed < duration)
            {
                if (ct.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            cg.alpha = 1f;
        }
    }
}
