using System;
using System.IO;
using UnityEngine;

namespace DeepEarth.Core
{
    [Serializable]
    public class SaveData
    {
        public int Will;
        public int MiningPowerLevel;
        public int MaxHPLevel;
        public int InventorySizeLevel;
        public string Language;
        public System.Collections.Generic.List<string> UnlockedCharacters = new System.Collections.Generic.List<string>();
        public int BestDepth;

        // Character Selection and Progression
        public CharacterID SelectedCharacterID;
        public int PersistentIron;
        public int PersistentSilver;
        public int PersistentGold;
        public int PersistentDiamond;
        public System.Collections.Generic.List<CharacterSaveEntry> CharacterProgress = new System.Collections.Generic.List<CharacterSaveEntry>();

        public void InitializeDefault()
        {
            Will = 0;
            MiningPowerLevel = 1;
            MaxHPLevel = 1;
            InventorySizeLevel = 1;
            Language = (Application.systemLanguage == SystemLanguage.Korean) ? "ko" : "en";
            UnlockedCharacters = new System.Collections.Generic.List<string> { "Prisoner" };
            BestDepth = 0;

            SelectedCharacterID = CharacterID.Prisoner;
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
                        // Ensure compatibility for existing older saves
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
                Debug.Log($"Game saved successfully at {SaveFilePath}");
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
