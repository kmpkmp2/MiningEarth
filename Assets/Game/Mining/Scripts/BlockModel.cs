using DeepEarth.Common;

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
            MaxHits = BaseHits + GetDifficultyBonus(depth);
            CurrentHits = MaxHits;
        }

        private int GetBaseHitsForType(BlockType type)
        {
            switch (type)
            {
                case BlockType.Dirt: return 1;
                case BlockType.Stone: return 2;
                case BlockType.Root: return 1;
                case BlockType.Iron: return 3;
                case BlockType.Silver: return 2;
                case BlockType.Gold: return 3;
                case BlockType.Diamond: return 5;
                default: return 1;
            }
        }

        private int GetDifficultyBonus(int depth)
        {
            if (depth < 80) return 0;
            if (depth < 150) return 1;
            if (depth < 250) return 2;
            return 3;
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
