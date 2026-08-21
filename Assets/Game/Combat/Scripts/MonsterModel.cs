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

        // 실드 변화 훅. 인자는 (현재 실드, MaxHP) — 실드 바 UI가 HP 바와 같은 분모(MaxHP)로
        // 채움 비율을 계산할 수 있도록 MaxHP를 함께 넘긴다.
        public event Action<int, int> OnShieldChanged;

        public MonsterType Type { get; private set; }
        public MonsterData Data { get; private set; }
        public int SpawnDepth { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int Damage { get; private set; }
        public int Shield { get; private set; }

        // 턴제 전투(1단계, 일반 몬스터): 몬스터 자신의 방어 패턴 스텝 중 true.
        // IronPlateSpider(엘리트)의 방어구 흡수 배율 계산에서 여전히 참조하므로 유지한다.
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

        public DamageResult TakeDamage(int amount)
        {
            if (CurrentHP <= 0) return new DamageResult(0, 0);

            int shieldDamage = Mathf.Clamp(amount, 0, Shield);
            int hpDamage     = amount - shieldDamage;

            if (shieldDamage > 0)
            {
                Shield -= shieldDamage;
                OnShieldChanged?.Invoke(Shield, MaxHP);
            }

            CurrentHP = Mathf.Max(0, CurrentHP - hpDamage);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            return new DamageResult(shieldDamage, hpDamage);
        }

        // 방어 패턴 스텝 등으로 실드를 부여한다. 유저의 StatManager.AddShield와 동일한 흡수형 실드.
        public void AddShield(int amount)
        {
            if (amount <= 0) return;
            Shield += amount;
            OnShieldChanged?.Invoke(Shield, MaxHP);
        }

        // 실드는 단발성 — 이번 방어로 얻은 실드가 상대 공격 1회를 막지 못하고 남아있어도,
        // 자신의 턴이 다시 돌아오면(ExecuteTurnAsync 재진입 시) 소멸한다. 유저의
        // StatManager.ResetShield와 동일한 규칙.
        public void ResetShield()
        {
            if (Shield == 0) return;
            Shield = 0;
            OnShieldChanged?.Invoke(Shield, MaxHP);
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
