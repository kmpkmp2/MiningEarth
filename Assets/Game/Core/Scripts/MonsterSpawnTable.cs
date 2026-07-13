using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class MonsterSpawnEntry
    {
        public MonsterType monsterType;
        [Min(1)] public int weight = 1;
    }

    [Serializable]
    public class DepthSpawnTier
    {
        public int minDepth = 0;
        public int maxDepth = -1; // -1 = unlimited
        public List<MonsterSpawnEntry> entries = new List<MonsterSpawnEntry>();
    }

    [CreateAssetMenu(fileName = "MonsterSpawnTable", menuName = "DeepEarth/Monster/SpawnTable")]
    public class MonsterSpawnTable : ScriptableObject
    {
        public List<DepthSpawnTier> tiers = new List<DepthSpawnTier>();

        public MonsterType PickMonster(int depth)
        {
            var tier = GetTierForDepth(depth);
            if (tier == null || tier.entries == null || tier.entries.Count == 0)
                return MonsterType.CaveRat;

            int totalWeight = 0;
            foreach (var e in tier.entries) totalWeight += e.weight;
            if (totalWeight <= 0) return tier.entries[0].monsterType;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var e in tier.entries)
            {
                cumulative += e.weight;
                if (roll < cumulative) return e.monsterType;
            }
            return tier.entries[0].monsterType;
        }

        private DepthSpawnTier GetTierForDepth(int depth)
        {
            DepthSpawnTier result = null;
            foreach (var tier in tiers)
            {
                if (depth < tier.minDepth) continue;
                if (tier.maxDepth >= 0 && depth > tier.maxDepth) continue;
                if (result == null || tier.minDepth > result.minDepth)
                    result = tier;
            }
            return result;
        }
    }
}
