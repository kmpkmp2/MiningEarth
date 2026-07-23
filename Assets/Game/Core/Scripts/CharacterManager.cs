using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

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
            if (staticData == null || staticData.UnlockCost == null) return 0;

            staticData.UnlockCost.TryGetValue(BlockType.Iron, out int iron);
            staticData.UnlockCost.TryGetValue(BlockType.Silver, out int silver);
            staticData.UnlockCost.TryGetValue(BlockType.Gold, out int gold);
            staticData.UnlockCost.TryGetValue(BlockType.Diamond, out int diamond);

            return GameBalanceData.Instance.CalculateOreWillValue(iron, silver, gold, diamond);
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

        public int GetPassiveAttackBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            if (data?.Passive != PassiveType.AttackBonus) return 0;
            return Mathf.RoundToInt(GetCurrentPassiveValue(id));
        }

        public int GetPassiveMiningBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            if (data?.Passive != PassiveType.MiningBonus) return 0;
            return Mathf.RoundToInt(GetCurrentPassiveValue(id));
        }

        public bool HasGraveRobberPassive(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            return data?.Passive == PassiveType.GraveRobberPassive && GetCurrentPassiveValue(id) > 0f;
        }
    }
}
