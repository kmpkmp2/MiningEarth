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
    public class PassiveLevelEffect
    {
        public int Level;
        public string DescKey;
        public float Value;
        public int WillCost;
    }

    [Serializable]
    public class CharacterSaveEntry
    {
        public CharacterID ID;
        public bool IsUnlocked;
    }

    public class CharacterStaticData
    {
        public CharacterID ID;
        public string NameKey;
        public string DescKey;
        public PassiveType Passive;
        public string PassiveNameKey;
        public int BaseHPBonus;
        public int BaseMiningPowerBonus;
        public int BaseAttackPowerBonus;
        public List<PassiveLevelEffect> PassiveLevels = new List<PassiveLevelEffect>();
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
                PassiveNameKey = "",
                BaseHPBonus = 0,
                BaseMiningPowerBonus = 0,
                BaseAttackPowerBonus = 0,
                PassiveLevels = new List<PassiveLevelEffect>(),
                UnlockCost = new Dictionary<BlockType, int>()
            },
            new CharacterStaticData
            {
                ID = CharacterID.Mercenary,
                NameKey = "char_mercenary_name",
                DescKey = "char_mercenary_desc",
                Passive = PassiveType.AttackBonus,
                PassiveNameKey = "passive_mercenary_name",
                BaseHPBonus = 0,
                BaseMiningPowerBonus = 0,
                BaseAttackPowerBonus = 0,
                PassiveLevels = new List<PassiveLevelEffect>
                {
                    new PassiveLevelEffect { Level = 1, DescKey = "passive_mercenary_lv1_desc", Value = 1f, WillCost = 20 },
                    new PassiveLevelEffect { Level = 2, DescKey = "passive_mercenary_lv2_desc", Value = 2f, WillCost = 40 },
                    new PassiveLevelEffect { Level = 3, DescKey = "passive_mercenary_lv3_desc", Value = 3f, WillCost = 60 },
                    new PassiveLevelEffect { Level = 4, DescKey = "passive_mercenary_lv4_desc", Value = 4f, WillCost = 80 },
                    new PassiveLevelEffect { Level = 5, DescKey = "passive_mercenary_lv5_desc", Value = 5f, WillCost = 100 },
                },
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
                PassiveNameKey = "passive_miner_name",
                BaseHPBonus = 0,
                BaseMiningPowerBonus = 0,
                BaseAttackPowerBonus = 0,
                PassiveLevels = new List<PassiveLevelEffect>
                {
                    new PassiveLevelEffect { Level = 1, DescKey = "passive_miner_lv1_desc", Value = 1f, WillCost = 20 },
                    new PassiveLevelEffect { Level = 2, DescKey = "passive_miner_lv2_desc", Value = 2f, WillCost = 40 },
                    new PassiveLevelEffect { Level = 3, DescKey = "passive_miner_lv3_desc", Value = 3f, WillCost = 60 },
                    new PassiveLevelEffect { Level = 4, DescKey = "passive_miner_lv4_desc", Value = 4f, WillCost = 80 },
                    new PassiveLevelEffect { Level = 5, DescKey = "passive_miner_lv5_desc", Value = 5f, WillCost = 100 },
                },
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
                PassiveNameKey = "passive_graverobber_name",
                BaseHPBonus = 0,
                BaseMiningPowerBonus = 0,
                BaseAttackPowerBonus = 0,
                PassiveLevels = new List<PassiveLevelEffect>
                {
                    new PassiveLevelEffect { Level = 1, DescKey = "passive_graverobber_lv1_desc", Value = 0.10f, WillCost = 20 },
                    new PassiveLevelEffect { Level = 2, DescKey = "passive_graverobber_lv2_desc", Value = 0.15f, WillCost = 40 },
                    new PassiveLevelEffect { Level = 3, DescKey = "passive_graverobber_lv3_desc", Value = 0.20f, WillCost = 60 },
                    new PassiveLevelEffect { Level = 4, DescKey = "passive_graverobber_lv4_desc", Value = 0.25f, WillCost = 80 },
                    new PassiveLevelEffect { Level = 5, DescKey = "passive_graverobber_lv5_desc", Value = 0.30f, WillCost = 100 },
                },
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
