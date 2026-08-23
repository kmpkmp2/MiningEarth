using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DeepEarth.Core;
using DeepEarth.Map;
using DeepEarth.Common;
using DeepEarth.Combat;

namespace DeepEarth.UI
{
    public class InventoryPopupPresenter
    {
        // 정렬 우선순위: 소비재 → 광물 → 그 외. BlockType(Constants.cs) 기반 7종 광물의 itemID 고정 목록 —
        // 새 광물(BlockType enum 값)이 추가되면 여기도 함께 갱신해야 한다.
        private static readonly HashSet<string> MineralItemIDs = new HashSet<string>
        {
            "Item_Dirt", "Item_Stone", "Item_Wood", "Item_Iron", "Item_Silver", "Item_Gold", "Item_Diamond"
        };

        private readonly InventoryPopupView _view;
        private readonly InventoryCollection _collection;
        private readonly List<GameObject> _activeSlots = new List<GameObject>();

        private InventorySlotModel _selectedSlotModel;
        private int _currentRefreshId = 0;
        private bool _pendingDiscardAll;

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
                detailPanel.OnDiscardAllClicked += HandleDiscardAllClick;
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
                detailPanel.OnDiscardAllClicked -= HandleDiscardAllClick;
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
            // 1순위: 소비재, 2순위: 광물, 3순위: 그 외. 같은 우선순위 내에서는 기존 순서를 유지한다(안정 정렬).
            var slotsCopy = _collection.Slots.OrderBy(GetSortPriority).ToList();

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
                    emptyTxt.text = LocalizationManager.Instance.GetTranslation("inv_empty_message");
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

                Sprite iconSprite = await LoadIconSpriteAsync(slotModel.Item.iconKey);

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
                    Sprite iconSprite = await LoadIconSpriteAsync(matchingSlot.Item.iconKey);
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

        private static int GetSortPriority(InventorySlotModel slot)
        {
            if (slot.Item != null && slot.Item.type == ItemType.Consumable) return 0;
            if (MineralItemIDs.Contains(slot.ItemID)) return 1;
            return 2;
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
            if (detailPanel != null) detailPanel.SetItem(slotModel, iconSprite, GetUseEnabled(slotModel));
        }

