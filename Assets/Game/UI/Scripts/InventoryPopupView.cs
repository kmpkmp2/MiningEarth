using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class InventoryPopupView : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Inventory Components")]
        [SerializeField] private Transform gridContentParent;
        [SerializeField] private ItemDetailPanelView detailPanel;
        [SerializeField] private GameObject slotPrefab; // Slot prefab reference (can be set procedurally)

        [Header("Confirmation Popup")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmOkButton;
        [SerializeField] private Button confirmCancelButton;

        [Header("Empty State Message")]
        [SerializeField] private TextMeshProUGUI emptyMessageText;

        public TextMeshProUGUI GetEmptyMessageText()
        {
            if (emptyMessageText == null)
            {
                var go = new GameObject("EmptyMessageText", typeof(RectTransform));
                if (gridContentParent != null)
                {
                    go.transform.SetParent(gridContentParent.parent, false);
                }
                else
                {
                    go.transform.SetParent(transform, false);
                }
                
                emptyMessageText = go.AddComponent<TextMeshProUGUI>();
                emptyMessageText.fontSize = 24;
                emptyMessageText.color = Color.white;
                emptyMessageText.alignment = TextAlignmentOptions.Center;
                emptyMessageText.text = "보유 중인 아이템이 없습니다.";
                
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(400, 50);
            }
            return emptyMessageText;
        }

        public event Action OnCloseClicked;
        public event Action OnConfirmOkClicked;
        public event Action OnConfirmCancelClicked;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            }

            if (confirmOkButton != null)
            {
                confirmOkButton.onClick.AddListener(() => OnConfirmOkClicked?.Invoke());
            }

            if (confirmCancelButton != null)
            {
                confirmCancelButton.onClick.AddListener(() => OnConfirmCancelClicked?.Invoke());
            }

            HideConfirmation();
            if (detailPanel != null)
            {
                detailPanel.SetVisible(false);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (popupRoot != null)
            {
                popupRoot.SetActive(visible);
            }

            if (!visible)
            {
                HideConfirmation();
                if (detailPanel != null)
                {
                    detailPanel.SetVisible(false);
                }
            }
        }

        public void LocalizeTitle(string text)
        {
            if (titleText != null)
            {
                titleText.text = text;
            }
        }

        public Transform GetGridContentParent()
        {
            return gridContentParent;
        }

        public ItemDetailPanelView GetDetailPanel()
        {
            return detailPanel;
        }

        public GameObject GetSlotPrefab()
        {
            return slotPrefab;
        }

        public void ShowConfirmation(string message)
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);
            }
            if (confirmationText != null)
            {
                confirmationText.text = message;
            }
        }

        public void HideConfirmation()
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }
        }
    }
}
