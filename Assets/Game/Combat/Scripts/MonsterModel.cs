using System;
using DeepEarth.Common;
using DeepEarth.Core;
using UnityEngine;

namespace DeepEarth.Combat
{
    public class MonsterModel
    {
        // 보스 본체 HP바(BossView)처럼, 데미지 흐름과 UI를 이벤트로 분리하기 위한 훅.
        // 일반 몬스터/엘리트는 구독자가 없으므로 비용이 없다.
        public event Action<int, int> OnHPChanged;

        public MonsterType Type { get; private set; }
        public MonsterData Data { get; private set; }
        public int SpawnDepth { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int Damage { get; private set; }

        // 턴제 전투(1단계, 일반 몬스터): 몬스터 자신의 방어 패턴 스텝 중 true.
        public bool IsDefending { get; set; }

        public bool IsDead => CurrentHP <= 0;

        public MonsterModel(MonsterData data, int depth)
        {
            Data       = data;
            Type       = data.monsterType;
            SpawnDepth = depth;

            int diffExtra = Mathf.Max(0, GetDifficultyLevel(depth) - 1);
            MaxHP   = data.baseMaxHP  + data.hpPerDifficultyLevel     * diffExtra;
            Damage  = data.baseDamage + data.damagePerDifficultyLevel  * diffExtra;
            CurrentHP = MaxHP;
        }

        // Custom-stats constructor for boss minions (baby spiders, summoned skeletons, boss body/parts)
        public MonsterModel(MonsterType type, int maxHp, int damage)
        {
            Type      = type;
            MaxHP     = maxHp;
            Damage    = damage;
            CurrentHP = maxHp;
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
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
            return true;
        }

        // 몬스터 자힐(광산 균사체 등). 최대 체력을 넘지 않는다.
        public void Heal(int amount)
        {
            if (CurrentHP <= 0 || amount <= 0) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
