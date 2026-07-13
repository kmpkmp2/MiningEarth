using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DeepEarth.Common;
using DeepEarth.Core;

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

        public void LocalizeStaticTexts()
        {
            if (LocalizationManager.Instance == null) return;
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.GetTranslation("go_title");
            if (restartButtonLabel != null)
                restartButtonLabel.text = LocalizationManager.Instance.GetTranslation("go_restart_btn");
        }

        public void SetResults(int depth, int willEarned, int totalWill, int bestDepth)
        {
            LocalizeStaticTexts();
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
            int iron, int silver, int gold, int diamond)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                LocalizeStaticTexts();

                // 초기 상태: 모두 숨김
                SetTextActive(resultDepthText, false);
                SetTextActive(willEarnedText, false);
                SetTextActive(bestDepthText, false);
                SetTextActive(totalWillText, false);
                if (resourceEarnedRow != null) resourceEarnedRow.gameObject.SetActive(false);
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

                if (willEarnedText != null)
                {
                    willEarnedText.text = LocalizationManager.Instance.GetFormatted("go_will_earned", willEarned);
                    SetTextActive(willEarnedText, true);
                }

                await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: ct);

                if (resourceEarnedRow != null)
                {
                    resourceEarnedRow.gameObject.SetActive(true);
                    await resourceEarnedRow.AnimateAsync(iron, silver, gold, diamond, ct);
                }

                await UniTask.Delay(150, ignoreTimeScale: true, cancellationToken: ct);

                if (bestDepthText != null)
                {
                    bestDepthText.text = LocalizationManager.Instance.GetFormatted("go_best_depth", bestDepth);
                    SetTextActive(bestDepthText, true);
                }

                await UniTask.Delay(150, ignoreTimeScale: true, cancellationToken: ct);

                if (totalWillText != null)
                {
                    totalWillText.text = LocalizationManager.Instance.GetFormatted("go_total_will", totalWill);
                    SetTextActive(totalWillText, true);
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
