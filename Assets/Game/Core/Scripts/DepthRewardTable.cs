using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [System.Serializable]
    public class OreDepthBonus
    {
        public BlockType blockType;
        public int bonus;
    }

    [System.Serializable]
    public class DepthTier
    {
        public int minDepth;
        [Tooltip("-1 = unlimited (open-ended tier)")]
        public int maxDepth = -1;
        public List<OreDepthBonus> bonuses = new List<OreDepthBonus>();

        public int GetBonus(BlockType type)
        {
            foreach (var b in bonuses)
                if (b.blockType == type) return b.bonus;
            return 0;
        }
    }

    [CreateAssetMenu(menuName = "DeepEarth/DepthRewardTable", fileName = "DepthRewardTable")]
    public class DepthRewardTable : ScriptableObject
    {
        public List<DepthTier> tiers = new List<DepthTier>();

        public int GetDepthBonus(BlockType type, int depth)
        {
            foreach (var tier in tiers)
            {
                bool inRange = depth >= tier.minDepth && (tier.maxDepth < 0 || depth <= tier.maxDepth);
                if (inRange) return tier.GetBonus(type);
            }
            return 0;
        }

        public int GetRewardAmount(BlockType type, int depth)
        {
            return 1 + GetDepthBonus(type, depth);
        }
    }
}
