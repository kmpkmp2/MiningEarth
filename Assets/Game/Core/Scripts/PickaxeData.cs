using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class PickaxeCostEntry
    {
        public BlockType resourceType;
        public int amount;
    }

    [CreateAssetMenu(fileName = "Pickaxe_New", menuName = "DeepEarth/Pickaxe/PickaxeData")]
    public class PickaxeData : ScriptableObject
    {
        public string pickaxeID = "";
        public string nameLocKey = "";
        public string descLocKey = "";
        public string iconKey = "";
        public int baseMaxDurability = 50;
        public int miningPower = 1;
        [Range(0.1f, 3f)] public float repairEfficiency = 1f;
        public bool isDefault = false;
        public List<PickaxeCostEntry> unlockCost = new List<PickaxeCostEntry>();
    }
}
