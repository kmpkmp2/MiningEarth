namespace DeepEarth.Combat
{
    public enum BossRewardType
    {
        // Normal Buffs
        Attack,
        MaxHP,
        Mining,
        Mineral20,
        SpawnDecrease,
        HealDrop,

        // Rare Buffs
        Revive,
        BossDamage,
        Mineral50,
        DoubleEvent
    }

    public class BossRewardChoice
    {
        public BossRewardType Type { get; private set; }
        public string TitleKey { get; private set; }
        public string DescriptionKey { get; private set; }
        public bool IsRare { get; private set; }

        public BossRewardChoice(BossRewardType type)
        {
            Type = type;
            IsRare = type switch
            {
                BossRewardType.Revive => true,
                BossRewardType.BossDamage => true,
                BossRewardType.Mineral50 => true,
                BossRewardType.DoubleEvent => true,
                _ => false
            };

            TitleKey = type switch
            {
                BossRewardType.Attack => "boss_buff_atk",
                BossRewardType.MaxHP => "boss_buff_hp",
                BossRewardType.Mining => "boss_buff_mining",
                BossRewardType.Mineral20 => "boss_buff_mineral",
                BossRewardType.SpawnDecrease => "boss_buff_spawn",
                BossRewardType.HealDrop => "boss_buff_heal",
                BossRewardType.Revive => "boss_rare_revive",
                BossRewardType.BossDamage => "boss_rare_boss_dmg",
                BossRewardType.Mineral50 => "boss_rare_mineral_50",
                BossRewardType.DoubleEvent => "boss_rare_event_double",
                _ => "boss_buff_atk"
            };

            DescriptionKey = type switch
            {
                BossRewardType.Attack => "boss_buff_atk_desc",
                BossRewardType.MaxHP => "boss_buff_hp_desc",
                BossRewardType.Mining => "boss_buff_mining_desc",
                BossRewardType.Mineral20 => "boss_buff_mineral_desc",
                BossRewardType.SpawnDecrease => "boss_buff_spawn_desc",
                BossRewardType.HealDrop => "boss_buff_heal_desc",
                BossRewardType.Revive => "boss_rare_revive_desc",
                BossRewardType.BossDamage => "boss_rare_boss_dmg_desc",
                BossRewardType.Mineral50 => "boss_rare_mineral_50_desc",
                BossRewardType.DoubleEvent => "boss_rare_event_double_desc",
                _ => "boss_buff_atk_desc"
            };
        }
    }
}
