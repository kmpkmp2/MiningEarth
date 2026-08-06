using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    // 방어 버튼 클릭 시 표시되는 광물 선택 팝업. 로직 없음 — ShieldPresenter가 버튼 상태/라벨을 전부 결정한다.
    public class ShieldPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;

        [SerializeField] private Button ironButton;
        [SerializeField] private TextMeshProUGUI ironLabel;
        [SerializeField] private Button silverButton;
        [SerializeField] private TextMeshProUGUI silverLabel;
        [SerializeField] private Button goldButton;
        [SerializeField] private TextMeshProUGUI goldLabel;
        [SerializeField] private Button diamondButton;
        [SerializeField] private TextMeshProUGUI diamondLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI cancelLabel;

        private static readonly Color EnabledColor  = new Color(0.12f, 0.26f, 0.52f, 1f);
        private static readonly Color DisabledColor = new Color(0.20f, 0.20f, 0.25f, 1f);

        public event Action<BlockType> OnOreSelected;
        public event Action OnCancelClicked;

        private void Awake()
        {
            if (ironButton != null)    ironButton.onClick.AddListener(() => OnOreSelected?.Invoke(BlockType.Iron));
            if (silverButton != null)  silverButton.onClick.AddListener(() => OnOreSelected?.Invoke(BlockType.Silver));
            if (goldButton != null)    goldButton.onClick.AddListener(() => OnOreSelected?.Invoke(BlockType.Gold));
            if (diamondButton != null) diamondButton.onClick.AddListener(() => OnOreSelected?.Invoke(BlockType.Diamond));
            if (cancelButton != null)  cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
            if (cancelLabel != null)   cancelLabel.text = LocalizationManager.Instance.GetTranslation("battle_cancel_button");

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (popupRoot != null) popupRoot.SetActive(visible);
        }

        public void SetOreButton(BlockType type, int owned, int shieldValue)
        {
            Button button = type switch
            {
                BlockType.Iron => ironButton,
                BlockType.Silver => silverButton,
                BlockType.Gold => goldButton,
                BlockType.Diamond => diamondButton,
                _ => null,
            };
            TextMeshProUGUI label = type switch
            {
                BlockType.Iron => ironLabel,
                BlockType.Silver => silverLabel,
                BlockType.Gold => goldLabel,
                BlockType.Diamond => diamondLabel,
                _ => null,
            };
            if (button == null) return;

            bool canSelect = owned > 0;
            button.interactable = canSelect;
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null) buttonImage.color = canSelect ? EnabledColor : DisabledColor;

            if (label != null)
            {
                string nameKey = "item_" + type.ToString().ToLower() + "_name";
                string oreName = LocalizationManager.Instance.GetTranslation(nameKey);
                label.text = $"{oreName} (+{shieldValue}) ({owned})";
            }
        }
    }
}
