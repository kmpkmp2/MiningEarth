using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    // Plain C# class — ShopItemSlot 프리팹을 Instantiate해서 Content에 배치
    public class PickaxeShopView
    {
        private readonly Transform              _contentParent;
        private readonly GameObject             _slotPrefab;
        private readonly List<ShopItemSlotView> _slots = new();

        public event Action<ShopItemDisplayData> OnItemSelected;
        public event Action<ShopItemDisplayData> OnItemAction;

        public PickaxeShopView(Transform contentParent, GameObject slotPrefab)
        {
            _contentParent = contentParent;
            _slotPrefab    = slotPrefab;
        }

        public void Refresh(IReadOnlyList<PickaxeData> pickaxes, string equippedID, SaveData saveData)
        {
            if (_slotPrefab == null)
            {
                Debug.LogError("[PickaxeShopView] slotPrefab is null — ShopItemSlot Addressable 로드 실패");
                return;
            }

            // 파괴된 슬롯 정리
            _slots.RemoveAll(s => s == null);

            // 초과 슬롯 제거
            while (_slots.Count > pickaxes.Count)
            {
                var last = _slots[_slots.Count - 1];
                _slots.RemoveAt(_slots.Count - 1);
                if (last != null) UnityEngine.Object.Destroy(last.gameObject);
            }

            // 부족한 슬롯 생성
            while (_slots.Count < pickaxes.Count)
            {
                var go   = UnityEngine.Object.Instantiate(_slotPrefab, _contentParent);
                var slot = go.GetComponent<ShopItemSlotView>();
                slot.OnSelected      += HandleSlotSelected;
                slot.OnActionClicked += HandleSlotAction;
                _slots.Add(slot);
            }

            // 각 슬롯에 데이터 설정
            var loc = LocalizationManager.Instance;
            for (int i = 0; i < pickaxes.Count; i++)
            {
                var data       = pickaxes[i];
                bool isUnlocked = (saveData.UnlockedPickaxeIDs?.Contains(data.pickaxeID) ?? false) || data.isDefault;
                bool isEquipped = data.pickaxeID == equippedID;
                bool canAfford  = PickaxeManager.Instance?.CanAfford(data) ?? false;

                _slots[i].SetData(new ShopItemDisplayData
                {
                    name        = loc?.GetTranslation(data.nameLocKey) ?? data.nameLocKey,
                    description = loc?.GetTranslation(data.descLocKey) ?? data.descLocKey,
                    iconKey     = data.iconKey,
                    stat1Text   = loc?.GetFormatted("shop_pickaxe_mining_power", data.miningPower)
                                  ?? $"⛏ {data.miningPower}",
                    stat2Text   = loc?.GetFormatted("shop_pickaxe_durability", data.baseMaxDurability)
                                  ?? $"♦ {data.baseMaxDurability}",
                    costText    = BuildCostString(data, loc),
                    isUnlocked  = isUnlocked,
                    isEquipped  = isEquipped,
                    canAfford   = canAfford,
                    tag         = data,
                });
            }
        }

        public void ClearSlotRefs() => _slots.Clear();

        private void HandleSlotSelected(ShopItemSlotView slot)
        {
            foreach (var s in _slots) s.SetSelected(false);
            slot.SetSelected(true);
            OnItemSelected?.Invoke(slot.DisplayData);
        }

        private void HandleSlotAction(ShopItemSlotView slot)
        {
            OnItemAction?.Invoke(slot.DisplayData);
        }

        private static string BuildCostString(PickaxeData data, LocalizationManager loc)
        {
            if (data.unlockCost == null || data.unlockCost.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var cost in data.unlockCost)
            {
                if (sb.Length > 0) sb.Append("  ");
                string resName = loc?.GetTranslation($"item_{cost.resourceType.ToString().ToLower()}_name")
                                 ?? cost.resourceType.ToString();
                sb.Append($"{resName} x{cost.amount}");
            }
            return sb.ToString();
        }
    }
}
