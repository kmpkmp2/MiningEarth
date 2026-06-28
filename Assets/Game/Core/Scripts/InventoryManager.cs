using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.UI;

namespace DeepEarth.Core
{
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager _instance;
        public static InventoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("InventoryManager");
                    _instance = go.AddComponent<InventoryManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public InventoryCollection RunCollection { get; private set; } = new InventoryCollection();
        public InventoryCollection MetaCollection { get; private set; } = new InventoryCollection();
        public InventoryCollection Collection => RunCollection;
        public int InventoryCapacity => StatManager.Instance != null ? StatManager.Instance.GetInventorySize() : 24;

        private void Start()
        {
            if (RunCollection != null)
            {
                RunCollection.OnInventoryChanged += LogInventoryRefresh;
            }
        }

        private void LogInventoryRefresh()
        {
            int usedSlots = Collection.Slots.Count;
            Debug.Log($"[Inventory]\nUsed Slots (Stacks) : {usedSlots} / {InventoryCapacity}");
        }

        private readonly Dictionary<string, InventoryItemData> _itemTemplates = new Dictionary<string, InventoryItemData>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeItemTemplates();
                InitializeMetaInventoryFromSave();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeItemTemplates()
        {
            // Resources — MaxStack = 9
            RegisterTemplate(new InventoryItemData("Item_Stone",   "item_stone_name",   "item_stone_desc",   ItemType.Resource,   ItemRarity.Common,    "Item_Stone",   9));
            RegisterTemplate(new InventoryItemData("Item_Wood",    "item_wood_name",    "item_wood_desc",    ItemType.Resource,   ItemRarity.Common,    "Item_Wood",    9));
            RegisterTemplate(new InventoryItemData("Item_Iron",    "item_iron_name",    "item_iron_desc",    ItemType.Resource,   ItemRarity.Common,    "Item_Iron",    9));
            RegisterTemplate(new InventoryItemData("Item_Silver",  "item_silver_name",  "item_silver_desc",  ItemType.Resource,   ItemRarity.Rare,      "Item_Silver",  9));
            RegisterTemplate(new InventoryItemData("Item_Gold",    "item_gold_name",    "item_gold_desc",    ItemType.Resource,   ItemRarity.Epic,      "Item_Gold",    9));
            RegisterTemplate(new InventoryItemData("Item_Diamond", "item_diamond_name", "item_diamond_desc", ItemType.Resource,   ItemRarity.Legendary, "Item_Diamond", 9));

            // Consumables — MaxStack = 1
            RegisterTemplate(new InventoryItemData("Item_Potion",              "item_potion_name",    "item_potion_desc",    ItemType.Consumable, ItemRarity.Common,    "Item_Potion",             1));
            RegisterTemplate(new InventoryItemData(AddressableKeys.ItemBurnCure, "item_burn_cure_name", "item_burn_cure_desc", ItemType.Consumable, ItemRarity.Common,    AddressableKeys.ItemBurnCure, 1));
            RegisterTemplate(new InventoryItemData("Item_Key",                 "item_key_name",       "item_key_desc",       ItemType.Consumable, ItemRarity.Rare,      "Item_Key",                1));
            RegisterTemplate(new InventoryItemData("Item_Chest",               "item_chest_name",     "item_chest_desc",     ItemType.Consumable, ItemRarity.Epic,      "Item_Chest",              1));
            RegisterTemplate(new InventoryItemData("Item_Special",             "item_special_name",   "item_special_desc",   ItemType.Consumable, ItemRarity.Legendary, "Item_Special",            1));
        }

        private void RegisterTemplate(InventoryItemData item)
        {
            _itemTemplates[item.Id] = item;
        }

        public InventoryItemData GetTemplate(string id)
        {
            if (_itemTemplates.TryGetValue(id, out var item)) return item;
            Debug.LogWarning($"InventoryManager: Item template not found: {id}");
            return null;
        }

