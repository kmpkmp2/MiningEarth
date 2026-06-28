using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.UI
{
    [System.Serializable]
    public class InventoryCollection
    {
        public List<InventorySlotModel> Slots { get; } = new List<InventorySlotModel>();

        public event Action OnInventoryChanged;

        public int GetTotalItemCount()
        {
            int total = 0;
            foreach (var slot in Slots) total += slot.Count;
            return total;
        }

        public int GetItemCount(string itemId)
        {
            int total = 0;
            foreach (var slot in Slots)
                if (slot.ItemID == itemId) total += slot.Count;
            return total;
        }

        // maxSlotCapacity: total stack slot limit. Pass int.MaxValue for unlimited (MetaCollection).
        public bool AddItem(InventoryItemData item, int quantity, int maxSlotCapacity = int.MaxValue)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;
            string cleanName = item.Id.Replace("Item_", "");

            // Fill existing partial stacks first (no new slot consumed)
            foreach (var slot in Slots)
            {
                if (slot.ItemID != item.Id || slot.IsFull) continue;
                int toAdd = Mathf.Min(remaining, slot.Available);
                slot.Count += toAdd;
                remaining -= toAdd;
                Debug.Log($"[Inventory]\n{cleanName}\nStack {slot.SlotIndex + 1}\n{slot.Count} / {slot.MaxStack}");
                if (remaining <= 0) break;
            }

            // Create new stacks for any remainder
            while (remaining > 0)
            {
                if (Slots.Count >= maxSlotCapacity) break;
                int stackSize = Mathf.Min(remaining, item.MaxStack);
                var newSlot = new InventorySlotModel
                {
                    ItemID = item.Id,
                    Item = item,
                    Count = stackSize,
                    MaxStack = item.MaxStack,
                    SlotIndex = Slots.Count
                };
                Slots.Add(newSlot);
                Debug.Log($"[Inventory]\n{cleanName}\nStack {newSlot.SlotIndex + 1}\n{newSlot.Count} / {newSlot.MaxStack}");
                remaining -= stackSize;
            }

            OnInventoryChanged?.Invoke();
            return remaining == 0;
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0) return false;
            if (GetItemCount(itemId) < quantity) return false;

            int remaining = quantity;

            // Remove from last stacks first (LIFO)
            for (int i = Slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (Slots[i].ItemID != itemId) continue;
                int toRemove = Mathf.Min(remaining, Slots[i].Count);
                Slots[i].Count -= toRemove;
                remaining -= toRemove;
                if (Slots[i].Count <= 0)
                    Slots.RemoveAt(i);
            }

            // Re-index after removal
            for (int i = 0; i < Slots.Count; i++)
                Slots[i].SlotIndex = i;

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveFromSlot(InventorySlotModel slot, int count)
        {
            if (slot == null || count <= 0) return false;
            int idx = Slots.IndexOf(slot);
            if (idx < 0) return false;

            int toRemove = Mathf.Min(count, slot.Count);
            slot.Count -= toRemove;
            if (slot.Count <= 0)
                Slots.RemoveAt(idx);

            for (int i = 0; i < Slots.Count; i++)
                Slots[i].SlotIndex = i;

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveSlot(InventorySlotModel slot)
        {
            if (slot == null) return false;
            bool removed = Slots.Remove(slot);
            if (removed)
            {
                for (int i = 0; i < Slots.Count; i++)
                    Slots[i].SlotIndex = i;
                OnInventoryChanged?.Invoke();
            }
            return removed;
        }

        public void Clear()
        {
            Slots.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}
