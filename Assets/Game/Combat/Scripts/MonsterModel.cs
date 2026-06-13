using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterModel
    {
        public MonsterType Type { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public float AttackInterval { get; private set; }
        public int Damage { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public MonsterModel(MonsterType type, int depth)
        {
            Type = type;
            AttackInterval = GameSettings.BaseAttackInterval;

            int difficultyLevel = GetDifficultyLevel(depth);

            switch (type)
            {
                case MonsterType.CaveRat:
                    MaxHP = 3;
                    Damage = 1 + difficultyLevel;
                    break;
                case MonsterType.CaveSpider:
                    MaxHP = 1;
                    Damage = 1 + difficultyLevel;
                    break;
            }

            CurrentHP = MaxHP;
        }

        private int GetDifficultyLevel(int depth)
        {
            if (depth < 50) return 1;
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
