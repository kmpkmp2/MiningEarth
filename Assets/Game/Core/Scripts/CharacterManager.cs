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

        public bool IsUnlocked(CharacterID id)
        {
            if (id == CharacterID.Prisoner) return true;
            return GetCharacterProgress(id).IsUnlocked;
        }

        public bool CanUnlock(CharacterID id)
        {
            if (IsUnlocked(id)) return false;

            var staticData = CharacterDatabase.Get(id);
            if (staticData == null) return false;

            foreach (var kvp in staticData.UnlockCost)
            {
                int owned = GetPersistentResource(kvp.Key);
                if (owned < kvp.Value) return false;
            }

            return true;
        }

        public bool UnlockCharacter(CharacterID id)
        {
            if (!CanUnlock(id)) return false;

            var staticData = CharacterDatabase.Get(id);
            if (staticData == null) return false;

            // Deduct resources
            foreach (var kvp in staticData.UnlockCost)
            {
                DeductPersistentResource(kvp.Key, kvp.Value);
            }

            var progress = GetCharacterProgress(id);
            progress.IsUnlocked = true;
            
            SaveManager.Save();
            return true;
        }

        public int GetPersistentResource(BlockType type)
        {
            switch (type)
            {
                case BlockType.Iron: return SaveManager.CurrentData.PersistentIron;
                case BlockType.Silver: return SaveManager.CurrentData.PersistentSilver;
                case BlockType.Gold: return SaveManager.CurrentData.PersistentGold;
                case BlockType.Diamond: return SaveManager.CurrentData.PersistentDiamond;
                default: return 0;
            }
        }

        private void DeductPersistentResource(BlockType type, int amount)
        {
            switch (type)
            {
                case BlockType.Iron:
                    SaveManager.CurrentData.PersistentIron = Mathf.Max(0, SaveManager.CurrentData.PersistentIron - amount);
                    break;
                case BlockType.Silver:
                    SaveManager.CurrentData.PersistentSilver = Mathf.Max(0, SaveManager.CurrentData.PersistentSilver - amount);
                    break;
                case BlockType.Gold:
                    SaveManager.CurrentData.PersistentGold = Mathf.Max(0, SaveManager.CurrentData.PersistentGold - amount);
                    break;
                case BlockType.Diamond:
                    SaveManager.CurrentData.PersistentDiamond = Mathf.Max(0, SaveManager.CurrentData.PersistentDiamond - amount);
                    break;
            }
        }

        public void AddPersistentResource(BlockType type, int amount)
        {
            if (amount <= 0) return;
            switch (type)
            {
                case BlockType.Iron: SaveManager.CurrentData.PersistentIron += amount; break;
                case BlockType.Silver: SaveManager.CurrentData.PersistentSilver += amount; break;
                case BlockType.Gold: SaveManager.CurrentData.PersistentGold += amount; break;
                case BlockType.Diamond: SaveManager.CurrentData.PersistentDiamond += amount; break;
            }
            SaveManager.Save();
        }

        // Stats integration
        public int GetStartingAttackBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            return data != null ? data.StartAttackBonus : 0;
        }

        public int GetStartingMiningBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            return data != null ? data.StartMiningBonus : 0;
        }

        public int GetPassiveAttackBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            if (data != null && data.Passive == PassiveType.AttackBonus)
            {
                return Mathf.RoundToInt(data.PassiveValue);
            }
            return 0;
        }

        public int GetPassiveMiningBonus(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            if (data != null && data.Passive == PassiveType.MiningBonus)
            {
                return Mathf.RoundToInt(data.PassiveValue);
            }
            return 0;
        }

        public bool HasGraveRobberPassive(CharacterID id)
        {
            var data = CharacterDatabase.Get(id);
            return data != null && data.Passive == PassiveType.GraveRobberPassive;
        }
    }
}
