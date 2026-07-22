namespace DeepEarth.Core
{
    public enum RelicRarity
    {
        Common    = 0,
        Rare      = 1,
        Legendary = 2
    }

    public enum RelicTag
    {
        Mining   = 0,
        Combat   = 1,
        Defense  = 2,
        Economy  = 3,
        Pickaxe  = 4,
        Healing  = 5,
        Status   = 6,
        Boss     = 7,
        Elite    = 8,
        Utility  = 9
    }

    public enum RelicEffectType
    {
        // ── 기존 ───────────────────────────────────────────────────────────
        AttackBonus                  = 0,
        MiningPowerBonus             = 1,
        MaxHPBonus                   = 2,
        ResourceMultiplierBonus      = 3,  // 전체 광물 획득 배율 (가산, 0.3 = +30%)
        BurnDurationModifier         = 4,
        BurnDamageModifier           = 5,
        BurnImmunityChance           = 6,
        MonsterAttackBonus           = 7,
        MonsterSpawnRateBonus        = 8,
        PickaxeDurabilityModifier    = 9,

        // ── 광물 타입별 획득 보너스 (가산, 0.2 = +20%) ─────────────────────
        IronGainBonus                = 10,
        SilverGainBonus              = 11,
        GoldGainBonus                = 12,
        DiamondGainBonus             = 13,

        // ── 인벤토리 ────────────────────────────────────────────────────────
        InventorySlotBonus           = 14,  // +N 슬롯

        // ── 전투·보스 후 회복 ────────────────────────────────────────────────
        PostCombatHealBonus          = 15,  // 전투 종료 후 HP +N
        PostBossHealBonus            = 16,  // 보스 처치 후 HP +N
        BossKillFullHeal             = 17,  // 보스 처치 시 HP 전부 회복 (value=1)
        HealingMultiplierModifier    = 18,  // 회복량 배율 (0.5 = -50%)

        // ── 곡괭이 확장 ──────────────────────────────────────────────────────
        PickaxeMaxDurabilityBonus    = 19,  // 최대 내구도 +N
        PickaxeDurabilityRateModifier = 20, // 내구도 감소율 배율 (0.8 = 20% 감소)
        PickaxeNoDurabilityLoss      = 21,  // 내구도 감소 없음 (value=1)
        PickaxeRepairOnKill          = 22,  // 처치 시 내구도 +N

        // ── 함정·전투 피해 ───────────────────────────────────────────────────
        TrapDamageReduction          = 23,  // Rockfall/SpikeTrap 피해 경감
        EliteDamageBonus             = 24,  // 엘리트에게 주는 피해 +% (0.1 = +10%)
        DamageMultiplierBonus        = 25,  // 모든 몬스터에게 주는 피해 +% (0.5 = +50%)

        // ── 채굴 시 확률 ─────────────────────────────────────────────────────
        LuckyMineChance              = 26,  // 채굴 시 동일 광물 추가 획득 확률
        MineHealChance               = 27,  // 채굴 시 HP +1 회복 확률

        // ── 처치 시 확률 ─────────────────────────────────────────────────────
        KillIronChance               = 28,  // 처치 시 철 +1 획득 확률

        // ── 상태이상 면역 ─────────────────────────────────────────────────────
        FloodImmunity                = 29,  // 수몰 피해 면역 (value=1)
        PoisonImmunity               = 30,  // 중독 면역 (value=1)

        // ── 엘리트 특수 ──────────────────────────────────────────────────────
        EliteKillRelicReward         = 31,  // 엘리트 처치 시 유물 추가 선택 (value=1)
        EliteRewardMultiplier        = 32,  // 엘리트 보상 배율 (2 = 2배)

        // ── 부활 ─────────────────────────────────────────────────────────────
        ReviveOnce                   = 33,  // 런 중 1회 부활, HP = value 비율 (0.3 = 30%)

        // ── 조건부 채굴 ──────────────────────────────────────────────────────
        ConditionalMiningBonus       = 34,  // 내구도 50% 이상 시 채굴력 +N
    }

    public enum RelicRewardContext
    {
        Standard,
        Boss,
        Elite,
        CrystalShrine,
        Merchant,
        Tombstone
    }
}
