using System;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class MetaProgressionManager : MonoBehaviour
    {
        private static MetaProgressionManager _instance;
        public static MetaProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MetaProgressionManager");
                    _instance = go.AddComponent<MetaProgressionManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public int Will => SaveManager.CurrentData.Will;
        public int MiningPowerLevel => CharacterManager.Instance.GetCharacterProgress(CharacterManager.Instance.SelectedCharacterID).UpgradeData.MiningPowerLevel;
        public int MaxHPLevel => CharacterManager.Instance.GetCharacterProgress(CharacterManager.Instance.SelectedCharacterID).UpgradeData.MaxHPLevel;
        public int AttackLevel => CharacterManager.Instance.GetCharacterProgress(CharacterManager.Instance.SelectedCharacterID).UpgradeData.AttackLevel;
        public int InventorySizeLevel => CharacterManager.Instance.GetCharacterProgress(CharacterManager.Instance.SelectedCharacterID).UpgradeData.InventorySizeLevel;

        public event Action OnMetaUpdated;

        public void TriggerMetaUpdated() => OnMetaUpdated?.Invoke();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                SaveManager.Load();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddWill(int amount)
        {
            SaveManager.CurrentData.Will += amount;
            SaveManager.Save();
            OnMetaUpdated?.Invoke();
        }

        public int GetUpgradeCost(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MiningPower:
                    return MiningPowerLevel * 10;
                case UpgradeType.MaxHP:
                    return MaxHPLevel * 8;
                case UpgradeType.Attack:
                    return AttackLevel * 5;
                case UpgradeType.InventorySize:
                    if (InventorySizeLevel >= 5) return int.MaxValue;
                    return (InventorySizeLevel + 1) * 6;
                default:
                    return int.MaxValue;
            }
        }

        public bool Upgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            if (Will >= cost)
            {
                SaveManager.CurrentData.Will -= cost;
                var progress = CharacterManager.Instance.GetCharacterProgress(CharacterManager.Instance.SelectedCharacterID);
                switch (type)
                {
                    case UpgradeType.MiningPower:
                        progress.UpgradeData.MiningPowerLevel++;
                        break;
                    case UpgradeType.MaxHP:
                        progress.UpgradeData.MaxHPLevel++;
                        break;
                    case UpgradeType.Attack:
                        progress.UpgradeData.AttackLevel++;
                        break;
                    case UpgradeType.InventorySize:
                        if (progress.UpgradeData.InventorySizeLevel < 5)
                        {
                            progress.UpgradeData.InventorySizeLevel++;
                            Debug.Log($"[Inventory]\nUpgrade Bonus : +{progress.UpgradeData.InventorySizeLevel * 4}");
                        }
                        break;
                }
                SaveManager.Save();
                OnMetaUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public void ResetProgress()
        {
            SaveManager.ResetToDefault();
            OnMetaUpdated?.Invoke();
        }
    }
}
