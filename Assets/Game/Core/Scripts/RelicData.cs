using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "Relic_New", menuName = "DeepEarth/Relic/RelicData")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public string relicID;
        public string nameLocKey;
        public string descLocKey;
        public string iconKey;

        [Header("Classification")]
        public RelicRarity rarity;

        [Header("Effects")]
        public List<RelicEffectData> effects = new List<RelicEffectData>();

        // ── Backward-Compatibility Legacy Fields ─────────────────────────────
        // Kept so existing .asset files (pre-migration) still deserialize without error.
        // After the Python migration script runs, all these will be 0/false.
        [HideInInspector] public bool isTombstone;
        [HideInInspector] public int  burnDurationModifier;
        [HideInInspector] public int  burnDamageModifier;
        [HideInInspector][Range(0f,1f)] public float burnImmunityChance;
        [HideInInspector] public int   attackBonus;
        [HideInInspector] public int   miningPowerBonus;
        [HideInInspector] public int   maxHPBonus;
        [HideInInspector] public float resourceMultiplierBonus;
        [HideInInspector] public int   monsterAttackBonus;
        [HideInInspector] public float monsterSpawnRateBonus;
        [HideInInspector] public int   pickaxeDurabilityModifier;

        // ── Helpers ──────────────────────────────────────────────────────────

        // Sum of all effects matching the given type.
        public float GetEffectValue(RelicEffectType type)
        {
            if (effects != null && effects.Count > 0)
            {
                float total = 0f;
                foreach (var e in effects)
                    if (e.effectType == type) total += e.value;
                return total;
            }
            return GetLegacyValue(type);
        }

        // Legacy fallback — reads old flat fields when effects list is empty.
        private float GetLegacyValue(RelicEffectType type) => type switch
        {
            RelicEffectType.AttackBonus               => attackBonus,
            RelicEffectType.MiningPowerBonus          => miningPowerBonus,
            RelicEffectType.MaxHPBonus                => maxHPBonus,
            RelicEffectType.ResourceMultiplierBonus   => resourceMultiplierBonus,
            RelicEffectType.BurnDurationModifier      => burnDurationModifier,
            RelicEffectType.BurnDamageModifier        => burnDamageModifier,
            RelicEffectType.BurnImmunityChance        => burnImmunityChance,
            RelicEffectType.MonsterAttackBonus        => monsterAttackBonus,
            RelicEffectType.MonsterSpawnRateBonus     => monsterSpawnRateBonus,
            RelicEffectType.PickaxeDurabilityModifier => pickaxeDurabilityModifier,
            _                                         => 0f
        };
    }
}
