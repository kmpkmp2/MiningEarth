using UnityEngine;

namespace DeepEarth.Core
{
    public class StatusEffectModel
    {
        public string EffectID { get; }
        public int RemainingTurns { get; private set; }
        public int DamagePerTurn { get; }
        public StatusEffectData Data { get; }

        public bool IsExpired => RemainingTurns <= 0;

        public StatusEffectModel(StatusEffectData data)
            : this(data, data.baseDuration, data.damagePerTurn) { }

        public StatusEffectModel(StatusEffectData data, int finalDuration, int finalDamage)
        {
            Data = data;
            EffectID = data.effectID;
            RemainingTurns = finalDuration;
            DamagePerTurn = finalDamage;
        }

        // Deals damage and decrements remaining turns. Returns damage dealt.
        public int Tick()
        {
            if (RemainingTurns <= 0) return 0;
            int dmg = DamagePerTurn;
            RemainingTurns = Mathf.Max(0, RemainingTurns - 1);
            return dmg;
        }
    }
}
