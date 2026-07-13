using DeepEarth.Common;
using DeepEarth.Core;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterModel
    {
        public MonsterType Type { get; private set; }
        public MonsterData Data { get; private set; }
        public int SpawnDepth { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public float AttackInterval { get; private set; }
        public float InitialAttackDelay { get; private set; }
        public int Damage { get; private set; }
        public bool CanSplit { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public MonsterModel(MonsterData data, int depth)
        {
            Data           = data;
            Type           = data.monsterType;
            SpawnDepth     = depth;
            AttackInterval = data.attackInterval;
            CanSplit       = data.canSplit;
            InitialAttackDelay = data.initialAttackDelayMax > 0f
                ? UnityEngine.Random.Range(data.initialAttackDelayMin, data.initialAttackDelayMax)
                : 0f;

            int diffExtra = Mathf.Max(0, GetDifficultyLevel(depth) - 1);
            MaxHP   = data.baseMaxHP  + data.hpPerDifficultyLevel     * diffExtra;
            Damage  = data.baseDamage + data.damagePerDifficultyLevel  * diffExtra;
            CurrentHP = MaxHP;
        }

        // Custom-stats constructor for boss minions (baby spiders, summoned skeletons)
        public MonsterModel(MonsterType type, int maxHp, int damage, float attackInterval)
        {
            Type               = type;
            MaxHP              = maxHp;
            Damage             = damage;
            AttackInterval     = attackInterval;
            InitialAttackDelay = 0f;
            CanSplit           = false;
            CurrentHP          = maxHp;
        }

        // Fallback constructor if MonsterData is unavailable
        public MonsterModel(MonsterType type, int depth)
        {
            Type               = type;
            AttackInterval     = GameSettings.BaseAttackInterval;
            InitialAttackDelay = 0f;
            CanSplit           = false;

            int diff = GetDifficultyLevel(depth);
            switch (type)
            {
                case MonsterType.CaveRat:    MaxHP = 3; Damage = 1 + diff; break;
                case MonsterType.CaveSpider: MaxHP = 1; Damage = 1 + diff; break;
                default:                     MaxHP = 2; Damage = 1 + diff; break;
            }
            CurrentHP = MaxHP;
        }

        private int GetDifficultyLevel(int depth)
        {
            if (depth < 50)  return 1;
            if (depth < 100) return 2;
            if (depth < 200) return 3;
            return 4;
        }

        public bool TakeDamage(int amount)
        {
            if (CurrentHP <= 0) return false;
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
            return true;
        }
    }
}
