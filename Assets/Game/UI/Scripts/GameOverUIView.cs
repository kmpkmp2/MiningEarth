using System;
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

        [Header("Actions")]
        [SerializeField] private Button restartButton;

        [Header("Localization")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI restartButtonLabel;

        public event Action OnRestartClicked;

        private void Start()
        {
            if (restartButton != null) restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void LocalizeStaticTexts()
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.Instance.GetTranslation("go_title");
            }
            if (restartButtonLabel != null)
            {
                restartButtonLabel.text = LocalizationManager.Instance.GetTranslation("go_restart_btn");
            }
        }

        public void SetResults(int depth, int willEarned, int totalWill, int bestDepth)
        {
            LocalizeStaticTexts();
            if (resultDepthText != null) resultDepthText.text = LocalizationManager.Instance.GetFormatted("go_depth", depth);
            if (willEarnedText != null) willEarnedText.text = LocalizationManager.Instance.GetFormatted("go_will_earned", willEarned);
            if (totalWillText != null) totalWillText.text = LocalizationManager.Instance.GetFormatted("go_total_will", totalWill);
            if (bestDepthText != null) bestDepthText.text = LocalizationManager.Instance.GetFormatted("go_best_depth", bestDepth);
        }
    }
}
