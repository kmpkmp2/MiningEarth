using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace DeepEarth.UI
{
    // 그룹 L(수집가의 가방) 전용 팝업 View — UI_Panel_RelicPopup의 구조를 재활용하되 신규 제작.
    // 닫기 버튼이 없다 — 반드시 카드 하나를 클릭해야만 닫힌다(강제 선택).
    public class RelicCopyPopupView : MonoBehaviour
    {
        [Header("Hierarchy References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private TextMeshProUGUI titleText;

        private readonly List<GameObject> _activeCards = new List<GameObject>();

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (popupRoot != null)
                popupRoot.SetActive(visible);
        }

        public void LocalizeTitle(string text)
        {
            if (titleText != null)
                titleText.text = text;
        }

        public Transform GetContentParent() => contentParent;
        public GameObject GetCardPrefab() => cardPrefab;

        public void ClearCards()
        {
            foreach (var card in _activeCards)
                if (card != null)
                    Destroy(card);
            _activeCards.Clear();
        }

        public void AddCard(GameObject cardObj)
        {
            if (cardObj != null)
                _activeCards.Add(cardObj);
        }
    }
}
