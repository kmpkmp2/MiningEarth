using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // In-game toast shown briefly when an achievement is completed.
    // AchievementManager.OnAchievementCompleted → ShowNotification().
    public class AchievementNotificationView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;

        private const float DisplayDuration = 4f;
        private CancellationTokenSource _hideCts;

        public void ShowNotification(AchievementModel model)
        {
            if (model == null) return;

            var loc = LocalizationManager.Instance;

            if (headerText != null)
                headerText.text = loc.GetTranslation("achievement_unlocked_header");

            if (nameText != null)
                nameText.text = loc.GetTranslation(model.Data.nameLocKey);

            if (descText != null)
                descText.text = loc.GetTranslation(model.Data.descLocKey);

            if (panel != null) panel.SetActive(true);

            ResetHideTimer();
        }

        private void ResetHideTimer()
        {
            CancelHideTimer();
            _hideCts = new CancellationTokenSource();
            RunHideAsync(_hideCts.Token).Forget();
        }

        private async UniTaskVoid RunHideAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(DisplayDuration), cancellationToken: token);
                if (panel != null) panel.SetActive(false);
            }
            catch (OperationCanceledException) { }
        }

        private void CancelHideTimer()
        {
            _hideCts?.Cancel();
            _hideCts?.Dispose();
            _hideCts = null;
        }

        private void OnDestroy()
        {
            CancelHideTimer();
        }
    }
}
