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
                borderOutline.color = GetRarityColor(type);
        }

        public void SetNameColor(Color color)
        {
            if (nameText != null) nameText.color = color;
        }

        public static Color GetRarityColor(EffectSystemType type) => type switch
        {
            EffectSystemType.CharacterPassive => new Color(0f,   0.7f, 1f),
            EffectSystemType.BossReward       => new Color(1f,   0.84f, 0f),
            EffectSystemType.Buff             => new Color(0.2f, 0.85f, 0.2f),
            EffectSystemType.Debuff           => new Color(0.9f, 0.1f, 0.15f),
            EffectSystemType.StatusEffect     => new Color(1f,   0.45f, 0f),
            EffectSystemType.Special          => new Color(0.6f, 0.2f, 0.9f),
            EffectSystemType.RelicCommon      => new Color(0.6f, 0.6f, 0.6f),   // gray
            EffectSystemType.RelicRare        => new Color(0.27f, 0.53f, 1f),   // blue
            EffectSystemType.RelicUnique      => new Color(0.64f, 0.21f, 0.93f),// purple
            EffectSystemType.RelicLegendary   => new Color(1f,   0.55f, 0.1f),  // orange
            _                                 => Color.white
        };

        public static Color GetRelicRarityColor(RelicRarity rarity) => rarity switch
        {
            RelicRarity.Common    => new Color(0.6f, 0.6f, 0.6f),
            RelicRarity.Rare      => new Color(0.27f, 0.53f, 1f),
            RelicRarity.Unique    => new Color(0.64f, 0.21f, 0.93f),
            RelicRarity.Legendary => new Color(1f, 0.55f, 0.1f),
            _                     => Color.white
        };
    }
}
