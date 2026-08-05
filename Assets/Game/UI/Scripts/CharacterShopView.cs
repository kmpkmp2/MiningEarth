using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DeepEarth.Common;
using DeepEarth.Core;

namespace DeepEarth.UI
{
    public class CharacterShopView
    {
        private readonly Transform _contentParent;
        private readonly GameObject _slotPrefab;
        private readonly List<ShopItemSlotView> _slots = new();
        private readonly Dictionary<CharacterID, ShopItemDisplayData> _displayDataById = new();

        public event Action<ShopItemDisplayData> OnItemSelected;
        public event Action<ShopItemDisplayData> OnItemAction;

        public CharacterShopView(Transform contentParent, GameObject slotPrefab)
        {
            _contentParent = contentParent;
            _slotPrefab = slotPrefab;
        }

        public void Refresh(IReadOnlyList<CharacterData> characters)
        {
            if (_slotPrefab == null)
            {
                Debug.LogError("[CharacterShopView] slotPrefab is null - ShopItemSlot Addressable load failed");
                return;
            }

            _displayDataById.Clear();
            _slots.RemoveAll(s => s == null);

            while (_slots.Count > characters.Count)
            {
                var last = _slots[_slots.Count - 1];
                _slots.RemoveAt(_slots.Count - 1);
                if (last != null) UnityEngine.Object.Destroy(last.gameObject);
            }

            while (_slots.Count < characters.Count)
            {
                var go = UnityEngine.Object.Instantiate(_slotPrefab, _contentParent);
                var slot = go.GetComponent<ShopItemSlotView>();
                slot.OnSelected      += HandleSlotSelected;
                slot.OnActionClicked += HandleSlotAction;
                _slots.Add(slot);
            }

            var loc = LocalizationManager.Instance;
            var manager = CharacterManager.Instance;

            for (int i = 0; i < characters.Count; i++)
            {
                var data = characters[i];
                bool isUnlocked = manager.IsUnlocked(data.ID);
                bool canUnlock = manager.CanUnlock(data.ID);

                var displayData = new ShopItemDisplayData
                {
                    name             = loc.GetTranslation(data.NameKey),
                    description      = loc.GetTranslation(data.DescKey),
                    iconKey          = "",
                    stat1Text        = BuildPassiveText(data, loc),
                    stat2Text        = BuildOwnedResourcesText(loc),
                    costText         = BuildCostString(data, loc),
                    lockedActionText = loc.GetTranslation("char_unlock"),
                    isUnlocked       = isUnlocked,
                    canAfford        = canUnlock,
                    tag              = data,
                };

                _displayDataById[data.ID] = displayData;
                _slots[i].SetData(displayData);
            }
        }

        public bool TryGetDisplayData(CharacterID id, out ShopItemDisplayData data)
        {
            return _displayDataById.TryGetValue(id, out data);
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

        private static string BuildPassiveText(CharacterData data, LocalizationManager loc)
        {
            if (loc == null) return "";

            return string.IsNullOrEmpty(data.PassiveDescKey)
                ? loc.GetTranslation(data.DescKey)
                : loc.GetTranslation(data.PassiveDescKey);
        }

        private static string BuildCostString(CharacterData data, LocalizationManager loc)
        {
            int willCost = CharacterManager.Instance?.GetUnlockWillCost(data.ID) ?? 0;
            if (willCost <= 0) return "";

            string costValue = loc.GetFormatted("go_will_cost", willCost);
            return $"{loc.GetTranslation("char_cost_label")} {costValue}";
        }

        private static string BuildOwnedResourcesText(LocalizationManager loc)
        {
            int ownedWill = MetaProgressionManager.Instance?.Will ?? 0;
            if (loc == null) return $"Will {ownedWill}";

            return $"{loc.GetTranslation("char_owned_resources")} {loc.GetFormatted("menu_will", ownedWill)}";
        }
    }
}
