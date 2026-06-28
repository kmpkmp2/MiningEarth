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

        public void Refresh(IReadOnlyList<CharacterStaticData> characters)
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
                    name             = loc?.GetTranslation(data.NameKey) ?? data.NameKey,
                    description      = loc?.GetTranslation(data.DescKey) ?? data.DescKey,
                    iconKey          = "",
                    stat1Text        = BuildPassiveText(data, loc),
                    stat2Text        = BuildOwnedResourcesText(loc),
                    costText         = BuildCostString(data, loc),
                    lockedActionText = loc?.GetTranslation("char_unlock") ?? "Unlock",
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

        private static string BuildPassiveText(CharacterStaticData data, LocalizationManager loc)
        {
            if (loc == null) return "";

            return data.ID switch
            {
                CharacterID.Mercenary   => loc.GetTranslation("effect_passive_mercenary_desc"),
                CharacterID.Miner       => loc.GetTranslation("effect_passive_miner_desc"),
                CharacterID.GraveRobber => loc.GetTranslation("effect_passive_graverobber_desc"),
                _                       => loc.GetTranslation(data.DescKey)
            };
        }

        private static string BuildCostString(CharacterStaticData data, LocalizationManager loc)
        {
            if (data.UnlockCost == null || data.UnlockCost.Count == 0) return "";

            var sb = new StringBuilder();
            if (loc != null)
            {
                sb.Append(loc.GetTranslation("char_cost_label"));
                sb.Append(' ');
            }

            foreach (var cost in data.UnlockCost)
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append("  ");
                string resourceName = loc?.GetTranslation(GetItemNameKey(cost.Key)) ?? cost.Key.ToString();
                sb.Append(resourceName);
                sb.Append(" x");
                sb.Append(cost.Value);
            }

            return sb.ToString();
        }

        private static string BuildOwnedResourcesText(LocalizationManager loc)
        {
            var save = SaveManager.CurrentData;
            if (loc == null)
            {
                return $"Iron {save.PersistentIron} | Silver {save.PersistentSilver} | Gold {save.PersistentGold} | Diamond {save.PersistentDiamond}";
            }

            return $"{loc.GetTranslation("char_owned_resources")} " +
                   $"{loc.GetFormatted("hud_iron", save.PersistentIron)} | " +
                   $"{loc.GetFormatted("hud_silver", save.PersistentSilver)} | " +
                   $"{loc.GetFormatted("hud_gold", save.PersistentGold)} | " +
                   $"{loc.GetFormatted("hud_diamond", save.PersistentDiamond)}";
        }

        private static string GetItemNameKey(BlockType type)
        {
            return type switch
            {
                BlockType.Iron    => "item_iron_name",
                BlockType.Silver  => "item_silver_name",
                BlockType.Gold    => "item_gold_name",
                BlockType.Diamond => "item_diamond_name",
                _                 => type.ToString()
            };
        }
    }
}
