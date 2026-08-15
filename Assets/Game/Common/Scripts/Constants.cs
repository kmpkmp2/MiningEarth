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
        CaveSpider,
        Slime,
        SmallSlime,
        Skeleton,
        ArmoredSkeleton,
        Mimic,

        // Elite monsters
        BigSlime,
        SkeletonMiner,
        IronPlateSpider,
        MerchantMimic,
        CursedKnight,
        CursedPriest,

        // Boss bodies (턴제 전환) — 세이브에 저장되지 않는 enum이라 끝에 추가해도 안전.
        StoneGolemBoss,
        MotherCaveSpiderBoss,
        SkeletonWarlordBoss,
        AllMetalColossusBoss,
        BossCore,
        CaveRatBoss,

        // 3단계 구조 확장(2026-08) — 정규 6종 + 엘리트 3종. 기존 enum 값의 정수 매핑을 깨지 않도록
        // 반드시 끝에만 추가한다(MonsterData/SpawnTable 에셋이 monsterType을 정수로 직렬화하기 때문).
        OreBurrower,
        CrystalBat,
        MineMycelium,
        PetrifiedMole,
        AbyssMinerBee,
        GoldVeinSpirit,
        MagmaLizard,
        CrystalSerpent,
        FossilTroll
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        MapSelecting,
        EventPause,
        SettingsPause,
        GameOver,
        BossCombat,
        BossReward,
        Victory
    }

    public enum UpgradeType
    {
        MiningPower,
        MaxHP,
        Attack,
        InventorySize,
        PickaxeDurability,
        RepairEfficiency,
        Luck,
        EventRate
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

        // New Boss Prefabs
        public const string MonsterBossStoneGolem       = "Combat_Boss_StoneGolem";
        public const string MonsterBossMotherSpider     = "Combat_Boss_MotherCaveSpider";
        public const string MonsterBossSkeletonWarlord  = "Combat_Boss_SkeletonWarlord";
        public const string MonsterBossAllMetalColossus = "Combat_Boss_AllMetalColossus";

        // UI Panels
        public const string ShopItemSlot = "UI_Prefab_ShopItemSlot";
        public const string UIPanelHUD = "UI_Panel_HUD";
        public const string UIPanelGameOver = "UI_Panel_GameOver";
        public const string UIPanelEvent = "UI_Panel_Event";
        public const string UIPanelSettings = "UI_Panel_Settings";
        public const string UIPanelBossRoom = "UI_Panel_BossRoom";
        public const string UIPanelBossReward = "UI_Panel_BossReward";
        public const string UIPanelRelicPopup = "UI_Panel_RelicPopup";
        public const string UIPanelRelicCopyPopup = "UI_Panel_RelicCopyPopup"; // 그룹 L(수집가의 가방) 전용
        public const string UIPanelInventoryPopup = "UI_Panel_InventoryPopup";
        public const string UIPanelEventReveal = "UI_Panel_EventReveal";
        public const string UIPanelMerchant = "UI_Panel_Merchant";
        public const string UIPanelAchievementNotification = "UI_Panel_AchievementNotification";
        public const string UIEffectIcon = "UI_EffectIcon";
        public const string UIEffectCard = "UI_EffectCard";
        public const string UIInventorySlot = "UI_Prefab_InventorySlot";
        
        // Items
        public const string ItemStone = "Item_Stone";
        public const string ItemWood = "Item_Wood";
        public const string ItemDirt = "Item_Dirt";
        public const string ItemIron = "Item_Iron";
        public const string ItemSilver = "Item_Silver";
        public const string ItemGold = "Item_Gold";
        public const string ItemDiamond = "Item_Diamond";
        public const string ItemPotion = "Item_Potion";
        public const string ItemPotionMedium = "Item_Potion_Medium";
        public const string ItemPotionLarge = "Item_Potion_Large";
        public const string ItemBurnCure = "Item_BurnCure";
        public const string ItemKey = "Item_Key";
        public const string ItemChest = "Item_Chest";
        public const string ItemSpecial = "Item_Special";
        public const string ItemPortableAnvil = "Item_PortableAnvil";
        public const string ItemImmortalityPotion = "Item_ImmortalityPotion";
        public const string ItemRepairKit = "Item_RepairKit";
        public const string ItemAntidotePotion = "Item_AntidotePotion";
        public const string ItemBomb = "Item_Bomb";
        public const string ItemHolyWater = "Item_HolyWater";
        public const string ItemEnhancedRepairKit = "Item_EnhancedRepairKit";
        public const string ItemEnhancedBomb = "Item_EnhancedBomb";
        public const string ItemAngelFeather = "Item_AngelFeather";
        public const string ItemFullRepairKit = "Item_FullRepairKit";
        public const string ItemGuardianHeart = "Item_GuardianHeart";

        public const string StatusEffectBurn       = "StatusEffect_Burn";
        public const string StatusEffectMiningDown = "StatusEffect_MiningPowerDown";
        public const string StatusEffectMiningUp   = "StatusEffect_MiningPowerUp";

        // Monster Prefabs
        public const string MonsterSlime           = "Combat_Monster_Slime";
        public const string MonsterSmallSlime      = "Combat_Monster_SmallSlime";
        public const string MonsterSkeleton        = "Combat_Monster_Skeleton";
        public const string MonsterArmoredSkeleton = "Combat_Monster_ArmoredSkeleton";
        public const string MonsterMimic           = "Combat_Monster_Mimic";

        // Monster Data (label + per-asset keys)
        public const string LabelMonsterData       = "MonsterData";
        public const string MonsterSpawnTableKey   = "Monster_SpawnTable";
        public const string MimicRewardTableKey    = "Monster_MimicRewardTable";

        // Elite Monster Prefabs
        public const string EliteBigSlime       = "Combat_Elite_BigSlime";
        public const string EliteSkeletonMiner  = "Combat_Elite_SkeletonMiner";
        public const string EliteIronPlateSpider = "Combat_Elite_IronPlateSpider";
        public const string EliteMerchantMimic  = "Combat_Elite_MerchantMimic";
        public const string EliteCursedKnight   = "Combat_Elite_CursedKnight";
        public const string EliteCursedPriest   = "Combat_Elite_CursedPriest";

        // Elite System Data
        public const string EliteSpawnTableKey  = "Elite_SpawnTable";
        public const string LabelEliteData      = "EliteData";

        // Pickaxe
        public const string PickaxeDefault = "Pickaxe_Default";
        public const string PickaxeConfig  = "Pickaxe_Config";

        // Achievement
        public const string LabelAchievement = "Achievement";

        // Node Event System
        public const string LabelNodeEvent = "NodeEvent";

        // Relic System (unified label — replaces Relic_Treasure / Relic_Tombstone)
        public const string LabelRelic           = "Relic";
        public const string RelicRewardConfigKey = "Relic_RewardConfig";

        // Pickaxe label (multi-load)
        public const string LabelPickaxe = "Pickaxe";

        // Item label (multi-load)
        public const string LabelItemData = "ItemData";

        // Game Balance (single config asset)
        public const string GameBalanceDataKey = "GameBalanceData";

        // Merchant System (label + config assets)
        public const string LabelMerchant           = "Merchant";
        public const string MerchantBalanceDataKey  = "MerchantBalanceData";
        public const string MerchantQuoteDataKey    = "MerchantQuoteData";

        // Merchant art/audio — reserved keys, real assets not yet produced (loads no-op until then)
        public const string MerchantPortrait        = "Merchant_Portrait";
        public const string MerchantBGM             = "Merchant_BGM";
        public const string MerchantSFXBuy          = "Merchant_SFX_Buy";
        public const string MerchantSFXDiscount     = "Merchant_SFX_Discount";
        public const string MerchantLegendaryGlow   = "Merchant_LegendaryGlow";

        // Turn-Based Battle System (일반 몬스터 + 엘리트 + 보스 공용)
        public const string LabelMonsterPattern     = "MonsterPattern";
        public const string LabelElitePattern       = "ElitePattern";
        public const string LabelBossPattern        = "BossPattern";
        public const string BattleBalanceDataKey    = "BattleBalanceData";
        public const string MonsterIntentDataKey    = "MonsterIntentData";
        public const string ShieldDataKey           = "ShieldData";
        public const string UIPanelBattle           = "UI_Panel_Battle";

        // Battle art/audio — reserved keys, real assets not yet produced (loads no-op until then)
        public const string BattleDefenseEffect        = "Battle_DefenseEffect";
        public const string BattleTurnChangeEffect     = "Battle_TurnChangeEffect";
        public const string BattleSFXPlayerTurn        = "Battle_SFX_PlayerTurn";
        public const string BattleSFXMonsterTurn       = "Battle_SFX_MonsterTurn";
        public const string BattleSFXAttack            = "Battle_SFX_Attack";
        public const string BattleSFXDefend            = "Battle_SFX_Defend";
        public const string BattleSFXDebuff            = "Battle_SFX_Debuff";
        public const string BattleSFXBuff              = "Battle_SFX_Buff";
        public const string BattleSFXHeavyAttack       = "Battle_SFX_HeavyAttack";
        public const string BattleSFXIntentChange      = "Battle_SFX_IntentChange";

        // Target Select — Target_Ring은 실제 플레이스홀더 스프라이트 등록, 나머지는 예약 키(에셋 없이 no-op)
        public const string TargetRingIcon          = "Target_Ring";
        public const string TargetArrowIcon         = "Target_Arrow";
        public const string BattleSFXTargetSelect   = "Battle_SFX_TargetSelect";

        // Relic Addressable Labels (label-based load — no per-relic key needed)
        public const string LabelRelicTreasure = "Relic_Treasure";
        public const string LabelRelicTombstone = "Relic_Tombstone";

        // Character System (label-based load)
        public const string LabelCharacterData = "CharacterData";

        // Starting Relic System (완전히 별도 시스템 — RelicManager/LabelRelic과 분리)
        public const string LabelStartingRelic = "StartingRelic";

        // Character Class System — Torch item
        public const string ItemTorch = "Item_Torch";

        public const string FontMalgunSDF = "Font_Malgun_SDF";
        public const string FontDefault = "Font_Default";
        public const string FontNotoSansKR = "Font_NotoSansKR";

        // Mining Data
        public const string DepthRewardTable = "Mining_DepthRewardTable";
        public const string LabelMineralData = "MineralData";
        public const string MiningSFXPickup  = "Mining_SFX_Pickup";
        public const string UIPrefabMiningRewardIcon = "UI_Prefab_MiningRewardIcon";

        // Route Map
        public const string UIPanelMapPopup       = "UI_Panel_MapPopup";
        public const string MapNodePrefab          = "Map_Node";
        public const string MapLinePrefab          = "Map_Line";
        public const string MapGenerationConfig    = "Map_GenerationConfig";
        public const string MapNodeIconData        = "Map_NodeIconData";

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
        MonsterSlime,
        Water,
        Lava,
        Boss,
        Rest
    }

    public static class GameSettings
    {
        public const int MaxBuffDebuffStack = 3;
        public const int BossSpawnDepthInterval = 50;

        // 3단계 구조(2026-08): StoneGolem(50) → SkeletonWarlord(100) → AllMetalColossus(150, 최종).
        // 이 깊이의 보스를 처치하면 런이 승리로 종료된다.
        public const int FinalBossDepth = 150;

        public const int MaxHPUpgradeLevelCap = 20;
        public const int AttackUpgradeLevelCap = 20;
        public const int MiningPowerUpgradeLevelCap = 50;

        // ── 신규 유저 초반 런 밸런싱 (후퇴/응급수리/쉼터 체크포인트) ──────────────
        public const int RetreatPickaxeDurabilityCost = 5;
        public const int EmergencyRepairHpCost = 2;
        public const int EmergencyRepairDurabilityGain = 10;
        public const int EmergencyRepairMaxUsesPerRun = 3;
        public const int RestCheckpointInterval = 12;
        public const float RestCheckpointHealRatio = 0.3f;
        public const int RestCheckpointDurabilityGain = 15;
    }

    public static class SceneNames
    {
        public const string Bootstrap = "BootstrapScene";
        public const string Loading   = "LoadingScene";
        public const string StartMenu = "StartMenuScene";
        public const string MainGame  = "MainGameScene";
    }

    public static class AudioMixerParams
    {
        public const string MasterVolume = "MasterVolume";
        public const string BGMVolume    = "BGMVolume";
        public const string SFXVolume    = "SFXVolume";
        public const string UIVolume     = "UIVolume";
    }
}
