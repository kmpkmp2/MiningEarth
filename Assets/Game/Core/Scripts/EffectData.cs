using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepEarth.Core
{
    public enum EffectSystemType
    {
        CharacterPassive,
        BossReward,
        Buff,
        Debuff,
        Special
    }

    [Serializable]
    public class EffectData
    {
        public string EffectID { get; set; }
        public string EffectNameKey { get; set; }
        public string DescriptionKey { get; set; }
        public EffectSystemType EffectType { get; set; }
        public float Value { get; set; }
        public string ValueDisplayString { get; set; }
        public string Source { get; set; }
        public string IconKey { get; set; }
        public int StackCount { get; set; } = 1;

        public string GetFormattedDescription()
        {
            if (LocalizationManager.Instance == null) return DescriptionKey;
            string rawDesc = LocalizationManager.Instance.GetTranslation(DescriptionKey);
            try
            {
                if (rawDesc.Contains("{0}"))
                {
                    return string.Format(rawDesc, Value);
                }
            }
            catch
            {
                // Fallback
            }
            return rawDesc;
        }

        public string GetTranslatedName()
        {
            if (LocalizationManager.Instance == null) return EffectNameKey;
            return LocalizationManager.Instance.GetTranslation(EffectNameKey);
        }
    }

    public class EffectCollection
    {
        private readonly List<EffectData> _effects = new List<EffectData>();

        public IReadOnlyList<EffectData> Effects => _effects;

        public void AddOrUpdate(EffectData newEffect)
        {
            var existing = _effects.Find(e => e.EffectID == newEffect.EffectID);
            if (existing != null)
            {
                existing.StackCount++;
                existing.Value += newEffect.Value;
                
                // Update display string based on accumulated value
                if (newEffect.EffectID.Contains("Spawn") || newEffect.EffectID.Contains("Chance") || newEffect.EffectID.Contains("Mineral"))
                {
                    existing.ValueDisplayString = $"{(existing.Value > 0 ? "+" : "")}{existing.Value}%";
                }
                else
                {
                    existing.ValueDisplayString = $"{(existing.Value > 0 ? "+" : "")}{existing.Value}";
                }
            }
            else
            {
                _effects.Add(newEffect);
            }
        }

        public bool Remove(string effectId)
        {
            int index = _effects.FindIndex(e => e.EffectID == effectId);
            if (index >= 0)
            {
                _effects.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _effects.Clear();
        }

        public List<EffectData> GetSortedEffects()
        {
            return _effects.OrderBy(e => (int)e.EffectType).ToList();
        }
    }
}
