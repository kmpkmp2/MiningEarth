using UnityEngine;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "Relic_New", menuName = "DeepEarth/Relic")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public string relicID;
        public string nameLocKey;
        public string descLocKey;
        public string iconKey;
        public bool isTombstone;

        [Header("Burn Modifiers")]
        public int burnDurationModifier;
        public int burnDamageModifier;
        [Range(0f, 1f)]
        public float burnImmunityChance;

        [Header("Stat Modifiers")]
        public int attackBonus;
        public int miningPowerBonus;
        public int maxHPBonus;
        public float resourceMultiplierBonus;
        public int monsterAttackBonus;
        public float monsterSpawnRateBonus;
    }
}
