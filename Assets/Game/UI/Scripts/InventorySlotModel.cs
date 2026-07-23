namespace DeepEarth.UI
{
    [System.Serializable]
    public class InventorySlotModel
    {
        public string ItemID;
        public ItemData Item;
        public int Count;
        public int MaxStack;
        public int SlotIndex;

        public bool IsFull => Count >= MaxStack;
        public int Available => MaxStack - Count;
    }
}
