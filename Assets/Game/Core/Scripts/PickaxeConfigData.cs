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

        public RepairRecipe GetRepairRecipe(string itemID)
        {
            return repairRecipes.Find(r => r.itemID == itemID);
        }
    }
}
