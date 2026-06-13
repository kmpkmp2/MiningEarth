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
        [SerializeField] private TextMeshProUGUI inventoryStatsText;

        public event Action OnCloseClicked;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (popupRoot != null)
            {
                popupRoot.SetActive(visible);
            }
        }

        public void LocalizeTitle(string text)
        {
            if (titleText != null)
            {
                titleText.text = text;
            }
        }

        public void UpdateInventoryStats(string statsText)
        {
            if (inventoryStatsText != null)
            {
                inventoryStatsText.text = statsText;
            }
        }
    }
}
