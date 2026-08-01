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

        public BlockModel(BlockType type, int depth, int baseHits)
        {
            Type = type;
            BaseHits = baseHits;
            MaxHits = Mathf.RoundToInt(BaseHits * (1f + (float)depth / 15f));
            CurrentHits = MaxHits;
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
