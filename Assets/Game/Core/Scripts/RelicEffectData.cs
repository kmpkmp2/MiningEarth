using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    [Serializable]
    public class RelicEffectData
    {
        public RelicEffectType effectType;
        public float value;
    }

    [Serializable]
    public class RelicFallbackReward
    {
        public string itemID;
        public int amount;
    }

    [CreateAssetMenu(fileName = "RelicRewardConfig", menuName = "DeepEarth/Relic/RelicRewardConfig")]
    public class RelicRewardConfig : ScriptableObject
    {
        [Header("Default Rarity Chances (sum must equal 1)")]
        [Range(0f, 1f)] public float commonChance    = 0.65f;
        [Range(0f, 1f)] public float rareChance      = 0.28f;
        [Range(0f, 1f)] public float legendaryChance = 0.07f;

        [Header("Context Modifiers")]
        [Tooltip("Boss: legendary chance bonus (added, removes from common)")]
        [Range(0f, 0.5f)] public float bossLegendaryBonus = 0.10f;

        [Tooltip("Elite: minimum rare chance")]
        [Range(0f, 1f)] public float eliteRareMinimum = 0.50f;

        [Tooltip("CrystalShrine / Tombstone: rare is the lowest available rarity")]
        public bool rareMinimumEnabled = false;

        [Header("Fallback Rewards (all relics acquired)")]
        public List<RelicFallbackReward> fallbackRewards = new List<RelicFallbackReward>();

        // Returns (common, rare, legendary) chances for the given context
        public (float c, float r, float l) GetChances(RelicRewardContext ctx)
        {
            float c = commonChance;
            float r = rareChance;
            float l = legendaryChance;

            switch (ctx)
            {
                case RelicRewardContext.Boss:
                    l += bossLegendaryBonus;
                    c -= bossLegendaryBonus;
                    c = Mathf.Max(0f, c);
                    break;
                case RelicRewardContext.Elite:
                    float bonus = Mathf.Max(0f, eliteRareMinimum - r);
                    r += bonus;
                    c -= bonus;
                    c = Mathf.Max(0f, c);
                    break;
                case RelicRewardContext.CrystalShrine:
                case RelicRewardContext.Tombstone:
                    // Rare is the minimum — no common
                    r += c;
                    c = 0f;
                    break;
                case RelicRewardContext.Merchant:
                    // No legendary in shops
                    r += l;
                    l = 0f;
                    break;
            }

            // Normalise to sum = 1
            float total = c + r + l;
            if (total > 0) { c /= total; r /= total; l /= total; }
            return (c, r, l);
        }
    }
}
