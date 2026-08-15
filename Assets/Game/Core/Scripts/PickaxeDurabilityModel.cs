using System;
using UnityEngine;

namespace DeepEarth.Core
{
    public class PickaxeDurabilityModel
    {
        // 내구도가 이 비율 이하로 내려가면 파손 전 사전 경고 상태로 진입한다.
        // PickaxeDurabilityView의 기존 WarningThreshold(0.25f)와 동일한 값으로 맞춘다.
        private const float WarningThresholdRatio = 0.25f;

        public PickaxeData PickaxeData { get; private set; }
        public int CurrentDurability { get; private set; }
        public int MaxDurability { get; private set; }
        public bool IsBroken => CurrentDurability <= 0;
        public bool IsWarning => MaxDurability > 0 && CurrentDurability <= MaxDurability * WarningThresholdRatio;

        public event Action OnDurabilityChanged;
        public event Action OnPickaxeBroken;
        public event Action OnPickaxeRepaired;
        public event Action OnDurabilityWarning;
        public event Action OnDurabilityWarningCleared;

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
            bool wasWarning = IsWarning;
            CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
            OnDurabilityChanged?.Invoke();
            if (!wasWarning && IsWarning)
                OnDurabilityWarning?.Invoke();
            if (!wasBroken && IsBroken)
                OnPickaxeBroken?.Invoke();
        }

        public void Repair(int amount)
        {
            if (amount <= 0) return;
            bool wasBroken = IsBroken;
            bool wasWarning = IsWarning;
            CurrentDurability = Mathf.Min(MaxDurability, CurrentDurability + amount);
            OnDurabilityChanged?.Invoke();
            if (wasBroken && !IsBroken)
                OnPickaxeRepaired?.Invoke();
            if (wasWarning && !IsWarning)
                OnDurabilityWarningCleared?.Invoke();
        }

        // 유물: 최대 내구도 증가 (PickaxeMaxDurabilityBonus)
        public void AddMaxDurability(int amount)
        {
            if (amount <= 0) return;
            MaxDurability += amount;
            CurrentDurability = Mathf.Min(CurrentDurability + amount, MaxDurability);
            OnDurabilityChanged?.Invoke();
        }
    }
}
