using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // MainGameScene 로딩 실패 시 표시하는 팝업 — 메시지 + 확인 버튼(메인 메뉴로 이동).
    public class LoadingFailurePopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;

        public event Action OnConfirmClicked;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(() => OnConfirmClicked?.Invoke());
        }

        public void Show(string message)
        {
            if (messageText != null) messageText.text = message;
            if (confirmButtonLabel != null)
                confirmButtonLabel.text = LocalizationManager.Instance?.GetTranslation("go_restart_btn") ?? "MAIN MENU";
            if (popupRoot != null) popupRoot.SetActive(true);
        }
    }
}