        private bool GetUseEnabled(InventorySlotModel slotModel)
        {
            if (slotModel.Item.repairAmount > 0)
            {
                var mgr = PickaxeDurabilityManager.Instance;
                return mgr != null && mgr.CurrentDurability < mgr.MaxDurability;
            }
            return true;
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

            var item = _selectedSlotModel.Item;
            string itemId = _selectedSlotModel.ItemID;

            if (item.type == ItemType.Resource)
            {
                if (itemId == "Item_Wood")
                {
                    StatManager.Instance.Heal(1);
                    TriggerFloatingText($"+1 {LocalizationManager.Instance.GetTranslation("hud_hp_heal")}", Color.green);
                    _collection.RemoveItem("Item_Wood", 1);
                    GameManager.Instance.TriggerStatsOrResourcesChanged();
                    return;
                }

                HandleRepairWithOre();
                return;
            }

            if (item.type != ItemType.Consumable) return;
            if (item.autoUseOnDeath) return; // 불사 물약: 수동 사용 불가, 사망 시 자동 소비만

            if (item.repairAmount > 0)
            {
                HandleRepairItem(item);
                return;
            }

            if (item.combatDamage > 0)
            {
                HandleCombatItem(item, itemId);
                return;
            }

            if (itemId == "Item_Chest")
            {
                HandleChestReward();
            }
            else
            {
                // 화상치료/해독/회복 효과는 한 아이템에 동시에 여러 개 설정될 수 있어(예: 정화의 성수 = 화상+중독 치료)
                // 상호 배타적인 else-if 대신 각 효과를 독립적으로 검사한다.
                bool effectApplied = false;
                bool blockConsume  = false;

                if (item.cureBurn)
                {
                    if (StatusEffectManager.Instance != null && StatusEffectManager.Instance.CureBurn())
                    {
                        TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_burn_cure_used"), new Color(0.4f, 0.9f, 1f));
                        effectApplied = true;
                    }
                    else if (!item.curePoison && item.healAmount <= 0)
                    {
                        TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_burn_cure_no_burn"), Color.yellow);
                        blockConsume = true;
                    }
                }

                if (item.curePoison)
                {
                    if (StatusEffectManager.Instance != null && StatusEffectManager.Instance.CurePoison())
                    {
                        TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_poison_cure_used"), new Color(0.6f, 0.9f, 0.4f));
                        effectApplied = true;
                    }
                    else if (!item.cureBurn && item.healAmount <= 0)
                    {
                        TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_poison_cure_no_poison"), Color.yellow);
                        blockConsume = true;
                    }
                }

                if (item.healAmount > 0)
                {
                    // 신규 패시브: Alchemist — 포션 회복량 증가 + 시작 유물(작은 약병) 추가 증가
                    var charID = CharacterManager.Instance.SelectedCharacterID;
                    float healBonus = CharacterManager.Instance.GetPassivePotionHealBonus(charID)
                                     + (StartingRelicManager.Instance != null ? StartingRelicManager.Instance.GetPotionHealBonus() : 0f);
                    int finalHeal = Mathf.RoundToInt(item.healAmount * (1f + healBonus));

                    // 그룹 C: 저체력일수록 회복량 배율 증가(회복 전 HP 비율 기준)
                    float lowHpMult = RelicManager.Instance?.GetLowHpHealMultiplier() ?? 1f;
                    finalHeal = Mathf.RoundToInt(finalHeal * lowHpMult);

                    StatManager.Instance.Heal(finalHeal);
                    TriggerFloatingText($"+{finalHeal} {LocalizationManager.Instance.GetTranslation("hud_hp_heal")}", Color.green);
                    effectApplied = true;

                    // 그룹 C: 포션(회복 아이템) 사용 트리거
                    RelicManager.Instance?.ApplyPotionUseEffects();
                }

                if (blockConsume && !effectApplied) return; // 치료할 상태이상이 없어 아무 효과도 없었다 — 소모하지 않음

                if (!effectApplied && !blockConsume)
                {
                    string usedName = LocalizationManager.Instance.GetTranslation(item.nameLocKey);
                    TriggerFloatingText(LocalizationManager.Instance.GetFormatted("item_used_generic_fmt", usedName), Color.white);
                }
            }

            // 그룹 C: 소비 아이템 사용 트리거(고대열쇠 등 — 여기 도달했다면 실제로 소비된 아이템 사용이다)
            RelicManager.Instance?.ApplyItemUseEffects();

            _collection.RemoveItem(itemId, 1);
            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleCombatItem(ItemData item, string itemId)
        {
            if (CombatSystem.Instance == null || !CombatSystem.Instance.HasActiveMonsters)
            {
                TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_combat_only"), Color.yellow);
                return;
            }

            CombatSystem.Instance.ApplyItemDamageToActiveMonsters(item.combatDamage);
            TriggerFloatingText($"-{item.combatDamage} HP", Color.red);
            _collection.RemoveItem(itemId, 1);
            GameManager.Instance.TriggerStatsOrResourcesChanged();
        }

        private void HandleChestReward()
        {
            const int chestHealAmount = 2;
            StatManager.Instance.Heal(chestHealAmount);
            float anvilChance = GameBalanceData.Instance.portableAnvilChestChance;
            float rnd = UnityEngine.Random.value;
            string rewardText;
            if (rnd < 0.5f)
            {
                InventoryManager.Instance.AddItem("Item_Iron", 5);
                rewardText = LocalizationManager.Instance.GetFormatted("chest_reward_iron_fmt", 5);
            }
            else if (rnd < 0.85f)
            {
                InventoryManager.Instance.AddItem("Item_Silver", 2);
                rewardText = LocalizationManager.Instance.GetFormatted("chest_reward_silver_fmt", 2);
            }
            else if (rnd < 0.85f + anvilChance)
            {
                InventoryManager.Instance.AddItem(AddressableKeys.ItemPortableAnvil, 1);
                rewardText = LocalizationManager.Instance.GetFormatted("chest_reward_anvil_fmt", 1);
            }
            else
            {
                InventoryManager.Instance.AddItem("Item_Gold", 1);
                rewardText = LocalizationManager.Instance.GetFormatted("chest_reward_gold_fmt", 1);
            }
            TriggerFloatingText(LocalizationManager.Instance.GetFormatted("chest_reward_result_fmt", chestHealAmount, rewardText), Color.yellow);
        }

        private void HandleRepairItem(ItemData item)
        {
            var mgr = PickaxeDurabilityManager.Instance;
            if (mgr == null) return;

            if (mgr.CurrentDurability >= mgr.MaxDurability)
            {
                TriggerFloatingText(LocalizationManager.Instance.GetTranslation("item_anvil_full"), Color.yellow);
                return;
            }

            mgr.Repair(item.repairAmount); // Repair()가 MaxDurability 클램프 처리
            _collection.RemoveItem(item.itemID, 1);
            GameManager.Instance.TriggerStatsOrResourcesChanged();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Item]\nRepair Item Used\nDurability +{item.repairAmount}");
#endif
            TriggerFloatingText($"⛏ +{item.repairAmount}", new Color(0.6f, 1f, 0.6f));
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

            string oreName = LocalizationManager.Instance.GetTranslation(_selectedSlotModel.Item.nameLocKey);
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
            _pendingDiscardAll = false;
            string msg = LocalizationManager.Instance.GetTranslation("inv_confirm_drop_title");
            _view.ShowConfirmation(msg);
        }

        private void HandleDiscardAllClick()
        {
            if (_selectedSlotModel == null) return;
            _pendingDiscardAll = true;
            string msg = LocalizationManager.Instance.GetTranslation("inv_confirm_discard_all_title");
            _view.ShowConfirmation(msg);
        }

        private void HandleConfirmOk()
        {
            if (_selectedSlotModel == null)
            {
                _pendingDiscardAll = false;
                _view.HideConfirmation();
                return;
            }

            if (_pendingDiscardAll)
            {
                string cleanName = LocalizationManager.Instance.GetTranslation(_selectedSlotModel.Item.nameLocKey);
                int count = _selectedSlotModel.Count;

                _collection.RemoveSlot(_selectedSlotModel);
                _view.HideConfirmation();

                Debug.Log($"[Inventory]\nDiscard Stack\n{cleanName}\nCount : {count}");

                _selectedSlotModel = null;
                _pendingDiscardAll = false;
                var detailPanel = _view.GetDetailPanel();
                if (detailPanel != null) detailPanel.SetVisible(false);

                GameManager.Instance?.TriggerStatsOrResourcesChanged();
            }
            else
            {
                string cleanName = LocalizationManager.Instance.GetTranslation(_selectedSlotModel.Item.nameLocKey);

                _collection.RemoveFromSlot(_selectedSlotModel, 1);
                _view.HideConfirmation();

                int remaining = _selectedSlotModel.Count;
                Debug.Log($"[Inventory]\nDiscard\n{cleanName}\nRemaining : {remaining}");

                TriggerFloatingText(LocalizationManager.Instance.GetFormatted("inv_dropped_fmt", cleanName), Color.red);
                GameManager.Instance?.TriggerStatsOrResourcesChanged();
            }
        }

        private void HandleConfirmCancel()
        {
            _pendingDiscardAll = false;
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
