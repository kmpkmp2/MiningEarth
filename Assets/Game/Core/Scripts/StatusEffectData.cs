using UnityEngine;

namespace DeepEarth.Core
{
    public enum StatusEffectID
    {
        Burn,
        // Future: Poison, Freeze, Bleeding, Weakness
    }

    [CreateAssetMenu(fileName = "StatusEffect_Burn", menuName = "DeepEarth/StatusEffect/Burn")]
    public class StatusEffectData : ScriptableObject
    {
        public StatusEffectID effectType = StatusEffectID.Burn;
        public string effectID = "Burn";
        public string nameLocKey = "status_burn_name";
        public string descLocKey = "status_burn_desc";
        public string iconKey = "";
        [Min(1)] public int baseDuration = 8;
        [Min(1)] public int damagePerTurn = 1;
    }
}
