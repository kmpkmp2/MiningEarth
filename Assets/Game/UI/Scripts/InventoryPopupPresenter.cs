using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Map;
using DeepEarth.Common;

namespace DeepEarth.UI
{
    public class InventoryPopupPresenter
    {
        private readonly InventoryPopupView _view;
        private readonly InventoryCollection _collection;
        private readonly List<GameObject> _activeSlots = new List<GameObject>();
        
        private InventorySlotData _selectedSlotData;
        private int _currentRefreshId = 0;

        public InventoryPopupPresenter(InventoryPopupView view, InventoryCollection collection)
        {
            _view = view;
            _collection = collection;

            // View bindings
            _view.OnConfirmOkClicked += HandleConfirmOk;
            _view.OnConfirmCancelClicked += HandleConfirmCancel;

            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null)
            {
                detailPanel.OnUseClicked += HandleUseItem;
                detailPanel.OnDropClicked += HandleDropClick;
                detailPanel.OnCloseClicked += HandleCloseDetailPanel;
            }

            _collection.OnInventoryChanged += RefreshGrid;
        }

        public void Dispose()
        {
            _view.OnConfirmOkClicked -= HandleConfirmOk;
            _view.OnConfirmCancelClicked -= HandleConfirmCancel;

            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null)
            {
                detailPanel.OnUseClicked -= HandleUseItem;
                detailPanel.OnDropClicked -= HandleDropClick;
                detailPanel.OnCloseClicked -= HandleCloseDetailPanel;
            }

