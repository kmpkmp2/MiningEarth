using System;
using System.Collections.Generic;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public enum CharacterID
    {
        Prisoner,
        Mercenary,
        Miner,
        GraveRobber
    }

    public enum PassiveType
    {
        None,
        AttackBonus,
        MiningBonus,
        GraveRobberPassive
    }

    [Serializable]
    public class CharacterUpgradeData
    {
        public int MiningPowerLevel = 1;
        public int MaxHPLevel = 1;
        public int AttackLevel = 1;
        public int InventorySizeLevel = 0;
        public int PickaxeDurabilityLevel = 0;
    }

    [Serializable]
    public class CharacterSaveEntry
    {
        public CharacterID ID;
        public bool IsUnlocked;
        public CharacterUpgradeData UpgradeData = new CharacterUpgradeData();
    }

    public class CharacterStaticData
    {
        public CharacterID ID;
        public string NameKey;
        public string DescKey;
        public PassiveType Passive;
        public float PassiveValue;
        public int StartAttackBonus;
        public int StartMiningBonus;
        public Dictionary<BlockType, int> UnlockCost = new Dictionary<BlockType, int>();
    }

    public static class CharacterDatabase
    {
        public static readonly List<CharacterStaticData> Characters = new List<CharacterStaticData>
        {
            new CharacterStaticData
            {
                ID = CharacterID.Prisoner,
                NameKey = "char_prisoner_name",
                DescKey = "char_prisoner_desc",
                Passive = PassiveType.None,
                PassiveValue = 0f,
                StartAttackBonus = 0,
                StartMiningBonus = 0,
                UnlockCost = new Dictionary<BlockType, int>() // Default unlocked
            },
            new CharacterStaticData
            {
                ID = CharacterID.Mercenary,
                NameKey = "char_mercenary_name",
                DescKey = "char_mercenary_desc",
                Passive = PassiveType.AttackBonus,
                PassiveValue = 1f,
                StartAttackBonus = 1,
                StartMiningBonus = 0,
                UnlockCost = new Dictionary<BlockType, int>
                {
                    { BlockType.Iron, 30 },
                    { BlockType.Silver, 10 }
                }
            },
            new CharacterStaticData
            {
                ID = CharacterID.Miner,
                NameKey = "char_miner_name",
                DescKey = "char_miner_desc",
                Passive = PassiveType.MiningBonus,
                PassiveValue = 1f,
                StartAttackBonus = 0,
                StartMiningBonus = 1,
                UnlockCost = new Dictionary<BlockType, int>
                {
                    { BlockType.Iron, 30 },
                    { BlockType.Silver, 20 },
                    { BlockType.Gold, 5 }
                }
            },
            new CharacterStaticData
            {
                ID = CharacterID.GraveRobber,
                NameKey = "char_graverobber_name",
                DescKey = "char_graverobber_desc",
                Passive = PassiveType.GraveRobberPassive,
                PassiveValue = 0.1f, // 10% chance
                StartAttackBonus = 0,
                StartMiningBonus = 0,
                UnlockCost = new Dictionary<BlockType, int>
                {
                    { BlockType.Silver, 50 },
                    { BlockType.Gold, 50 },
                    { BlockType.Diamond, 50 }
                }
            }
        };

        public static CharacterStaticData Get(CharacterID id)
        {
            return Characters.Find(c => c.ID == id);
        }
    }
}
