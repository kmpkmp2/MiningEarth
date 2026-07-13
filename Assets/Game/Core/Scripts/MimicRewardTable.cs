using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public enum MimicRewardType
    {
        Iron,
        Silver,
        Gold,
        Diamond,
        Potion,
        MiningPowerUp,
    }

    [Serializable]
    public class MimicRewardEntry
    {
        public MimicRewardType rewardType;
        [Min(1)] public int baseWeight = 10;
        public int weightBonusPer10Depth = 0; // adds weight for valuable items at deeper depths
        [Min(1)] public int amount = 1;
    }

    [CreateAssetMenu(fileName = "MimicRewardTable", menuName = "DeepEarth/Monster/MimicRewardTable")]
    public class MimicRewardTable : ScriptableObject
    {
        public List<MimicRewardEntry> entries = new List<MimicRewardEntry>();

        public MimicRewardEntry PickReward(int depth)
        {
            if (entries == null || entries.Count == 0) return null;

            int totalWeight = 0;
            foreach (var e in entries)
                totalWeight += Mathf.Max(1, e.baseWeight + (depth / 10) * e.weightBonusPer10Depth);

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var e in entries)
            {
                int w = Mathf.Max(1, e.baseWeight + (depth / 10) * e.weightBonusPer10Depth);
                cumulative += w;
                if (roll < cumulative) return e;
            }
            return entries[0];
        }
    }
}
