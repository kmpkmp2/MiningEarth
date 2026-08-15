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

        [Header("Emergency Repair")]
        [SerializeField] private Button emergencyRepairButton;

        public event System.Action OnEmergencyRepairClicked;

        private const float AlertDuration = 3f;
        private const float WarningThreshold = 0.25f;
        private Coroutine _alertCoroutine;

        private void Awake()
        {
            if (emergencyRepairButton != null)
                emergencyRepairButton.onClick.AddListener(() => OnEmergencyRepairClicked?.Invoke());
        }

        // 전투 중에는 버튼 자체를 눌러도 즉시 실패하지만, 애초에 손댈 수 없도록 interactable도 함께 끈다.
        public void SetEmergencyRepairInteractable(bool interactable)
        {
            if (emergencyRepairButton != null) emergencyRepairButton.interactable = interactable;
        }

        public void SetDurability(int current, int max, bool broken)
        {
            if (durabilityText != null)
            {
                if (broken)
                    durabilityText.text = LocalizationManager.Instance.GetTranslation("hud_pickaxe_broken");
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

        public void ShowBrokenAlert() => ShowAlert("pickaxe_broken_alert_title", "pickaxe_broken_alert_desc");

        // 내구도 25% 이하 진입 시 1회성 경고(파손 전 미리 알림). 상시 표시되는 warningIndicator/텍스트 색상과는
        // 별개로, 처음 그 구간에 들어가는 순간을 놓치지 않도록 짧은 팝업으로도 알려준다.
        public void ShowWarningAlert() => ShowAlert("pickaxe_warning_alert_title", "pickaxe_warning_alert_desc");

        private void ShowAlert(string titleKey, string descKey)
        {
            if (brokenAlertPanel == null) return;

            if (brokenAlertTitleText != null)
                brokenAlertTitleText.text = LocalizationManager.Instance.GetTranslation(titleKey);
            if (brokenAlertDescText != null)
                brokenAlertDescText.text = LocalizationManager.Instance.GetTranslation(descKey);

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
