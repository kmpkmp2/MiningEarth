using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public enum EliteRelicFilter { Any }

    [Serializable]
    public class EliteAutoReward
    {
        public string itemId;
        [Min(1)] public int amount = 1;
    }

    [Serializable]
    public class EliteDepthRewardEntry
    {
        [Min(0)] public int minDepth;
        public string itemId;
        [Min(1)] public int amount = 1;
        [Min(1)] public int weight = 1;
    }

    [CreateAssetMenu(fileName = "EliteRewardTable_New", menuName = "DeepEarth/Elite/EliteRewardTable")]
    public class EliteRewardTable : ScriptableObject
    {
        [Header("Will Reward (added to run total)")]
        [Min(0)] public int willReward = 5;

        [Header("Auto Drops (always given on defeat)")]
        public List<EliteAutoReward> autoDrops = new List<EliteAutoReward>();

        [Header("Random Drop (pick one from list)")]
        public bool hasRandomDrop;
        public List<EliteAutoReward> randomDropOptions = new List<EliteAutoReward>();

        [Header("Depth-Scaled Drops (MerchantMimic)")]
        public bool useDepthScaledDrops;
        public List<EliteDepthRewardEntry> depthDrops = new List<EliteDepthRewardEntry>();

        [Header("Relic Choice Reward")]
        public bool giveRelicChoice;
        [Min(1)] public int relicChoiceCount = 3;
        public EliteRelicFilter relicChoiceFilter = EliteRelicFilter.Any;

        public EliteDepthRewardEntry PickDepthDrop(int depth)
        {
            var eligible = new List<EliteDepthRewardEntry>();
            foreach (var d in depthDrops)
                if (depth >= d.minDepth) eligible.Add(d);

            if (eligible.Count == 0) return null;

            int total = 0;
            foreach (var d in eligible) total += d.weight;

            int roll = UnityEngine.Random.Range(0, total);
            int acc = 0;
            foreach (var d in eligible)
            {
                acc += d.weight;
                if (roll < acc) return d;
            }
            return eligible[eligible.Count - 1];
        }
    }
}
