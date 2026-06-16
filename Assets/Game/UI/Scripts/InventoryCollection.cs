using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepEarth.UI
{
    [System.Serializable]
    public class InventorySlotData
    {
        public InventoryItemData Item;
        public int Quantity;

        public InventorySlotData(InventoryItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    [System.Serializable]
    public class InventoryCollection
    {
        public List<InventorySlotData> Slots { get; } = new List<InventorySlotData>();
        
        public event Action OnInventoryChanged;

        public int GetTotalItemCount()
        {
            return Slots.Sum(s => s.Quantity);
        }

        public int GetItemCount(string itemId)
        {
            var slot = Slots.FirstOrDefault(s => s.Item.Id == itemId);
            return slot?.Quantity ?? 0;
        }

        public bool AddItem(InventoryItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            var slot = Slots.FirstOrDefault(s => s.Item.Id == item.Id);
            if (slot != null)
            {
                slot.Quantity += quantity;
            }
            else
            {
                Slots.Add(new InventorySlotData(item, quantity));
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0) return false;

            var slot = Slots.FirstOrDefault(s => s.Item.Id == itemId);
            if (slot == null) return false;

            slot.Quantity -= quantity;
            if (slot.Quantity <= 0)
            {
                Slots.Remove(slot);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            Slots.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}
