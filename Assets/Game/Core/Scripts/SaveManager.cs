using System;
using System.IO;
using UnityEngine;

namespace DeepEarth.Core
{
    [Serializable]
    public class AchievementSaveEntry
    {
        public string AchievementID;
        public int    CurrentProgress;
        public bool   IsCompleted;
    }

    [Serializable]
    public class GlobalUpgradeData
    {
        public int MiningPowerLevel = 1;
        public int MaxHPLevel = 1;
        public int AttackLevel = 1;
        public int InventorySizeLevel = 0;
        public int PickaxeDurabilityLevel = 0;
        public int RepairEfficiencyLevel = 0;
        public int LuckLevel = 0;
        public int EventRateLevel = 0;
    }

    [Serializable]
    public class CharacterPassiveSaveEntry
    {
        public CharacterID ID;
        public int PassiveLevel;
    }

    [Serializable]
    public class SaveData
    {
        public int Will;
        public string Language;
        public System.Collections.Generic.List<string> UnlockedCharacters = new System.Collections.Generic.List<string>();
        public int BestDepth;

        // Character Selection
        public CharacterID SelectedCharacterID;
        public int PersistentStone;
        public int PersistentWood;
        public int PersistentIron;
        public int PersistentSilver;
        public int PersistentGold;
        public int PersistentDiamond;

        // Character unlock tracking only (no per-character upgrade data)
        public System.Collections.Generic.List<CharacterSaveEntry> CharacterProgress = new System.Collections.Generic.List<CharacterSaveEntry>();

        // Global upgrades shared by all characters
        public GlobalUpgradeData GlobalUpgrade = new GlobalUpgradeData();

        // Per-character passive levels
        public System.Collections.Generic.List<CharacterPassiveSaveEntry> CharacterPassiveLevels = new System.Collections.Generic.List<CharacterPassiveSaveEntry>();

        public System.Collections.Generic.List<AchievementSaveEntry> AchievementProgress = new System.Collections.Generic.List<AchievementSaveEntry>();

        // Pickaxe Shop
        public string EquippedPickaxeID = "pickaxe_wood";
        public System.Collections.Generic.List<string> UnlockedPickaxeIDs = new System.Collections.Generic.List<string>();

        public void InitializeDefault()
        {
            Will = 0;
            Language = (Application.systemLanguage == SystemLanguage.Korean) ? "ko" : "en";
            UnlockedCharacters = new System.Collections.Generic.List<string> { "Prisoner" };
            BestDepth = 0;

            SelectedCharacterID = CharacterID.Prisoner;
            PersistentStone = 0;
            PersistentWood = 0;
            PersistentIron = 0;
            PersistentSilver = 0;
            PersistentGold = 0;
            PersistentDiamond = 0;

            CharacterProgress = new System.Collections.Generic.List<CharacterSaveEntry>
            {
                new CharacterSaveEntry { ID = CharacterID.Prisoner, IsUnlocked = true },
                new CharacterSaveEntry { ID = CharacterID.Mercenary, IsUnlocked = false },
                new CharacterSaveEntry { ID = CharacterID.Miner, IsUnlocked = false },
                new CharacterSaveEntry { ID = CharacterID.GraveRobber, IsUnlocked = false }
            };

            GlobalUpgrade = new GlobalUpgradeData();
            CharacterPassiveLevels = new System.Collections.Generic.List<CharacterPassiveSaveEntry>();
            AchievementProgress = new System.Collections.Generic.List<AchievementSaveEntry>();

            EquippedPickaxeID = "pickaxe_wood";
            UnlockedPickaxeIDs = new System.Collections.Generic.List<string> { "pickaxe_wood" };
        }
    }

    public static class SaveManager
    {
        private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "savedata.json");
        private static SaveData _cachedData;

        public static SaveData CurrentData
        {
            get
            {
                if (_cachedData == null)
                {
                    Load();
                }
                return _cachedData;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _cachedData = JsonUtility.FromJson<SaveData>(json);

                    if (_cachedData == null)
                    {
                        Debug.LogWarning("Save file corrupted, initializing default data.");
                        ResetToDefault();
                    }
                    else
                    {
                        MigrateIfNeeded();
                    }
                }
                else
                {
                    Debug.Log("Save file not found, creating new default save.");
                    ResetToDefault();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load save data: {ex.Message}. Resetting to default.");
                ResetToDefault();
            }
        }

        private static void MigrateIfNeeded()
        {
            if (_cachedData.AchievementProgress == null)
                _cachedData.AchievementProgress = new System.Collections.Generic.List<AchievementSaveEntry>();

            if (_cachedData.UnlockedPickaxeIDs == null || _cachedData.UnlockedPickaxeIDs.Count == 0)
            {
                _cachedData.UnlockedPickaxeIDs = new System.Collections.Generic.List<string> { "pickaxe_wood" };
                if (string.IsNullOrEmpty(_cachedData.EquippedPickaxeID))
                    _cachedData.EquippedPickaxeID = "pickaxe_wood";
            }

            if (_cachedData.CharacterProgress == null || _cachedData.CharacterProgress.Count == 0)
            {
                _cachedData.SelectedCharacterID = CharacterID.Prisoner;
                _cachedData.CharacterProgress = new System.Collections.Generic.List<CharacterSaveEntry>
                {
                    new CharacterSaveEntry { ID = CharacterID.Prisoner, IsUnlocked = true },
                    new CharacterSaveEntry { ID = CharacterID.Mercenary, IsUnlocked = false },
                    new CharacterSaveEntry { ID = CharacterID.Miner, IsUnlocked = false },
                    new CharacterSaveEntry { ID = CharacterID.GraveRobber, IsUnlocked = false }
                };
            }

            // Migration: GlobalUpgrade (replaces per-character UpgradeData)
            if (_cachedData.GlobalUpgrade == null)
                _cachedData.GlobalUpgrade = new GlobalUpgradeData();

            // Migration: CharacterPassiveLevels
            if (_cachedData.CharacterPassiveLevels == null)
                _cachedData.CharacterPassiveLevels = new System.Collections.Generic.List<CharacterPassiveSaveEntry>();
        }

        public static void Save()
        {
            if (_cachedData == null)
            {
                _cachedData = new SaveData();
                _cachedData.InitializeDefault();
            }

            try
            {
                string json = JsonUtility.ToJson(_cachedData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log("[Save]\nSave Complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save data: {ex.Message}");
            }
        }

        public static void ResetToDefault()
        {
            _cachedData = new SaveData();
            _cachedData.InitializeDefault();
            Save();
        }
    }
}
