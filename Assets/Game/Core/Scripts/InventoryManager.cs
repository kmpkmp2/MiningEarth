using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        private readonly Dictionary<string, ItemData> _itemTemplates = new Dictionary<string, ItemData>();
        private bool _initialized;

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

            var items = await ResourceManager.Instance.LoadAllByLabelAsync<ItemData>(AddressableKeys.LabelItemData);
            if (items != null)
            {
                foreach (var item in items) RegisterTemplate(item);
            }

            InitializeMetaInventoryFromSave();
            Debug.Log($"[Inventory]\nManager Initialized\nTotal Templates : {_itemTemplates.Count}");
        }

        private void RegisterTemplate(ItemData item)
        {
            _itemTemplates[item.itemID] = item;
        }

        public ItemData GetTemplate(string id)
        {
            if (_itemTemplates.TryGetValue(id, out var item)) return item;
            Debug.LogWarning($"InventoryManager: Item template not found: {id}");
            return null;
        }

        // Merchant 등 랜덤 상품 풀 구성을 위한 전체 템플릿 조회(타입 필터).
        public IEnumerable<ItemData> GetTemplatesByType(ItemType type)
        {
            foreach (var item in _itemTemplates.Values)
                if (item.type == type)
                    yield return item;
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
            if (data.PersistentDirt    > 0) MetaCollection.AddItem(GetTemplate("Item_Dirt"),    data.PersistentDirt);
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
            int runDirt    = RunCollection.GetItemCount("Item_Dirt");

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
            if (runDirt > 0)
            {
                data.PersistentDirt += runDirt;
                MetaCollection.AddItem(GetTemplate("Item_Dirt"), runDirt);
                Debug.Log($"[Run]\nDirt +{runDirt}");
            }

            SaveManager.Save();
        }

        public InventoryCollection GetRunInventory()  => RunCollection;
        public InventoryCollection GetMetaInventory() => MetaCollection;
    }
}
