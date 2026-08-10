using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Core
{
    public class CharacterManager : MonoBehaviour
    {
        private static CharacterManager _instance;
        public static CharacterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CharacterManager");
                    _instance = go.AddComponent<CharacterManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

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

        public CharacterID SelectedCharacterID
        {
            get => SaveManager.CurrentData.SelectedCharacterID;
            set
            {
                if (IsUnlocked(value))
                {
                    SaveManager.CurrentData.SelectedCharacterID = value;
                    SaveManager.Save();
                    if (MetaProgressionManager.Instance != null)
                    {
                        MetaProgressionManager.Instance.TriggerMetaUpdated();
                    }
                }
            }
        }

        public CharacterSaveEntry GetCharacterProgress(CharacterID id)
        {
            var progress = SaveManager.CurrentData.CharacterProgress;
            var entry = progress.Find(c => c.ID == id);
            if (entry == null)
            {
                entry = new CharacterSaveEntry { ID = id, IsUnlocked = (id == CharacterID.Prisoner) };
                progress.Add(entry);
                SaveManager.Save();
            }
            return entry;
        }

        public float GetCurrentPassiveValue(CharacterID id)
        {
            var staticData = CharacterDatabase.Get(id);
            if (staticData == null || staticData.PassiveLevels == null || staticData.PassiveLevels.Count == 0) return 0f;
            int level = MetaProgressionManager.Instance?.GetPassiveLevel(id) ?? 0;
            if (level <= 0) return 0f;
            int idx = Mathf.Min(level - 1, staticData.PassiveLevels.Count - 1);
            return staticData.PassiveLevels[idx].Value;
        }

        public bool IsUnlocked(CharacterID id)
        {
            if (id == CharacterID.Prisoner) return true;
            return GetCharacterProgress(id).IsUnlocked;
        }

        public int GetUnlockWillCost(CharacterID id)
        {
            var staticData = CharacterDatabase.Get(id);
            return staticData?.UnlockWillCost ?? 0;
        }

        public bool CanUnlock(CharacterID id)
        {
            if (IsUnlocked(id)) return false;

            var staticData = CharacterDatabase.Get(id);
            if (staticData == null) return false;

            int willCost = GetUnlockWillCost(id);
            int ownedWill = MetaProgressionManager.Instance?.Will ?? 0;
            return ownedWill >= willCost;
        }

        public bool UnlockCharacter(CharacterID id)
        {
            if (!CanUnlock(id)) return false;

            var staticData = CharacterDatabase.Get(id);
            if (staticData == null) return false;

            int willCost = GetUnlockWillCost(id);
            if (MetaProgressionManager.Instance == null || !MetaProgressionManager.Instance.TrySpendWill(willCost))
                return false;

            var progress = GetCharacterProgress(id);
            progress.IsUnlocked = true;
            MetaProgressionManager.Instance?.InitializePassiveOnUnlock(id);

            SaveManager.Save();
            DeepEarth.Common.GameEvents.FireCharacterUnlocked(id);
            return true;
        }

        // 패시브 타입이 id의 실제 패시브와 일치할 때만 현재 레벨 값을 반환 — 아래 9개 getter가 공유하는 핵심 로직.
        private float GetPassiveValueIfType(CharacterID id, PassiveType type)
        {
            var data = CharacterDatabase.Get(id);
            return data?.Passive == type ? GetCurrentPassiveValue(id) : 0f;
        }

        public int GetPassiveAttackBonus(CharacterID id) => Mathf.RoundToInt(GetPassiveValueIfType(id, PassiveType.AttackBonus));

        public int GetPassiveMiningBonus(CharacterID id) => Mathf.RoundToInt(GetPassiveValueIfType(id, PassiveType.MiningBonus));

        public bool HasGraveRobberPassive(CharacterID id) => GetPassiveValueIfType(id, PassiveType.GraveRobberPassive) > 0f;

        // ── 신규 6개 패시브 조회 (Character Class System) ──────────────────────

        public float GetPassiveEventChoiceRoll(CharacterID id) => GetPassiveValueIfType(id, PassiveType.EventChoiceBonus);

        public float GetPassivePickaxeDurabilityReduction(CharacterID id) => GetPassiveValueIfType(id, PassiveType.PickaxeDurabilityReduction);

        public float GetPassivePotionHealBonus(CharacterID id) => GetPassiveValueIfType(id, PassiveType.PotionHealBonus);

        public bool HasTreasureRewardBonus(CharacterID id) => GetPassiveValueIfType(id, PassiveType.TreasureRewardBonus) > 0f;

        public float GetTreasureRelicChanceBonus(CharacterID id) => GetPassiveValueIfType(id, PassiveType.TreasureRewardBonus);

        public float GetPassiveCurseDurationReduction(CharacterID id) => GetPassiveValueIfType(id, PassiveType.CurseDurationReduction);

        public float GetPassiveLowHpAttackBonusMax(CharacterID id) => GetPassiveValueIfType(id, PassiveType.LowHpAttackBonus);
    }
}
