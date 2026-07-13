using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class ExitRunConfirmPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI cancelButtonLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;

        public event Action OnCancelClicked;
        public event Action OnConfirmClicked;

        private void Start()
        {
            cancelButton?.onClick.AddListener(() => OnCancelClicked?.Invoke());
            confirmButton?.onClick.AddListener(() => OnConfirmClicked?.Invoke());
        }

        public void Show()
        {
            if (popupRoot != null) popupRoot.SetActive(true);
            Localize();
        }

        public void Hide()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
        }

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;
            if (messageText != null)
                messageText.text = LocalizationManager.Instance.GetTranslation("exit_run_confirm_msg");
            if (cancelButtonLabel != null)
                cancelButtonLabel.text = LocalizationManager.Instance.GetTranslation("exit_run_cancel_btn");
            if (confirmButtonLabel != null)
                confirmButtonLabel.text = LocalizationManager.Instance.GetTranslation("exit_run_confirm_btn");
        }
    }
}
