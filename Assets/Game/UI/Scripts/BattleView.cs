using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 턴제 전투 팝업 루트 View. 공격/방어/취소 버튼 + Turn 배너 + Intent/Target 레이어 참조를 노출한다.
    public class BattleView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button retreatButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI cancelButtonLabel;
        [SerializeField] private TextMeshProUGUI targetSelectGuidanceText;
        [SerializeField] private TurnView turnView;
        [SerializeField] private GameObject intentViewPrefab;
        [SerializeField] private Transform intentLayer;
        [SerializeField] private GameObject targetIndicatorPrefab;
        [SerializeField] private Transform targetIndicatorLayer;
        [SerializeField] private GameObject hpBarViewPrefab;
        [SerializeField] private Transform hpBarLayer;
        [SerializeField] private GameObject defenseEffectObject;
        [SerializeField] private ShieldPopupView shieldPopupView;

        public event Action OnAttackClicked;
        public event Action OnDefendClicked;
        public event Action OnCancelClicked;
        public event Action OnRetreatClicked;

        public TurnView TurnView => turnView;
        public GameObject IntentViewPrefab => intentViewPrefab;
        public Transform IntentLayer => intentLayer;
        public GameObject TargetIndicatorPrefab => targetIndicatorPrefab;
        public Transform TargetIndicatorLayer => targetIndicatorLayer;
        public GameObject HPBarViewPrefab => hpBarViewPrefab;
        public Transform HPBarLayer => hpBarLayer;
        public ShieldPopupView ShieldPopupView => shieldPopupView;

        private void Awake()
        {
            if (attackButton != null) attackButton.onClick.AddListener(() => OnAttackClicked?.Invoke());
            if (defendButton != null) defendButton.onClick.AddListener(() => OnDefendClicked?.Invoke());
            if (retreatButton != null) retreatButton.onClick.AddListener(() => OnRetreatClicked?.Invoke());
            if (cancelButton != null) cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
            if (cancelButtonLabel != null) cancelButtonLabel.text = LocalizationManager.Instance.GetTranslation("battle_cancel_button");
            SetTargetSelectUIVisible(false);
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
            if (retreatButton != null) retreatButton.interactable = interactable;
        }

        // 보스전에서는 후퇴가 불가능하므로 버튼 자체를 감춘다(비활성화가 아니라 노출 자체를 제거).
        public void SetRetreatButtonVisible(bool visible)
        {
            if (retreatButton != null) retreatButton.gameObject.SetActive(visible);
        }

        // Target Select 상태 진입/이탈 시 안내 문구 + 취소 버튼 표시 토글.
        // Cancel 버튼은 Defend 버튼과 동일한 자리를 사용하므로, 진입 시 Defend를 숨기고 Cancel을 보여준다.
        public void SetTargetSelectUIVisible(bool visible)
        {
            if (cancelButton != null) cancelButton.gameObject.SetActive(visible);
            if (defendButton != null) defendButton.gameObject.SetActive(!visible);
            if (targetSelectGuidanceText != null)
            {
                targetSelectGuidanceText.gameObject.SetActive(visible);
                if (visible) targetSelectGuidanceText.text = LocalizationManager.Instance.GetTranslation("battle_target_select_guidance");
            }
        }

        public void PlayDefendEffect()
        {
            if (defenseEffectObject == null) return;
            StartCoroutine(CoFlashDefense(BattleBalanceData.Instance.defenseAnimationTime));
        }

        private IEnumerator CoFlashDefense(float duration)
        {
            defenseEffectObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            defenseEffectObject.SetActive(false);
        }
    }
}
