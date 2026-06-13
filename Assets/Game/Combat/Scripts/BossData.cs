using System;
using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.Combat
{
    public enum BossID
    {
        CaveRat,
        QueenSpider,
        RockGolem,
        LavaWorm,
        CrystalTitan
    }

    public class BossData
    {
        public BossID ID { get; private set; }
        public string BossNameKey { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int Damage { get; private set; }
        public float AttackInterval { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public BossData(BossID id, int depth)
        {
            ID = id;
            AttackInterval = GameSettings.BaseAttackInterval;

            // Base values for Tier 1-5
            // Depth 50 (Tier 1), 100 (Tier 2), 150 (Tier 3), 200 (Tier 4), 250 (Tier 5)
            int tier = depth / 50;
            int cycle = (tier - 1) / 5;
            int index = (tier - 1) % 5;

            int[] baseHPs = { 20, 40, 70, 100, 150 };
            int[] baseDamages = { 1, 2, 3, 4, 5 };

            BossNameKey = id switch
            {
                BossID.CaveRat => "boss_rat_name",
                BossID.QueenSpider => "boss_spider_name",
                BossID.RockGolem => "boss_golem_name",
                BossID.LavaWorm => "boss_worm_name",
                BossID.CrystalTitan => "boss_titan_name",
                _ => "boss_rat_name"
            };

            if (id == BossID.CaveRat)
            {
                // Slow attack rate (slow attack speed)
                AttackInterval = 2.0f;
            }

            MaxHP = baseHPs[index] + cycle * 150;
            Damage = baseDamages[index] + cycle * 5;
            CurrentHP = MaxHP;
        }

        public bool TakeDamage(int amount)
        {
            if (CurrentHP <= 0) return false;

            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;

            return true;
        }
    }
}
