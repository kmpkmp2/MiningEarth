namespace DeepEarth.Common
{
    public enum BlockType
    {
        Dirt,
        Stone,
        Root,
        Iron,
        Silver,
        Gold,
        Diamond
    }

    public enum MonsterType
    {
        CaveRat,
        CaveSpider
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        EventPause,
        SettingsPause,
        GameOver,
        BossCombat,
        BossReward
    }

    public enum UpgradeType
    {
        MiningPower,
        MaxHP,
        Attack,
        InventorySize,
        PickaxeDurability
    }

    public static class AddressableKeys
    {
        // Blocks
        public const string BlockDirt = "Mining_Block_Dirt";
        public const string BlockStone = "Mining_Block_Stone";
        public const string BlockRoot = "Mining_Block_Root";
        public const string BlockIron = "Mining_Block_Iron";
        public const string BlockSilver = "Mining_Block_Silver";
        public const string BlockGold = "Mining_Block_Gold";
        public const string BlockDiamond = "Mining_Block_Diamond";

        // Monsters
        public const string MonsterRat = "Combat_Monster_Rat";
        public const string MonsterSpider = "Combat_Monster_Spider";
        public const string MonsterBossRat = "Combat_Boss_Rat";
        public const string MonsterBossSpider = "Combat_Boss_Spider";
        public const string MonsterBossGolem = "Combat_Boss_Golem";
        public const string MonsterBossWorm = "Combat_Boss_Worm";
        public const string MonsterBossTitan = "Combat_Boss_Titan";

        // UI Panels
        public const string UIPanelHUD = "UI_Panel_HUD";
        public const string UIPanelGameOver = "UI_Panel_GameOver";
        public const string UIPanelEvent = "UI_Panel_Event";
        public const string UIPanelSettings = "UI_Panel_Settings";
        public const string UIPanelBossRoom = "UI_Panel_BossRoom";
        public const string UIPanelBossReward = "UI_Panel_BossReward";
        public const string UIPanelRelicPopup = "UI_Panel_RelicPopup";
        public const string UIPanelInventoryPopup = "UI_Panel_InventoryPopup";
        public const string UIPanelEventReveal = "UI_Panel_EventReveal";
        public const string UIEffectIcon = "UI_EffectIcon";
        public const string UIEffectCard = "UI_EffectCard";
        public const string UIInventorySlot = "UI_Prefab_InventorySlot";
        
        // Items
        public const string ItemStone = "Item_Stone";
        public const string ItemWood = "Item_Wood";
        public const string ItemIron = "Item_Iron";
        public const string ItemSilver = "Item_Silver";
        public const string ItemGold = "Item_Gold";
        public const string ItemDiamond = "Item_Diamond";
        public const string ItemPotion = "Item_Potion";
        public const string ItemKey = "Item_Key";
        public const string ItemChest = "Item_Chest";
        public const string ItemSpecial = "Item_Special";

        public const string StatusEffectBurn = "StatusEffect_Burn";

        // Pickaxe
        public const string PickaxeDefault = "Pickaxe_Default";
        public const string PickaxeConfig = "Pickaxe_Config";

        // Relic Addressable Labels (label-based load — no per-relic key needed)
        public const string LabelRelicTreasure = "Relic_Treasure";
        public const string LabelRelicTombstone = "Relic_Tombstone";

        public const string FontMalgunSDF = "Font_Malgun_SDF";
        public const string FontDefault = "Font_Default";
        public const string FontNotoSansKR = "Font_NotoSansKR";

        // Map & Themes
        public const string MapWallSegment = "Map_Wall_Segment";
        public const string ThemeManager = "ThemeManager";
        public const string ThemeDirt = "Theme_Dirt";
        public const string ThemeStone = "Theme_Stone";
        public const string ThemeIron = "Theme_Iron";
        public const string ThemeGold = "Theme_Gold";
        public const string ThemeCrystal = "Theme_Crystal";
    }

    public enum EventRevealType
    {
        Treasure,
        Tombstone,
        MonsterRat,
        MonsterSpider,
        Water,
        Lava,
        Boss
    }

    public static class GameSettings
    {
        public const float BaseAttackInterval = 1f;
        public const int MaxBuffDebuffStack = 3;
    }
}
