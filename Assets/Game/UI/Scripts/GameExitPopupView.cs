using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class GameExitPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI cancelButtonLabel;
        [SerializeField] private float animDuration = 0.15f;

        private static readonly Vector3 HiddenScale = new Vector3(0.9f, 0.9f, 1f);

        public event Action OnConfirmClicked;
        public event Action OnCancelClicked;

        private void Start()
        {
            confirmButton?.onClick.AddListener(() => OnConfirmClicked?.Invoke());
            cancelButton?.onClick.AddListener(() => OnCancelClicked?.Invoke());
            Localize();
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (confirmButton != null) confirmButton.interactable = interactable;
            if (cancelButton != null) cancelButton.interactable = interactable;
        }

        public async UniTask ShowAsync()
        {
            if (popupRoot == null) return;

            SetButtonsInteractable(true);
            popupRoot.SetActive(true);

            if (canvasGroup == null || panelRect == null) return;

            canvasGroup.alpha = 0f;
            panelRect.localScale = HiddenScale;

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);
                canvasGroup.alpha = t;
                panelRect.localScale = Vector3.Lerp(HiddenScale, Vector3.one, t);
                await UniTask.Yield();
            }
            canvasGroup.alpha = 1f;
            panelRect.localScale = Vector3.one;
        }

        public async UniTask HideAsync()
        {
            if (popupRoot == null) return;

            if (canvasGroup != null && panelRect != null)
            {
                float startAlpha = canvasGroup.alpha;
                Vector3 startScale = panelRect.localScale;
                float elapsed = 0f;
                while (elapsed < animDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animDuration);
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    panelRect.localScale = Vector3.Lerp(startScale, HiddenScale, t);
                    await UniTask.Yield();
                }
                canvasGroup.alpha = 0f;
            }

            popupRoot.SetActive(false);
        }

        public void Localize()
        {
            if (LocalizationManager.Instance == null) return;
            var loc = LocalizationManager.Instance;

            if (titleText != null) titleText.text = loc.GetTranslation("game_exit_title");
            if (descriptionText != null) descriptionText.text = loc.GetTranslation("game_exit_desc");
            if (confirmButtonLabel != null) confirmButtonLabel.text = loc.GetTranslation("game_exit_confirm_btn");
            if (cancelButtonLabel != null) cancelButtonLabel.text = loc.GetTranslation("game_exit_cancel_btn");
        }
    }
}
