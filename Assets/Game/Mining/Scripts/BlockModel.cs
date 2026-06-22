using DeepEarth.Common;
using UnityEngine;

namespace DeepEarth.Mining
{
    public class BlockModel
    {
        public BlockType Type { get; private set; }
        public int BaseHits { get; private set; }
        public int MaxHits { get; private set; }
        public int CurrentHits { get; private set; }

        public bool IsDestroyed => CurrentHits <= 0;

        public BlockModel(BlockType type, int depth)
        {
            Type = type;
            BaseHits = GetBaseHitsForType(type);
            MaxHits = Mathf.RoundToInt(BaseHits * (1f + (float)depth / 15f));
            CurrentHits = MaxHits;
        }

        private int GetBaseHitsForType(BlockType type)
        {
            switch (type)
            {
                case BlockType.Dirt:    return 1;
                case BlockType.Root:    return 2;
                case BlockType.Stone:   return 4;
                case BlockType.Iron:    return 8;
                case BlockType.Silver:  return 15;
                case BlockType.Gold:    return 25;
                case BlockType.Diamond: return 50;
                default:                return 1;
            }
        }

        public bool TakeHit(int damage)
        {
            if (CurrentHits <= 0) return false;

            CurrentHits -= damage;
            if (CurrentHits < 0) CurrentHits = 0;

            return true;
        }
    }
}
