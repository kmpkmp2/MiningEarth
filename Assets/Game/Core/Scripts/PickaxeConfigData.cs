using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class OrePickaxeEntry
    {
        public BlockType blockType;
        public int durabilityLoss;
        public int brokenDamage;
        public int durabilityLossPerHit = 0;
    }

    [Serializable]
    public class RepairRecipe
    {
        public string itemID = "";
        public string itemNameLocKey = "";
        public int itemCostPerUse = 1;
        public int durabilityGain = 5;
    }

    [CreateAssetMenu(fileName = "PickaxeConfig", menuName = "DeepEarth/Pickaxe/PickaxeConfig")]
    public class PickaxeConfigData : ScriptableObject
    {
        public List<OrePickaxeEntry> oreEntries = new List<OrePickaxeEntry>();
        public List<RepairRecipe> repairRecipes = new List<RepairRecipe>();

        [Header("Per-Hit Depth Scaling")]
        public int depthBonusInterval = 100;

        public int GetDurabilityLoss(BlockType type)
        {
            var entry = oreEntries.Find(e => e.blockType == type);
            return entry?.durabilityLoss ?? 0;
        }

        public int GetBrokenDamage(BlockType type)
        {
            var entry = oreEntries.Find(e => e.blockType == type);
            return entry?.brokenDamage ?? 0;
        }

        public int GetPerHitDurabilityLoss(BlockType type, int depth)
        {
            var entry = oreEntries.Find(e => e.blockType == type);
            if (entry == null || entry.durabilityLossPerHit == 0) return 0;
            int depthBonus = Mathf.FloorToInt((float)depth / depthBonusInterval);
            return entry.durabilityLossPerHit + depthBonus;
        }

        public RepairRecipe GetRepairRecipe(string itemID)
        {
            return repairRecipes.Find(r => r.itemID == itemID);
        }
    }
}
