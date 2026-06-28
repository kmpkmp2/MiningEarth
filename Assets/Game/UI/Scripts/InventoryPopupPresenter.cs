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

        private InventorySlotModel _selectedSlotModel;
        private int _currentRefreshId = 0;

        public InventoryPopupPresenter(InventoryPopupView view, InventoryCollection collection)
        {
            _view = view;
            _collection = collection;

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
            _selectedSlotModel = null;
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null) detailPanel.SetVisible(false);
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
            var slotsCopy = new List<InventorySlotModel>(_collection.Slots);

            var parent = _view.GetGridContentParent();
            if (parent == null) return;

            ClearAllGridChildren(parent);
            ClearSlots();

            Debug.Log("[Inventory]\nRefresh Start");
            Debug.Log($"[Inventory]\nSlot Count : {slotsCopy.Count}");

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
                if (emptyTxt != null) emptyTxt.gameObject.SetActive(false);
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
                if (slotView == null) slotView = slotGo.AddComponent<InventorySlotView>();

                var slotModel = slotsCopy[i];
                Debug.Log($"[Inventory]\nCreate Slot : {slotModel.ItemID.Replace("Item_", "")} ({slotModel.Count}/{slotModel.MaxStack})");

                Sprite iconSprite = await LoadIconSpriteAsync(slotModel.Item.IconKey);

                if (refreshId != _currentRefreshId) return;

                slotView.SetData(slotModel, iconSprite);

                slotView.OnClicked += () =>
                {
                    _selectedSlotModel = slotModel;
                    ShowDetailPanel(slotModel, iconSprite);
                };
            }

            Debug.Log($"[Inventory]\nVisible Slot Count : {parent.childCount}");

            // Re-validate selected slot after refresh
            if (_selectedSlotModel != null)
            {
                var matchingSlot = _collection.Slots.Find(s => s.ItemID == _selectedSlotModel.ItemID);
                if (matchingSlot != null)
                {
                    _selectedSlotModel = matchingSlot;
                    Sprite iconSprite = await LoadIconSpriteAsync(matchingSlot.Item.IconKey);
                    if (refreshId == _currentRefreshId) ShowDetailPanel(matchingSlot, iconSprite);
                }
                else
                {
                    _selectedSlotModel = null;
                    var detailPanel = _view.GetDetailPanel();
                    if (detailPanel != null) detailPanel.SetVisible(false);
                }
            }
        }

        private async UniTask<Sprite> LoadIconSpriteAsync(string iconKey)
        {
            Sprite iconSprite = null;
            if (!string.IsNullOrEmpty(iconKey))
            {
                try { iconSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey); }
                catch (Exception) { iconSprite = null; }
            }

            if (iconSprite == null)
            {
                Debug.Log($"[Inventory]\nMissing Icon\nUse Empty_item_Icon");
                try { iconSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>("Empty_item_Icon"); }
                catch (Exception) { iconSprite = null; }
            }
            return iconSprite;
        }

        private void ShowDetailPanel(InventorySlotModel slotModel, Sprite iconSprite)
        {
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null) detailPanel.SetItem(slotModel, iconSprite);
        }

        private void HandleCloseDetailPanel()
        {
            _selectedSlotModel = null;
            var detailPanel = _view.GetDetailPanel();
            if (detailPanel != null) detailPanel.SetVisible(false);
        }

        private void HandleUseItem()
        {
            if (_selectedSlotModel == null) return;

            if (_selectedSlotModel.Item.Type == ItemType.Resource)
            {
                HandleRepairWithOre();
                return;
            }

            if (_selectedSlotModel.Item.Type != ItemType.Consumable) return;

            string itemId = _selectedSlotModel.ItemID;

            if (itemId == "Item_Potion")
            {
                StatManager.Instance.Heal(5);
                TriggerFloatingText($"+5 {LocalizationManager.Instance.GetTranslation("hud_hp_heal") ?? "HP Healed!"}", Color.green);
            }
            else if (itemId == AddressableKeys.ItemBurnCure)
            {
                if (StatusEffectManager.Instance != null && StatusEffectManager.Instance.CureBurn())
                {
                    TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_burn_cure_used"), new Color(0.4f, 0.9f, 1f));
                }
                else
                {
                    TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_burn_cure_no_burn"), Color.yellow);
                    return;
                }
            }
            else if (itemId == "Item_Special")
            {
                StatManager.Instance.Heal(10);
                TriggerFloatingText($"+10 {LocalizationManager.Instance.GetTranslation("hud_hp_heal") ?? "HP Healed!"}", Color.green);
            }
            else if (itemId == "Item_Chest")
            {
                StatManager.Instance.Heal(2);
                float rnd = UnityEngine.Random.value;
                string rewardText;
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
                TriggerFloatingText($"Used {itemId}", Color.white);
            }

            _collection.RemoveItem(itemId, 1);
            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleRepairWithOre()
        {
            var manager = DeepEarth.Core.PickaxeDurabilityManager.Instance;
            if (manager == null) return;

            string itemId = _selectedSlotModel.ItemID;
            var recipe = manager.GetRepairRecipe(itemId);
            if (recipe == null) return;

            if (manager.CurrentDurability >= manager.MaxDurability)
            {
                TriggerFloatingText(LocalizationManager.Instance.GetTranslation("pickaxe_repair_full"), Color.yellow);
                return;
            }

            int available = InventoryManager.Instance.GetItemCount(itemId);
            if (available < recipe.itemCostPerUse)
            {
                TriggerFloatingText(LocalizationManager.Instance.GetTranslation("pickaxe_repair_not_enough"), Color.red);
                return;
            }

            int gain = UnityEngine.Mathf.Max(1,
                UnityEngine.Mathf.RoundToInt(recipe.durabilityGain * manager.CurrentPickaxeEfficiency));

            _collection.RemoveItem(itemId, recipe.itemCostPerUse);
            manager.Repair(gain);

            string oreName = LocalizationManager.Instance.GetTranslation(_selectedSlotModel.Item.NameKey) ?? itemId;
            Debug.Log($"[Pickaxe]\nRepair\nOre : {oreName}\nConsumed : {recipe.itemCostPerUse}\nRecovered : {gain}\nCurrent : {manager.CurrentDurability} / {manager.MaxDurability}");

            TriggerFloatingText($"⛏ +{gain}", new Color(0.6f, 1f, 0.6f));
            GameManager.Instance.TriggerStatsOrResourcesChanged();

            var oreType = ItemIdToBlockType(itemId);
            if (oreType.HasValue) DeepEarth.Common.GameEvents.FireRepairWithOre(oreType.Value);
        }

        private DeepEarth.Common.BlockType? ItemIdToBlockType(string itemId)
        {
            switch (itemId)
            {
                case "Item_Stone":   return DeepEarth.Common.BlockType.Stone;
                case "Item_Iron":    return DeepEarth.Common.BlockType.Iron;
                case "Item_Silver":  return DeepEarth.Common.BlockType.Silver;
                case "Item_Gold":    return DeepEarth.Common.BlockType.Gold;
                case "Item_Diamond": return DeepEarth.Common.BlockType.Diamond;
                default:             return null;
            }
        }

        private void HandleDropClick()
        {
            if (_selectedSlotModel == null) return;
            string translatedMsg = LocalizationManager.Instance.GetTranslation("inv_confirm_drop_title") ?? "Really Drop?";
            _view.ShowConfirmation(translatedMsg);
        }

        private void HandleConfirmOk()
        {
            if (_selectedSlotModel == null)
            {
                _view.HideConfirmation();
                return;
            }

            string itemId = _selectedSlotModel.ItemID;
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
                if (slot != null) UnityEngine.Object.Destroy(slot);
            }
            _activeSlots.Clear();
        }

        private void ClearAllGridChildren(Transform parent)
        {
            if (parent == null) return;
            Debug.Log("[Inventory]\nClear All Slots");
            Debug.Log($"[Inventory]\nChild Count Before : {parent.childCount}");

            var children = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
                children.Add(parent.GetChild(i).gameObject);
            foreach (var child in children)
                UnityEngine.Object.DestroyImmediate(child);

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
