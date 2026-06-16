namespace DeepEarth.UI
{
    public enum ItemType
    {
        Resource,
        Consumable
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public class InventoryItemData
    {
        public string Id;
        public string NameKey;
        public string DescriptionKey;
        public ItemType Type;
        public ItemRarity Rarity;
        public string IconKey;

        public InventoryItemData(string id, string nameKey, string descriptionKey, ItemType type, ItemRarity rarity, string iconKey)
        {
            Id = id;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            Type = type;
            Rarity = rarity;
            IconKey = iconKey;
        }
    }
}
