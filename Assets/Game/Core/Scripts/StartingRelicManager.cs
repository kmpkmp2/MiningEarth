using UnityEngine;

namespace DeepEarth.Core
{
    // 캐릭터 전용 시작 유물 매니저 — RelicManager(일반 유물 풀/추첨)와 완전히 분리된 시스템.
    // 캐릭터당 최대 1개, 런 시작 시 CharacterData.StartingRelic으로 고정 지급된다(추첨 없음).
    public class StartingRelicManager : MonoBehaviour
    {
        private static StartingRelicManager _instance;
        public static StartingRelicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StartingRelicManager");
                    _instance = go.AddComponent<StartingRelicManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private StartingRelicData _active;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ApplyForCharacter(CharacterData data)
        {
            ClearAll();
            _active = data?.StartingRelic;
            if (_active == null) return;

            EffectManager.Instance?.RegisterEffect(
                $"StartingRelic_{_active.relicID}",
                _active.nameLocKey,
                _active.descLocKey,
                EffectSystemType.CharacterPassive,
                0f,
                "",
                "StartingRelic",
                _active.iconKey);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[StartingRelic]\nGranted\nID : {_active.relicID}\nCharacter : {data.ID}");
#endif
        }

        public void ClearAll()
        {
            if (_active != null)
                EffectManager.Instance?.RemoveEffect($"StartingRelic_{_active.relicID}");
            _active = null;
        }

        private float Sum(StartingRelicEffectType type)
        {
            if (_active?.effects == null) return 0f;
            float total = 0f;
            foreach (var e in _active.effects)
                if (e.effectType == type) total += e.value;
            return total;
        }

        public float GetEventChoiceRollBonus()      => Sum(StartingRelicEffectType.EventChoiceRollBonus);
        public float GetPickaxeDurabilityReduction() => Sum(StartingRelicEffectType.PickaxeDurabilityReduction);
        public float GetPotionHealBonus()            => Sum(StartingRelicEffectType.PotionHealBonus);
        public float GetTreasureRewardBonus()        => Sum(StartingRelicEffectType.TreasureRewardBonus);
        public float GetCurseDurationReduction()     => Sum(StartingRelicEffectType.CurseDurationReduction);
        public float GetLowHpAttackBonus()           => Sum(StartingRelicEffectType.LowHpAttackBonus);
        public float GetMiningGainBonus()            => Sum(StartingRelicEffectType.MiningGainBonus);
        public int   GetFixedAttackBonus()           => Mathf.RoundToInt(Sum(StartingRelicEffectType.FixedAttackBonus));
    }
}
