using UnityEngine;

namespace DeepEarth.Core
{
    public class StatusEffectModel
    {
        public string EffectID { get; }
        public int RemainingTurns { get; private set; }
        public int DamagePerTurn { get; }
        public StatusEffectData Data { get; }

        public float MiningPowerModifier => Data.miningPowerModifier;
        public float AttackModifier      => Data.attackModifier;
        public bool IsExpired => RemainingTurns <= 0;

        // 독 전용(그룹 I) — true면 Tick() 데미지가 고정값이 아니라 "틱 시점의 남은 턴수"가 된다.
        public bool DamageScalesWithRemainingTurns { get; }

        public StatusEffectModel(StatusEffectData data)
            : this(data, data.baseDuration, data.damagePerTurn) { }

        public StatusEffectModel(StatusEffectData data, int finalDuration, int finalDamage, bool damageScalesWithRemainingTurns = false)
        {
            Data = data;
            EffectID = data.effectID;
            RemainingTurns = finalDuration;
            DamagePerTurn = finalDamage;
            DamageScalesWithRemainingTurns = damageScalesWithRemainingTurns;
        }

        // Deals damage and decrements remaining turns. Returns damage dealt.
        public int Tick()
        {
            if (RemainingTurns <= 0) return 0;
            int dmg = DamageScalesWithRemainingTurns ? RemainingTurns : DamagePerTurn;
            RemainingTurns = Mathf.Max(0, RemainingTurns - 1);
            return dmg;
        }

        // 독 재적용 시 교체 대신 가산(그룹 I)
        public void ExtendDuration(int addedTurns) => RemainingTurns += Mathf.Max(0, addedTurns);
    }
}
