namespace DeepEarth.Combat
{
    public enum EliteSkillType
    {
        None,
        SplitOnDeath,  // BigSlime: splits into 3 Slimes on death
        MineralTheft,  // SkeletonMiner: periodically steals minerals
        IronArmor,     // IronPlateSpider: damage reduction until armor breaks + poison
        TreasureTable, // MerchantMimic: depth-scaled mineral or buff rewards
        Enrage,        // CursedKnight: enrages at 75%/50%/25% HP thresholds
        AttackDebuff   // CursedPriest: applies attack damage curse every 1s during combat
    }
}
