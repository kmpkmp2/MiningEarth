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

        // addressableKey가 가리키는 프리팹(스프라이트 포함)을 다른 몬스터와 공유하면서
        // 겉모습만 구분하고 싶을 때 쓰는 Addressable Sprite 키. 비어있으면 프리팹에 내장된
        // 기본 스프라이트를 그대로 쓴다. (예: 엘리트 빅 슬라임이 일반 슬라임 프리팹을 공유하되
        // 전용 스프라이트로 덮어쓰는 경우)
        public string spriteOverrideKey;

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

        [Header("Encounter Reveal: Death Trigger Warning")]
        // 비어있지 않으면 조우 리빌 화면에 서브타이틀로 노출된다(예: 슬라임의 분열 경고).
        // canSplit/hasDeathDebuff/hasDeathReward 같은 실제 로직 플래그와는 별개로,
        // "플레이어에게 미리 알려줄 가치가 있는가"만 이 필드 하나로 판단한다.
        public string deathTriggerDescKey;
    }
}
