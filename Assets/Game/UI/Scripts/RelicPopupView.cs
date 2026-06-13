using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    public class RelicPopupView : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        public event Action OnCloseClicked;

        private readonly List<GameObject> _activeCards = new List<GameObject>();

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

        public Transform GetContentParent()
        {
            return contentParent;
        }

        public GameObject GetCardPrefab()
        {
            return cardPrefab;
        }

        public void ClearCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            _activeCards.Clear();
        }

        public void AddCard(GameObject cardObj)
        {
            if (cardObj != null)
            {
                _activeCards.Add(cardObj);
            }
        }
    }
}
