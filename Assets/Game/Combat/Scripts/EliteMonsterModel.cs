using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.Combat
{
    public class EliteMonsterModel : MonsterModel
    {
        // IronArmor state (IronPlateSpider)
        public int ArmorMaxHP { get; private set; }
        public int ArmorCurrentHP { get; private set; }
        public bool IsArmorBroken => ArmorCurrentHP <= 0;

        // Enrage state (CursedKnight)
        private bool _threshold75;
        private bool _threshold50;
        private bool _threshold25;
        public int EnrageCount { get; private set; }

        // Poison counter (IronPlateSpider) — counts touches since last poison tick
        public int TouchCountSincePoison { get; private set; }

        public EliteMonsterModel(MonsterData data, int depth) : base(data, depth)
        {
            if (data.eliteSkillType == EliteSkillType.IronArmor)
            {
                ArmorMaxHP = Mathf.Max(1, MaxHP / 2);
                ArmorCurrentHP = ArmorMaxHP;
            }
        }

        public void TakeArmorDamage(int amount)
        {
            if (IsArmorBroken) return;
            ArmorCurrentHP = Mathf.Max(0, ArmorCurrentHP - amount);
        }

        public void IncrementTouch() => TouchCountSincePoison++;

        public bool ShouldTriggerPoison() => TouchCountSincePoison >= 3;

        public void ResetPoisonCounter() => TouchCountSincePoison = 0;

        // Returns the new EnrageCount if a threshold is newly crossed; -1 if none.
        public int CheckAndTriggerEnrage()
        {
            if (MaxHP <= 0) return -1;
            float pct = (float)CurrentHP / MaxHP;

            if (!_threshold75 && pct <= 0.75f) { _threshold75 = true; return ++EnrageCount; }
            if (!_threshold50 && pct <= 0.50f) { _threshold50 = true; return ++EnrageCount; }
            if (!_threshold25 && pct <= 0.25f) { _threshold25 = true; return ++EnrageCount; }
            return -1;
        }
    }
}
