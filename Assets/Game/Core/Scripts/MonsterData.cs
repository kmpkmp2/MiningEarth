using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Combat;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "MonsterData_New", menuName = "DeepEarth/Monster/MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [Header("Identity")]
        public MonsterType monsterType;
        public string nameLocKey;
        public string addressableKey;

        [Header("Elite: Spawn Depth Range")]
        [Min(0)] public int spawnDepthMin = 0;
        [Min(-1)] public int spawnDepthMax = -1; // -1 = no upper limit

        [Header("Elite: Skill")]
        public EliteSkillType eliteSkillType = EliteSkillType.None;
        public EliteRewardTable eliteRewardTable;

        [Header("Base Stats")]
        [Min(1)] public int baseMaxHP = 3;
        [Min(1)] public int baseDamage = 1;

        [Header("Difficulty Scaling (per level above 1)")]
        public int hpPerDifficultyLevel = 0;
        public int damagePerDifficultyLevel = 0;

        [Header("Spawn Configuration")]
        [Min(1)] public int spawnCount = 1;
        public Vector3[] spawnOffsets = new Vector3[0];

        [Header("Slime: Split on Death")]
        public bool canSplit = false;
        public MonsterType splitIntoType = MonsterType.SmallSlime;
        [Min(2)] public int splitCount = 2;

        [Header("Skeleton: Death Debuff")]
        public bool hasDeathDebuff = false;
        [Range(0f, 1f)] public float deathDebuffChance = 0.25f;
        public StatusEffectData deathDebuffEffect;

        [Header("Mimic: Death Reward")]
        public bool hasDeathReward = false;
        public MimicRewardTable rewardTable;
    }
}
