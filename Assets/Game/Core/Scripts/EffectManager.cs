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

            switch (id)
            {
                case CharacterID.Miner:
                    RegisterEffect(
                        "CharPassive_Miner",
                        "char_miner_name",
                        "effect_passive_miner_desc",
                        EffectSystemType.CharacterPassive,
                        1f,
                        "⚒1",
                        "Miner",
                        "Effect_CharacterPassive_Miner"
                    );
                    break;
                case CharacterID.Mercenary:
                    RegisterEffect(
                        "CharPassive_Mercenary",
                        "char_mercenary_name",
                        "effect_passive_mercenary_desc",
                        EffectSystemType.CharacterPassive,
                        1f,
                        "⚔1",
                        "Mercenary",
                        "Effect_CharacterPassive_Mercenary"
                    );
                    break;
                case CharacterID.GraveRobber:
                    RegisterEffect(
                        "CharPassive_GraveRobber",
                        "char_graverobber_name",
                        "effect_passive_graverobber_desc",
                        EffectSystemType.CharacterPassive,
                        10f,
                        "💎10%",
                        "Grave Robber",
                        "Effect_CharacterPassive_GraveRobber"
                    );
                    break;
            }
        }
    }
}
