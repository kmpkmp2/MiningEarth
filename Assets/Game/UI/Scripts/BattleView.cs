using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeepEarth.UI
{
    // 턴제 전투 팝업 루트 View. 공격/방어 버튼 + Turn 배너 + Intent 레이어 참조를 노출한다.
    public class BattleView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private TurnView turnView;
        [SerializeField] private GameObject intentViewPrefab;
        [SerializeField] private Transform intentLayer;
        [SerializeField] private GameObject defenseEffectObject;

        public event Action OnAttackClicked;
        public event Action OnDefendClicked;

        public TurnView TurnView => turnView;
        public GameObject IntentViewPrefab => intentViewPrefab;
        public Transform IntentLayer => intentLayer;

        private void Awake()
        {
            if (attackButton != null) attackButton.onClick.AddListener(() => OnAttackClicked?.Invoke());
            if (defendButton != null) defendButton.onClick.AddListener(() => OnDefendClicked?.Invoke());
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (popupRoot != null) popupRoot.SetActive(visible);
        }

        public void SetActionButtonsInteractable(bool interactable)
        {
            if (attackButton != null) attackButton.interactable = interactable;
            if (defendButton != null) defendButton.interactable = interactable;
        }

        public void PlayDefendEffect()
        {
            if (defenseEffectObject == null) return;
            StartCoroutine(CoFlashDefense());
        }

        private IEnumerator CoFlashDefense()
        {
            defenseEffectObject.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            defenseEffectObject.SetActive(false);
        }
    }
}
