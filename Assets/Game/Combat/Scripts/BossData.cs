using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.Combat
{
    public enum BossID
    {
        StoneGolem,
        MotherCaveSpider,
        SkeletonWarlord,
        AllMetalColossus
    }

    public class BossData
    {
        public BossID ID { get; private set; }
        public string BossNameKey { get; private set; }
        public int MaxHP { get; private set; }
        public int CurrentHP { get; private set; }
        public int Damage { get; private set; }
        public float AttackInterval { get; private set; }
        public int SpawnDepth { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public BossData(BossID id, int depth)
        {
            ID = id;
            SpawnDepth = depth;

            BossNameKey = id switch
            {
                BossID.StoneGolem       => "boss_stone_golem_name",
                BossID.MotherCaveSpider => "boss_mother_cave_spider_name",
                BossID.SkeletonWarlord  => "boss_skeleton_warlord_name",
                BossID.AllMetalColossus => "boss_all_metal_colossus_name",
                _                       => "boss_stone_golem_name"
            };

            int tier = depth / 50;
            // AllMetalColossus scales for each repeat beyond tier 4
            int colossusScale = Mathf.Max(0, tier - 4);

            switch (id)
            {
                case BossID.StoneGolem:
                    MaxHP = 60;
                    Damage = 3;
                    AttackInterval = 2.5f;
                    break;
                case BossID.MotherCaveSpider:
                    MaxHP = 80;
                    Damage = 0;
                    AttackInterval = 99999f; // never attacks directly
                    break;
                case BossID.SkeletonWarlord:
                    MaxHP = 120;
                    Damage = 3;
                    AttackInterval = 1.5f;
                    break;
                case BossID.AllMetalColossus:
                    MaxHP = 200 + colossusScale * 100;
                    Damage = 5 + colossusScale * 2;
                    AttackInterval = 3.0f;
                    break;
            }

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
