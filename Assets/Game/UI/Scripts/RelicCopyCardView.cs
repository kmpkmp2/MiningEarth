using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // 그룹 L(수집가의 가방) 전용 — EffectCardView와 동일한 구성이지만 클릭으로 선택 가능한 카드.
    public class RelicCopyCardView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private Image borderOutline;
        [SerializeField] private Button selectButton;

        public event Action OnClicked;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void Setup(string name, string typeName, string description, Sprite sprite, RelicRarity rarity)
        {
            if (nameText != null) nameText.text = name;
            if (typeText != null) typeText.text = typeName;
            if (descText != null) descText.text = description;
            if (iconImage != null && sprite != null) iconImage.sprite = sprite;
            if (borderOutline != null) borderOutline.color = EffectCardView.GetRelicRarityColor(rarity);
        }
    }
}
