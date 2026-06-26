using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public enum AchievementType
    {
        MonsterKill,
        OreMined,
        BossKill,
        ReachDepth,
        PickaxeRepair,
        RepairWithOre,
        UnlockCharacter,
        CollectRelic,
        RunCount,
        DeathCount,
        TreasureOpen,
        TombstoneOpen,
        LavaEncounter,
        WaterEncounter,
        Custom
    }

    public enum RewardType { None, Gold, Gem, Resource, Character, Relic }

    [CreateAssetMenu(fileName = "Achievement_New", menuName = "DeepEarth/Achievement/AchievementData")]
    public class AchievementData : ScriptableObject
    {
        [Header("Identity")]
        public string achievementID;
        public string nameLocKey;
        public string descLocKey;
        public string iconKey;

        [Header("Condition")]
        public AchievementType type;
        public int targetValue = 1;

        [Tooltip("OreMined / RepairWithOre: which ore type to count")]
        public BlockType targetOreType;

        [Tooltip("BossKill: BossID string ('CaveRat', 'QueenSpider' …). Leave empty to count any boss.")]
        public string targetBossID;

        [Header("Reward")]
        public RewardType rewardType = RewardType.None;
        public int rewardValue;

        [Header("Display")]
        public bool isHidden;
    }
}
