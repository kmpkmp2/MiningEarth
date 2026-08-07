using UnityEngine;

namespace DeepEarth.Core
{
    public enum StatusEffectID
    {
        Burn,
        MiningPowerDown,
        MiningPowerUp,
        Poison,             // time-limited attack debuff (-10% per stack for N turns), 그룹 I부터 데미지도 부여
        PlayerAttackBuff,   // 그룹 C — 소비 아이템/포션 사용 시 부여되는 임시 공격력 버프(스택 가능)
    }

    [CreateAssetMenu(fileName = "StatusEffect_New", menuName = "DeepEarth/StatusEffect/StatusEffect")]
    public class StatusEffectData : ScriptableObject
    {
        public StatusEffectID effectType = StatusEffectID.Burn;
        public string effectID = "Burn";
        public string nameLocKey = "status_burn_name";
        public string descLocKey = "status_burn_desc";
        public string iconKey = "";
        [Min(1)] public int baseDuration = 8;
        public int damagePerTurn = 0;
        [Range(-1f, 1f)] public float miningPowerModifier = 0f;
        [Range(-1f, 1f)] public float attackModifier      = 0f;
        public EffectSystemType systemType = EffectSystemType.StatusEffect;
        public string source = "";
    }
}
