using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public enum StartingRelicEffectType
    {
        EventChoiceRollBonus,
        PickaxeDurabilityReduction,
        PotionHealBonus,
        TreasureRewardBonus,
        CurseDurationReduction,
        LowHpAttackBonus,
        MiningGainBonus,
        FixedAttackBonus
    }

    [Serializable]
    public class StartingRelicEffectData
    {
        public StartingRelicEffectType effectType;
        public float value;
    }

    [CreateAssetMenu(fileName = "StartingRelic_New", menuName = "DeepEarth/Character/StartingRelicData")]
    public class StartingRelicData : ScriptableObject
    {
        [Header("Identity")]
        public string relicID;
        public string nameLocKey;
        public string descLocKey;
        public string iconKey;

        [Header("Effects")]
        public List<StartingRelicEffectData> effects = new List<StartingRelicEffectData>();
    }
}
