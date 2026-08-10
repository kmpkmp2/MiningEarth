using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    public class PickaxeManager : MonoBehaviour
    {
        private static PickaxeManager _instance;
        public static PickaxeManager Instance => _instance;

        private readonly List<PickaxeData> _allPickaxes = new List<PickaxeData>();
        private bool _initialized;

        public event Action OnPickaxeStateChanged;

        public IReadOnlyList<PickaxeData> AllPickaxes => _allPickaxes;
        public PickaxeData EquippedPickaxeData => GetPickaxeByID(SaveManager.CurrentData.EquippedPickaxeID);

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

        public async UniTask InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;

            var pickaxes = await ResourceManager.Instance.LoadAllByLabelAsync<PickaxeData>(AddressableKeys.LabelPickaxe);
            _allPickaxes.Clear();
            if (pickaxes != null)
                _allPickaxes.AddRange(pickaxes.OrderBy(p => p.miningPower));

            var save = SaveManager.CurrentData;
            if (save.UnlockedPickaxeIDs == null)
                save.UnlockedPickaxeIDs = new List<string>();

            foreach (var p in _allPickaxes.Where(p => p.isDefault))
            {
                if (!save.UnlockedPickaxeIDs.Contains(p.pickaxeID))
                    save.UnlockedPickaxeIDs.Add(p.pickaxeID);
            }

            if (string.IsNullOrEmpty(save.EquippedPickaxeID) ||
                !save.UnlockedPickaxeIDs.Contains(save.EquippedPickaxeID))
            {
                var def = _allPickaxes.FirstOrDefault(p => p.isDefault) ?? _allPickaxes.FirstOrDefault();
                save.EquippedPickaxeID = def?.pickaxeID ?? "";
            }

            SaveManager.Save();
            Debug.Log($"[Pickaxe]\nManager Initialized\nTotal : {_allPickaxes.Count}\nEquipped : {save.EquippedPickaxeID}");
        }

        public PickaxeData GetPickaxeByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _allPickaxes.FirstOrDefault(p => p.pickaxeID == id);
        }

        public bool IsUnlocked(string pickaxeID) =>
            SaveManager.CurrentData.UnlockedPickaxeIDs?.Contains(pickaxeID) ?? false;

        public int GetUnlockWillCost(PickaxeData data)
        {
            return data?.unlockWillCost ?? 0;
        }

        public bool CanAfford(PickaxeData data)
        {
            if (data == null || data.isDefault) return false;
            int willCost = GetUnlockWillCost(data);
            int ownedWill = MetaProgressionManager.Instance?.Will ?? 0;
            return ownedWill >= willCost;
        }

        public bool Purchase(PickaxeData data)
        {
            if (data == null || IsUnlocked(data.pickaxeID)) return false;
            if (!CanAfford(data)) return false;

            int willCost = GetUnlockWillCost(data);
            if (MetaProgressionManager.Instance == null || !MetaProgressionManager.Instance.TrySpendWill(willCost))
                return false;

            var save = SaveManager.CurrentData;
            save.UnlockedPickaxeIDs.Add(data.pickaxeID);
            SaveManager.Save();
            OnPickaxeStateChanged?.Invoke();

            string name = GetLocalizedName(data);
            Debug.Log($"[Pickaxe]\nPurchased\nPickaxe : {name}");
            return true;
        }

        public void Equip(PickaxeData data)
        {
            if (data == null || !IsUnlocked(data.pickaxeID)) return;
            SaveManager.CurrentData.EquippedPickaxeID = data.pickaxeID;
            SaveManager.Save();

            PickaxeDurabilityManager.Instance?.SetCurrentPickaxe(data);
            OnPickaxeStateChanged?.Invoke();

            string name = GetLocalizedName(data);
            Debug.Log($"[Pickaxe]\nEquipped\nPickaxe : {name}");
        }

        public int GetFinalMaxDurability(PickaxeData data)
        {
            if (data == null) return 50;
            int level = MetaProgressionManager.Instance?.PickaxeDurabilityLevel ?? 0;
            float multiplier = 1.0f + level * 0.1f;
            return Mathf.Max(1, Mathf.RoundToInt(data.baseMaxDurability * multiplier));
        }

        public int GetEquippedMiningPower() => EquippedPickaxeData?.miningPower ?? 1;

        private string GetLocalizedName(PickaxeData data)
        {
            if (data == null) return "Unknown";
            return LocalizationManager.Instance.GetTranslation(data.nameLocKey);
        }
    }
}
