using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class EffectCardView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private Image borderOutline;

        public void Setup(string name, string typeName, string description, Sprite sprite, EffectSystemType type)
        {
            if (nameText != null) nameText.text = name;
            if (typeText != null) typeText.text = typeName;
            if (descText != null) descText.text = description;
            if (iconImage != null && sprite != null) iconImage.sprite = sprite;

            if (borderOutline != null)
            {
                Color borderColor = type switch
                {
                    EffectSystemType.CharacterPassive => new Color(0f, 0.7f, 1f),
                    EffectSystemType.BossReward => new Color(1f, 0.84f, 0f),
                    EffectSystemType.Buff => new Color(0.2f, 0.85f, 0.2f),
                    EffectSystemType.Debuff => new Color(0.9f, 0.1f, 0.15f),
                    EffectSystemType.StatusEffect => new Color(1f, 0.45f, 0f),
                    EffectSystemType.Special => new Color(0.6f, 0.2f, 0.9f),
                    _ => Color.white
                };
                borderOutline.color = borderColor;
            }
        }
    }
}
