namespace DeepEarth.UI
{
    public struct ShopItemDisplayData
    {
        public string name;
        public string description;
        public string iconKey;
        public string stat1Text;
        public string stat2Text;
        public string costText;
        public bool   isUnlocked;
        public bool   isEquipped;
        public bool   canAfford;
        public object tag; // PickaxeData, CharacterData, etc. — opaque to View
    }
}
