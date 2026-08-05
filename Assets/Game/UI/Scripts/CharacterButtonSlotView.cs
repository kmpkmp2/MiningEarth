using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeepEarth.UI
{
    // 캐릭터 선택 버튼 1개 슬롯 — CharacterSelectorView/CharacterPopupView가 캐릭터 수만큼 동적 생성한다.
    public class CharacterButtonSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;

        private static readonly Color ColorSelected = new Color(0.18f, 0.77f, 0.31f);
        private static readonly Color ColorNormal   = new Color(0.2f, 0.22f, 0.25f);
        private static readonly Color ColorLocked   = new Color(0.15f, 0.15f, 0.17f);

        // 이벤트(+=/-=) 대신 단순 델리게이트 필드 — Refresh()가 매번 덮어써도 중복 구독이 쌓이지 않는다.
        public Action OnClick;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => OnClick?.Invoke());
        }

        public void SetData(string name, bool isUnlocked, bool isSelected)
        {
            if (button != null) button.interactable = isUnlocked;
            if (background != null)
                background.color = isSelected ? ColorSelected : (isUnlocked ? ColorNormal : ColorLocked);
            if (label != null)
                label.text = isUnlocked ? name : $"🔒 {name}";
        }
    }
}
