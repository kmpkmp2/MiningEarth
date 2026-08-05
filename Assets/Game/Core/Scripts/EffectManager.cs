using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class EffectManager : MonoBehaviour
    {
        private static EffectManager _instance;
        public static EffectManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EffectManager");
                    _instance = go.AddComponent<EffectManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly EffectCollection _collection = new EffectCollection();

        public event Action OnEffectsChanged;

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

        public List<EffectData> GetActiveEffects()
        {
            return _collection.GetSortedEffects();
        }

        public void RegisterEffect(string id, string nameKey, string descKey, EffectSystemType type, float value, string display, string source, string iconKey)
        {
            var effect = new EffectData
            {
                EffectID = id,
                EffectNameKey = nameKey,
                DescriptionKey = descKey,
                EffectType = type,
                Value = value,
                ValueDisplayString = display,
                Source = source,
                IconKey = iconKey
            };

            _collection.AddOrUpdate(effect);
            OnEffectsChanged?.Invoke();
            Debug.Log($"EffectManager: Registered/Updated effect: {id}");
            
            if (type != EffectSystemType.CharacterPassive)
            {
                Debug.Log("[Inventory]\nRelic Acquired\nInventory Capacity Unchanged");
            }
        }

        public void RemoveEffect(string id)
        {
            if (_collection.Remove(id))
            {
                OnEffectsChanged?.Invoke();
                Debug.Log($"EffectManager: Removed effect: {id}");
            }
        }

        public void UpdateEffectDisplay(string id, string displayString, float value)
        {
            if (_collection.UpdateDisplay(id, displayString, value))
            {
                OnEffectsChanged?.Invoke();
            }
        }

        public void ClearRunEffects()
        {
            var active = _collection.Effects;
            var toRemove = new List<string>();
            foreach (var eff in active)
            {
                if (eff.EffectType != EffectSystemType.CharacterPassive)
                {
                    toRemove.Add(eff.EffectID);
                }
            }

            bool changed = false;
            foreach (var id in toRemove)
            {
                if (_collection.Remove(id)) changed = true;
            }

            if (changed)
            {
                OnEffectsChanged?.Invoke();
                Debug.Log("EffectManager: Cleared all run-local effects.");
            }
        }

        public void InitializeCharacterPassive(CharacterID id)
        {
            var active = _collection.Effects;
            var toRemove = new List<string>();
            foreach (var eff in active)
            {
                if (eff.EffectType == EffectSystemType.CharacterPassive)
                {
                    toRemove.Add(eff.EffectID);
                }
            }
            foreach (var pId in toRemove)
            {
                _collection.Remove(pId);
            }

            var data = CharacterDatabase.Get(id);
            if (data == null || data.Passive == PassiveType.None) return;

            float value = CharacterManager.Instance.GetCurrentPassiveValue(id);
            string display = BuildPassiveDisplayString(data, value);

            RegisterEffect(
                $"CharPassive_{id}",
                data.NameKey,
                data.PassiveDescKey,
                EffectSystemType.CharacterPassive,
                value,
                display,
                id.ToString(),
                $"Effect_CharacterPassive_{id}"
            );
        }

        // 표시 포맷팅 전용 — 실제 수치 로직에는 관여하지 않는다 (CharacterManager.GetCurrentPassiveValue가 진실의 원천).
        // 아이콘/포맷은 CharacterData(PassiveHudIcon/PassiveHudFormat/PassiveValueIsPercent) 데이터로 관리된다.
        private static string BuildPassiveDisplayString(CharacterData data, float value)
        {
            if (string.IsNullOrEmpty(data.PassiveHudIcon)) return "";
            float shown = data.PassiveValueIsPercent ? value * 100f : value;
            string format = string.IsNullOrEmpty(data.PassiveHudFormat) ? "{0:0}" : data.PassiveHudFormat;
            return data.PassiveHudIcon + string.Format(format, shown);
        }
    }
}