        public bool AddItem(string itemId, int quantity)
        {
            var template = GetTemplate(itemId);
            if (template == null) return false;

            bool success = Collection.AddItem(template, quantity, InventoryCapacity);
            if (!success)
            {
                if (Camera.main != null && EffectSystem.Instance != null)
                {
                    EffectSystem.Instance.SpawnDamageText(
                        Camera.main.transform.position + Camera.main.transform.forward * 1.5f,
                        LocalizationManager.Instance.GetTranslation("hud_inv_full"),
                        Color.red);
                }
            }

            if (GameManager.Instance != null)
                GameManager.Instance.TriggerStatsOrResourcesChanged();

            return success;
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            bool success = Collection.RemoveItem(itemId, quantity);
            if (success)
            {
                string cleanName = itemId.Replace("Item_", "");
                Debug.Log($"[Inventory]\nRemove Item : {cleanName} x{quantity}");
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerStatsOrResourcesChanged();
            }
            return success;
        }

        public int GetItemCount(string itemId)
        {
            return Collection.GetItemCount(itemId);
        }

        public void Clear()
        {
            Collection.Clear();
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        public void InitializeMetaInventoryFromSave()
        {
            MetaCollection.Clear();
            var data = SaveManager.CurrentData;
            if (data.PersistentStone   > 0) MetaCollection.AddItem(GetTemplate("Item_Stone"),   data.PersistentStone);
            if (data.PersistentWood    > 0) MetaCollection.AddItem(GetTemplate("Item_Wood"),    data.PersistentWood);
            if (data.PersistentIron    > 0) MetaCollection.AddItem(GetTemplate("Item_Iron"),    data.PersistentIron);
            if (data.PersistentSilver  > 0) MetaCollection.AddItem(GetTemplate("Item_Silver"),  data.PersistentSilver);
            if (data.PersistentGold    > 0) MetaCollection.AddItem(GetTemplate("Item_Gold"),    data.PersistentGold);
            if (data.PersistentDiamond > 0) MetaCollection.AddItem(GetTemplate("Item_Diamond"), data.PersistentDiamond);
        }

        public void ClearRunInventory()
        {
            RunCollection.Clear();
            Debug.Log("[Run]\nRun Inventory Cleared");
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        public void TransferRunRewardToMeta()
        {
            Debug.Log("[Run]\nTransfer Currency To Meta");
            var data = SaveManager.CurrentData;

            int runStone   = RunCollection.GetItemCount("Item_Stone");
            int runWood    = RunCollection.GetItemCount("Item_Wood");
            int runIron    = RunCollection.GetItemCount("Item_Iron");
            int runSilver  = RunCollection.GetItemCount("Item_Silver");
            int runGold    = RunCollection.GetItemCount("Item_Gold");
            int runDiamond = RunCollection.GetItemCount("Item_Diamond");

            if (runStone > 0)
            {
                data.PersistentStone += runStone;
                MetaCollection.AddItem(GetTemplate("Item_Stone"), runStone);
                Debug.Log($"[Run]\nStone +{runStone}");
            }
            if (runWood > 0)
            {
                data.PersistentWood += runWood;
                MetaCollection.AddItem(GetTemplate("Item_Wood"), runWood);
                Debug.Log($"[Run]\nWood +{runWood}");
            }
            if (runIron > 0)
            {
                data.PersistentIron += runIron;
                MetaCollection.AddItem(GetTemplate("Item_Iron"), runIron);
                Debug.Log($"[Run]\nIron +{runIron}");
            }
            if (runSilver > 0)
            {
                data.PersistentSilver += runSilver;
                MetaCollection.AddItem(GetTemplate("Item_Silver"), runSilver);
                Debug.Log($"[Run]\nSilver +{runSilver}");
            }
            if (runGold > 0)
            {
                data.PersistentGold += runGold;
                MetaCollection.AddItem(GetTemplate("Item_Gold"), runGold);
                Debug.Log($"[Run]\nGold +{runGold}");
            }
            if (runDiamond > 0)
            {
                data.PersistentDiamond += runDiamond;
                MetaCollection.AddItem(GetTemplate("Item_Diamond"), runDiamond);
                Debug.Log($"[Run]\nDiamond +{runDiamond}");
            }

            SaveManager.Save();
        }

        public InventoryCollection GetRunInventory()  => RunCollection;
        public InventoryCollection GetMetaInventory() => MetaCollection;
    }
}
