using System;
using System.Collections.Generic;
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

        private GlobalUpgradeData GlobalUpgrade
        {
            get
            {
                var data = SaveManager.CurrentData;
                if (data.GlobalUpgrade == null)
                    data.GlobalUpgrade = new GlobalUpgradeData();
                return data.GlobalUpgrade;
            }
        }

        public int Will => SaveManager.CurrentData.Will;
        public int MiningPowerLevel => GlobalUpgrade.MiningPowerLevel;
        public int MaxHPLevel => GlobalUpgrade.MaxHPLevel;
        public int AttackLevel => GlobalUpgrade.AttackLevel;
        public int InventorySizeLevel => GlobalUpgrade.InventorySizeLevel;
        public int PickaxeDurabilityLevel => GlobalUpgrade.PickaxeDurabilityLevel;

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
                case UpgradeType.MiningPower:    return MiningPowerLevel * 10;
                case UpgradeType.MaxHP:          return MaxHPLevel * 8;
                case UpgradeType.Attack:         return AttackLevel * 5;
                case UpgradeType.InventorySize:
                    if (InventorySizeLevel >= 5) return int.MaxValue;
                    return (InventorySizeLevel + 1) * 6;
                case UpgradeType.PickaxeDurability:
                    return (PickaxeDurabilityLevel + 1) * 8;
                default:
                    return int.MaxValue;
            }
        }

        public bool Upgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            if (Will < cost) return false;

            SaveManager.CurrentData.Will -= cost;
            var upgrade = GlobalUpgrade;

            switch (type)
            {
                case UpgradeType.MiningPower:    upgrade.MiningPowerLevel++;    break;
                case UpgradeType.MaxHP:          upgrade.MaxHPLevel++;          break;
                case UpgradeType.Attack:         upgrade.AttackLevel++;         break;
                case UpgradeType.InventorySize:
                    if (upgrade.InventorySizeLevel < 5)
                    {
                        upgrade.InventorySizeLevel++;
                        Debug.Log($"[Inventory]\nUpgrade Bonus : +{upgrade.InventorySizeLevel * 4}");
                    }
                    break;
                case UpgradeType.PickaxeDurability:
                    upgrade.PickaxeDurabilityLevel++;
                    Debug.Log($"[Pickaxe]\nUpgrade\nNew Level : {upgrade.PickaxeDurabilityLevel}\nBonus : +{upgrade.PickaxeDurabilityLevel * 10}%");
                    break;
            }

            SaveManager.Save();
            OnMetaUpdated?.Invoke();
            return true;
        }

        // ── Passive Level Management ──────────────────────────────────

        public int GetPassiveLevel(CharacterID id)
        {
            var list = SaveManager.CurrentData.CharacterPassiveLevels;
            if (list == null) return 0;
            var entry = list.Find(e => e.ID == id);
            return entry?.PassiveLevel ?? 0;
        }

        public int GetPassiveMaxLevel(CharacterID id)
        {
            return CharacterDatabase.Get(id)?.PassiveLevels?.Count ?? 0;
        }

        public int GetPassiveUpgradeCost(CharacterID id)
        {
            var staticData = CharacterDatabase.Get(id);
            int level = GetPassiveLevel(id);
            if (staticData == null || staticData.PassiveLevels == null || level >= staticData.PassiveLevels.Count)
                return int.MaxValue;
            return staticData.PassiveLevels[level].WillCost;
        }

        public bool UpgradePassive(CharacterID id)
        {
            int cost = GetPassiveUpgradeCost(id);
            if (cost == int.MaxValue || SaveManager.CurrentData.Will < cost) return false;

            SaveManager.CurrentData.Will -= cost;
            SetPassiveLevel(id, GetPassiveLevel(id) + 1);
            SaveManager.Save();
            OnMetaUpdated?.Invoke();

            Debug.Log($"[Upgrade]\nPassive : {id}\nNew Level : {GetPassiveLevel(id)}");
            return true;
        }

        private void SetPassiveLevel(CharacterID id, int level)
        {
            var list = SaveManager.CurrentData.CharacterPassiveLevels;
            if (list == null)
            {
                list = new List<CharacterPassiveSaveEntry>();
                SaveManager.CurrentData.CharacterPassiveLevels = list;
            }
            var entry = list.Find(e => e.ID == id);
            if (entry == null)
            {
                entry = new CharacterPassiveSaveEntry { ID = id };
                list.Add(entry);
            }
            entry.PassiveLevel = level;
        }

        public void ResetProgress()
        {
            SaveManager.ResetToDefault();
            OnMetaUpdated?.Invoke();
        }
    }
}