            _collection.OnInventoryChanged -= RefreshGrid;
            ClearSlots();
        }

        public void InitializePopup()
        {
            Debug.Log("[Inventory]\nInventoryPopupView Open");
            _selectedSlotData = null;
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null)
            {
                detailPanel.SetVisible(false);
            }
            _view.HideConfirmation();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _currentRefreshId++;
            RefreshGridAsync(_currentRefreshId).Forget();
        }

        private async UniTaskVoid RefreshGridAsync(int refreshId)
        {
            // Create a shallow copy to prevent InvalidOperationException if the collection changes during await yields
            var slotsCopy = new List<InventorySlotData>(_collection.Slots);
            
            var parent = _view.GetGridContentParent();
            if (parent == null) return;

            ClearAllGridChildren(parent);
            ClearSlots();

            Debug.Log("[Inventory]\nRefresh Start");
            Debug.Log($"[Inventory]\nItem Count : {slotsCopy.Count}");

            var emptyTxt = _view.GetEmptyMessageText();
            if (slotsCopy.Count == 0)
            {
                if (emptyTxt != null)
                {
                    emptyTxt.text = "보유 중인 아이템이 없습니다.";
                    emptyTxt.gameObject.SetActive(true);
                }
            }
            else
            {
                if (emptyTxt != null)
                {
                    emptyTxt.gameObject.SetActive(false);
                }
            }

            GameObject prefab = _view.GetSlotPrefab();
            if (prefab == null)
            {
                prefab = await ResourceManager.Instance.LoadAssetAsync<GameObject>(AddressableKeys.UIInventorySlot);
            }

            for (int i = 0; i < slotsCopy.Count; i++)
            {
                if (refreshId != _currentRefreshId) return;

                var slotGo = UnityEngine.Object.Instantiate(prefab, parent);
                if (slotGo == null) continue;

                _activeSlots.Add(slotGo);

                var slotView = slotGo.GetComponent<InventorySlotView>();
                if (slotView == null)
                {
                    slotView = slotGo.AddComponent<InventorySlotView>();
                }

                var slotData = slotsCopy[i];
                string cleanName = slotData.Item.Id.Replace("Item_", "");
                Debug.Log($"[Inventory]\nCreate Slot : {cleanName}");

                string translatedName = LocalizationManager.Instance.GetTranslation(slotData.Item.NameKey) ?? slotData.Item.Id;

                // Load Icon Sprite from Addressables
                Sprite iconSprite = await LoadIconSpriteAsync(slotData.Item.IconKey, translatedName);

                // Re-verify stale check after asynchronous operation
                if (refreshId != _currentRefreshId)
                {
                    return;
                }

                // Setup Slot data
                slotView.SetData(slotData, iconSprite);

                // Setup Slot Clicked Callback
                slotView.OnClicked += () =>
                {
                    _selectedSlotData = slotData;
                    ShowDetailPanel(slotData, iconSprite);
                };
            }

            Debug.Log($"[Inventory]\nVisible Slot Count : {parent.childCount}");

            // Verify if selected item still exists in inventory
            if (_selectedSlotData != null)
            {
                var matchingSlot = _collection.Slots.Find(s => s.Item.Id == _selectedSlotData.Item.Id);
                if (matchingSlot != null)
                {
                    _selectedSlotData = matchingSlot;
                    string translatedName = LocalizationManager.Instance.GetTranslation(matchingSlot.Item.NameKey) ?? matchingSlot.Item.Id;
                    Sprite iconSprite = await LoadIconSpriteAsync(matchingSlot.Item.IconKey, translatedName);
                    
                    if (refreshId == _currentRefreshId)
                    {
                        ShowDetailPanel(matchingSlot, iconSprite);
                    }
                }
                else
                {
                    _selectedSlotData = null;
                    var detailPanel = _view.GetDetailPanel();
                    if (detailPanel != null)
                    {
                        detailPanel.SetVisible(false);
                    }
                }
            }
        }

        private async UniTask<Sprite> LoadIconSpriteAsync(string iconKey, string itemName)
        {
            Sprite iconSprite = null;
            if (!string.IsNullOrEmpty(iconKey))
            {
                try
                {
                    iconSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
                }
                catch (Exception)
                {
                    iconSprite = null;
                }
            }

            if (iconSprite == null)
            {
                Debug.Log($"[Inventory]\nMissing Icon\nUse Empty_item_Icon");
                try
                {
                    iconSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Empty_item_Icon");
                }
                catch (Exception)
                {
                    iconSprite = null;
                }
            }
            return iconSprite;
        }

        private void ShowDetailPanel(InventorySlotData slotData, Sprite iconSprite)
        {
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null)
            {
                detailPanel.SetItem(slotData, iconSprite);
            }
        }

        private void HandleCloseDetailPanel()
        {
            _selectedSlotData = null;
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null)
            {
                detailPanel.SetVisible(false);
            }
        }

        private void HandleUseItem()
        {
            if (_selectedSlotData == null) return;

            // Resource items → pickaxe repair
            if (_selectedSlotData.Item.Type == ItemType.Resource)
            {
                HandleRepairWithOre();
                return;
            }

            // Only consumable items can be used
            if (_selectedSlotData.Item.Type != ItemType.Consumable) return;

            string itemId = _selectedSlotData.Item.Id;

            // Apply healing/effect
            if (itemId == "Item_Potion")
            {
                StatManager.Instance.Heal(5);
                TriggerFloatingText($"+5 {LocalizationManager.Instance.GetTranslation("hud_hp_heal") ?? "HP Healed!"}", Color.green);
            }
            else if (itemId == "Item_Special")
            {
                StatManager.Instance.Heal(10);
                TriggerFloatingText($"+10 {LocalizationManager.Instance.GetTranslation("hud_hp_heal") ?? "HP Healed!"}", Color.green);
            }
            else if (itemId == "Item_Chest")
            {
                // Treasure Box: heals 2 HP + adds some random resource (e.g. 5 Iron / 2 Silver / 1 Gold)
                StatManager.Instance.Heal(2);
                
                float rnd = UnityEngine.Random.value;
                string rewardText = "";
                if (rnd < 0.5f)
                {
                    InventoryManager.Instance.AddItem("Item_Iron", 5);
                    rewardText = "+5 Iron";
                }
                else if (rnd < 0.85f)
                {
                    InventoryManager.Instance.AddItem("Item_Silver", 2);
                    rewardText = "+2 Silver";
                }
                else
                {
                    InventoryManager.Instance.AddItem("Item_Gold", 1);
                    rewardText = "+1 Gold";
                }
                TriggerFloatingText($"+2 HP, {rewardText}", Color.yellow);
            }
            else
            {
                // Generic Consumable fallback
                TriggerFloatingText($"Used {itemId}", Color.white);
            }

            // Decrement item
            _collection.RemoveItem(itemId, 1);
            
            // Invoke GameManager/HUD update
            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleRepairWithOre()
        {
            var manager = DeepEarth.Core.PickaxeDurabilityManager.Instance;
            if (manager == null) return;

            string itemId = _selectedSlotData.Item.Id;
            var recipe = manager.GetRepairRecipe(itemId);
            if (recipe == null) return;

            if (manager.CurrentDurability >= manager.MaxDurability)
            {
                TriggerFloatingText(
                    LocalizationManager.Instance.GetTranslation("pickaxe_repair_full"),
                    Color.yellow);
                return;
            }

            int available = InventoryManager.Instance.GetItemCount(itemId);
            if (available < recipe.itemCostPerUse)
            {
                TriggerFloatingText(
                    LocalizationManager.Instance.GetTranslation("pickaxe_repair_not_enough"),
                    Color.red);
                return;
            }

            int gain = UnityEngine.Mathf.Max(1,
                UnityEngine.Mathf.RoundToInt(recipe.durabilityGain * manager.CurrentPickaxeEfficiency));

            _collection.RemoveItem(itemId, recipe.itemCostPerUse);
            manager.Repair(gain);

            string oreName = LocalizationManager.Instance.GetTranslation(_selectedSlotData.Item.NameKey) ?? itemId;
            Debug.Log($"[Pickaxe]\nRepair\nOre : {oreName}\nConsumed : {recipe.itemCostPerUse}\nRecovered : {gain}\nCurrent : {manager.CurrentDurability} / {manager.MaxDurability}");

            TriggerFloatingText($"⛏ +{gain}", new Color(0.6f, 1f, 0.6f));
            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleDropClick()
        {
            if (_selectedSlotData == null) return;

            string translatedMsg = LocalizationManager.Instance.GetTranslation("inv_confirm_drop_title") ?? "Really Drop?";
            _view.ShowConfirmation(translatedMsg);
        }

        private void HandleConfirmOk()
        {
            if (_selectedSlotData == null)
            {
                _view.HideConfirmation();
                return;
            }

            string itemId = _selectedSlotData.Item.Id;
            _collection.RemoveItem(itemId, 1);
            
            _view.HideConfirmation();
            
            TriggerFloatingText($"Dropped {itemId}", Color.red);

            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleConfirmCancel()
        {
            _view.HideConfirmation();
        }

        private void ClearSlots()
        {
            foreach (var slot in _activeSlots)
            {
                if (slot != null)
                {
                    UnityEngine.Object.Destroy(slot);
                }
            }
            _activeSlots.Clear();
        }

        private void ClearAllGridChildren(Transform parent)
        {
            if (parent == null) return;
            
            int childCountBefore = parent.childCount;
            Debug.Log("[Inventory]\nClear All Slots");
            Debug.Log($"[Inventory]\nChild Count Before : {childCountBefore}");
            
            System.Collections.Generic.List<GameObject> children = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i).gameObject);
            }
            
            foreach (var child in children)
            {
                UnityEngine.Object.DestroyImmediate(child);
            }
            
            Debug.Log($"[Inventory]\nChild Count After : {parent.childCount}");
        }

        private void TriggerFloatingText(string message, Color color)
        {
            if (Camera.main != null && EffectSystem.Instance != null)
            {
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
                EffectSystem.Instance.SpawnDamageText(spawnPos, message, color);
            }
        }
    }
}
