namespace DeepEarth.Core
{
    // 2026-08-07 4단계 등급 체계로 개편 — Unique는 Rare와 Legendary 사이에 위치.
    // Common=0/Rare=1 값은 기존과 동일하게 유지, Legendary만 2→3으로 이동.
    public enum RelicRarity
    {
        Common    = 0,
        Rare      = 1,
        Unique    = 2,
        Legendary = 3
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

        // ── 신규: 노드 트리거(그룹 A) ───────────────────────────────────────
        HealBonus                    = 35,  // 트리거 발생 시 즉시 HP +N (노드 도착/아이템 사용 등과 함께 사용)
        NodeItemGrant                = 36,  // 트리거 발생 시 targetItems 중 랜덤 1개 지급 (value=수량)

        // ── 신규: 맵 생성 가중치(그룹 B) ──────────────────────────────────────
        NodeWeightBonus              = 37,  // 지정 RoomType의 맵 생성 가중치 +% (triggerNodeType으로 대상 지정)

        // ── 신규: 아이템/포션 트리거(그룹 C) ───────────────────────────────────
        DebuffClearAll               = 38,  // 트리거 시 화상+독 제거 (value=1)
        LowHpHealMultiplier          = 39,  // conditionHpRatioMax 이하일 때 포션 회복량 배율 (value=2 → 2배)

        // ── 신규: 전투 내 동적 스케일링/트리거(그룹 D) ──────────────────────────
        FirstAttackDamageBonus       = 40,  // 전투 첫 공격 피해 +% (1회 한정)
        CombatTurnAttackBonus        = 41,  // 전투 중 매 턴 공격력 +N (누적, 전투종료 리셋)
        CombatFirstTurnShieldBonus   = 42,  // 전투 첫 턴 한정 Shield +N
        PerMonsterAttackBonus        = 43,  // 조우 중인 몬스터 1마리당 공격력 +N
        CombatHitTakenAttackBonus    = 44,  // 전투 중 HP 실피해 1회당 공격력 +N (누적)
        EveryTurnShieldBonus         = 45,  // 전투 중 매 턴 시작 시 Shield +N

        // ── 신규: 처치/수리 트리거(그룹 E) ─────────────────────────────────────
        OnKillHealBonus              = 46,  // 몬스터 처치 시 HP +N
        OnHitHealBonus                = 47,  // 공격 적중 시 HP +N
        EliteKillPotionDropBonus     = 48,  // 엘리트 처치 시 포션 추가 드롭 (value=1)
        PickaxeFullRestoreOnKill     = 49,  // 엘리트/보스 처치 시 내구도 완전 회복 (value 미사용)
        OnRepairHealBonus            = 50,  // 곡괭이 수리 시 HP +N
        OnRepairMaxDurabilityChance  = 51,  // 수리 시 확률로 최대 내구도 +1 영구 증가
        RepairEfficiencyBonus        = 52,  // 수리 효율 +% (패시브, 트리거 아님)

        // ── 신규: 상점 가격(그룹 H) ─────────────────────────────────────────────
        PotionPriceReduction         = 53,  // 포션 구매 가격(뿌리) -N
        ShopDiscountBonus            = 54,  // 상점 전체 가격 -% (패시브)

        // ── 신규: 상태이상 일반화(그룹 I) ────────────────────────────────────────
        PoisonDurationModifier       = 55,  // 독 지속시간 +N턴 (패시브, BurnDurationModifier와 동일 패턴)
        StatusDamagePercentModifier  = 56,  // 화상+독 데미지 -% (패시브, ProcessActionTurn에서 공통 적용)
        CombatEndDebuffClear         = 57,  // 전투 종료 시 화상+독 제거 (value=1)

        // ── 신규: 확률 계열(그룹 J) ──────────────────────────────────────────────
        RareChanceBonus              = 58,  // 유물 획득 시 Rare 이상 확률 +% (전역, RollRarity)
        TreasureRareChanceBonus      = 59,  // 보물상자 유물 후보 중 Rare 가중치 +% (Treasure 컨텍스트 한정)

        // ── 신규: 최후의 일격(그룹 N) ────────────────────────────────────────────
        FinishingBlowMultiplier      = 60,  // 상대 HP비율 < 내 HP비율일 때 피해 배율 (전투당 1회, value=2 → 2배)

        // ── 신규: 캐릭터 시스템 개념 중복(그룹 M) ─────────────────────────────────
        TreasureRewardBonus          = 61,  // 보물상자 보상 옵션 개수 +1 (TreasureHunter 패시브와 동일 개념, value=1)
    }

    // 그룹 A/C — 효과가 즉시적용/패시브조회가 아니라 특정 게임 이벤트 발생 시 적용되는 경우의 트리거 종류.
    public enum RelicTriggerEvent
    {
        None,               // 즉시적용형/패시브조회형(기존 방식) — 대부분의 효과가 여기 해당
        NodeArrival,        // 특정 RoomType 노드 도착 시
        NodeCompletion,     // 특정 RoomType 노드 완료 시
        ItemUse,            // 소비 아이템(포션 포함 전체) 사용 시
        PotionUse,          // 포션(healAmount > 0인 아이템) 사용 시
        CombatTurnStart,    // 전투 중 매 턴 시작 시
        CombatFirstAttack,  // 전투 진입 후 첫 공격 시(1회 한정)
        CombatFirstTurnOnly,// 전투 첫 턴에만(1회 한정)
        CombatHitTaken,     // 전투 중 HP 실피해를 입을 때마다
        MonsterKilled,      // 몬스터(일반) 처치 시
        EliteKilled,        // 엘리트 몬스터 처치 시
        BossKilled,         // 보스(본체/파트) 처치 시
        PickaxeRepaired,    // 곡괭이 수리 성공 시
        PlayerDealtDamage,  // 플레이어가 몬스터에게 피해를 입힐 때마다
        CombatEnd,          // 전투 종료 시
    }

    // 그룹 F/K — 실시간으로 다른 게임 상태값을 조회해 "N당 +value" 형태로 스탯을 계산하는 소스.
    public enum RelicScalingSource
    {
        None,               // 스케일링 없음(기존 고정치 가산 방식)
        InventoryOreCount,  // 인벤토리 내 철+은+금+다이아 합산 개수
        MiningPower,        // StatManager.GetMiningPower()의 최종값
        PickaxeDurability,  // 곡괭이 현재 내구도
        Depth,              // 현재 깊이(m)
        HpLostPercent,      // 잃은 체력 비율(%, 0~100)
        CombatStatusDamage, // 전투 중 화상+독으로 누적된 데미지 총합
    }

    public enum RelicRewardContext
    {
        Standard,
        Boss,
        Elite,
        CrystalShrine,
        Merchant,
        Tombstone,
        Treasure,   // 신규(그룹 J) — 보물상자 유물 후보(rarity ≤ Rare) 전용, Legendary 없음
    }
}
