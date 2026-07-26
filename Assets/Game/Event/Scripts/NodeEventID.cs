namespace DeepEarth.Event
{
    public enum NodeEventID
    {
        None = 0,

        // Negative events
        Rockfall          = 1,
        PoisonMist        = 2,
        SpikeTrap         = 3,
        CollapsedTunnel   = 4,
        DarkAltar         = 5,
        LostSpirit        = 6,

        // Positive events
        AbandonedCamp     = 7,
        RichOreVein       = 8,
        HotSpring         = 10,
        AbandonedEquipment = 11,
        MinersJournal     = 12,
        CrystalShrine     = 13,
        AncientForge      = 14,

        // Risk/Reward events
        DevilContract     = 15,
        MineGodBlessing   = 16
    }

    public enum NodeEventCategory
    {
        Negative,
        Positive,
        RiskReward
    }
}
