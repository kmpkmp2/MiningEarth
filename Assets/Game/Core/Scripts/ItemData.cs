using UnityEngine;

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

    [CreateAssetMenu(fileName = "Item_New", menuName = "DeepEarth/Item/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemID;
        public string nameLocKey;
        public string descLocKey;
        public ItemType type;
        public ItemRarity rarity;
        public string iconKey;
        public int maxStack = 1;

        // Use-effect fields — 0 / false means "no effect of this kind" (RelicData-style flat optional fields)
        public int healAmount;
        public int repairAmount;
        public int reviveHP;
        public bool autoUseOnDeath;
        public bool cureBurn;
    }
}
