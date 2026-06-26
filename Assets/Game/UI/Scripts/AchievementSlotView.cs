using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class AchievementSlotView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image completedOverlay;
        [SerializeField] private Image hiddenOverlay;

        public void SetData(AchievementModel model)
        {
            if (model == null) return;

            var loc = LocalizationManager.Instance;
            bool hidden = model.Data.isHidden && !model.IsCompleted;

            if (hiddenOverlay != null) hiddenOverlay.gameObject.SetActive(hidden);

            if (hidden)
            {
                if (nameText != null) nameText.text = loc?.GetTranslation("achievement_hidden_name") ?? "???";
                if (descText != null) descText.text  = loc?.GetTranslation("achievement_hidden_desc") ?? "???";
                if (progressText != null) progressText.text = "";
                if (progressSlider != null) progressSlider.value = 0f;
                if (completedOverlay != null) completedOverlay.gameObject.SetActive(false);
                return;
            }

            if (nameText != null)
                nameText.text = loc?.GetTranslation(model.Data.nameLocKey) ?? model.Data.nameLocKey;

            if (descText != null)
                descText.text = loc?.GetTranslation(model.Data.descLocKey) ?? model.Data.descLocKey;

            if (progressText != null)
                progressText.text = model.IsCompleted
                    ? loc?.GetTranslation("achievement_completed_label") ?? "달성"
                    : $"{model.CurrentProgress} / {model.Data.targetValue}";

            if (progressSlider != null)
                progressSlider.value = model.ProgressRatio;

            if (completedOverlay != null)
                completedOverlay.gameObject.SetActive(model.IsCompleted);

            // Dim incomplete achievements slightly
            var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            cg.alpha = model.IsCompleted ? 1f : 0.75f;
        }
    }
}
