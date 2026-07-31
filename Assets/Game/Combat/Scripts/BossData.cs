using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.Combat
{
    public enum BossID
    {
        StoneGolem,
        MotherCaveSpider,
        SkeletonWarlord,
        AllMetalColossus,
        CaveRat
    }

    // 보스 스탯 공식 소스. HP 추적은 Combat.MonsterModel(BossManager가 이 값들로 생성)이 담당한다.
    public class BossData
    {
        public BossID ID { get; private set; }
        public string BossNameKey { get; private set; }
        public int MaxHP { get; private set; }
        public int Damage { get; private set; }
        public int SpawnDepth { get; private set; }

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
                BossID.CaveRat          => "boss_cave_rat_name",
                _                       => "boss_stone_golem_name"
            };

            int tier = depth / 50;
            int colossusScale = Mathf.Max(0, tier - 4);

            switch (id)
            {
                case BossID.StoneGolem:
                    MaxHP = 60;
                    Damage = 3;
                    break;
                case BossID.MotherCaveSpider:
                    MaxHP = 80;
                    Damage = 0;
                    break;
                case BossID.SkeletonWarlord:
                    MaxHP = 120;
                    Damage = 3;
                    break;
                case BossID.AllMetalColossus:
                    MaxHP = 200 + colossusScale * 100;
                    Damage = 5 + colossusScale * 2;
                    break;
                case BossID.CaveRat:
                    MaxHP = 60;
                    Damage = 3;
                    break;
            }
        }
    }
}
