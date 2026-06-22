using System;
using UnityEngine;

namespace DeepEarth.Core
{
    public class PickaxeDurabilityModel
    {
        public PickaxeData PickaxeData { get; private set; }
        public int CurrentDurability { get; private set; }
        public int MaxDurability { get; private set; }
        public bool IsBroken => CurrentDurability <= 0;

        public event Action OnDurabilityChanged;
        public event Action OnPickaxeBroken;
        public event Action OnPickaxeRepaired;

        public PickaxeDurabilityModel(PickaxeData data, int maxDurability)
        {
            PickaxeData = data;
            MaxDurability = maxDurability;
            CurrentDurability = maxDurability;
        }

        public void LoseDurability(int amount)
        {
            if (amount <= 0) return;
            bool wasBroken = IsBroken;
            CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
            OnDurabilityChanged?.Invoke();
            if (!wasBroken && IsBroken)
                OnPickaxeBroken?.Invoke();
        }

        public void Repair(int amount)
        {
            if (amount <= 0) return;
            bool wasBroken = IsBroken;
            CurrentDurability = Mathf.Min(MaxDurability, CurrentDurability + amount);
            OnDurabilityChanged?.Invoke();
            if (wasBroken && !IsBroken)
                OnPickaxeRepaired?.Invoke();
        }
    }
}
