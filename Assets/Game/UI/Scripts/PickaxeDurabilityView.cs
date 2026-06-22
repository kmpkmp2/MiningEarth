using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class PickaxeDurabilityView : MonoBehaviour
    {
        [Header("Durability Display")]
        [SerializeField] private TextMeshProUGUI durabilityText;
        [SerializeField] private Slider durabilitySlider;
        [SerializeField] private GameObject brokenIndicator;
        [SerializeField] private GameObject warningIndicator;

        [Header("Broken Alert")]
        [SerializeField] private GameObject brokenAlertPanel;
        [SerializeField] private TextMeshProUGUI brokenAlertTitleText;
        [SerializeField] private TextMeshProUGUI brokenAlertDescText;

        private const float AlertDuration = 3f;
        private const float WarningThreshold = 0.25f;
        private Coroutine _alertCoroutine;

        public void SetDurability(int current, int max, bool broken)
        {
            if (durabilityText != null)
            {
                if (broken)
                    durabilityText.text = LocalizationManager.Instance?.GetTranslation("hud_pickaxe_broken") ?? "파손";
                else
                    durabilityText.text = $"{current} / {max}";

                bool isWarning = !broken && max > 0 && (float)current / max <= WarningThreshold;
                durabilityText.color = broken ? Color.red
                                     : isWarning ? new Color(1f, 0.5f, 0f)
                                     : Color.white;
            }

            if (durabilitySlider != null)
            {
                durabilitySlider.maxValue = Mathf.Max(1, max);
                durabilitySlider.value = current;
            }

            if (brokenIndicator != null)
                brokenIndicator.SetActive(broken);

            if (warningIndicator != null)
                warningIndicator.SetActive(!broken && max > 0 && (float)current / max <= WarningThreshold);
        }

        public void ShowBrokenAlert()
        {
            if (brokenAlertPanel == null) return;

            if (brokenAlertTitleText != null)
                brokenAlertTitleText.text = LocalizationManager.Instance?.GetTranslation("pickaxe_broken_alert_title")
                                            ?? "곡괭이가 파손되었습니다.";
            if (brokenAlertDescText != null)
                brokenAlertDescText.text = LocalizationManager.Instance?.GetTranslation("pickaxe_broken_alert_desc")
                                           ?? "이제 채굴 시 체력을 소모합니다.";

            if (_alertCoroutine != null) StopCoroutine(_alertCoroutine);
            _alertCoroutine = StartCoroutine(AlertCoroutine());
        }

        public void HideBrokenAlert()
        {
            if (_alertCoroutine != null)
            {
                StopCoroutine(_alertCoroutine);
                _alertCoroutine = null;
            }
            if (brokenAlertPanel != null) brokenAlertPanel.SetActive(false);
        }

        private IEnumerator AlertCoroutine()
        {
            brokenAlertPanel.SetActive(true);
            yield return new WaitForSeconds(AlertDuration);
            brokenAlertPanel.SetActive(false);
            _alertCoroutine = null;
        }
    }
}
