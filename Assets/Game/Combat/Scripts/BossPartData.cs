using DeepEarth.Common;

namespace DeepEarth.Combat
{
    // 부위 파괴형 보스(전금속 거인)의 파트 1개(광석 코어 또는 본체) 메타데이터.
    public class BossPartData
    {
        public BlockType OreType { get; }
        public bool IsMainBody { get; }

        public BossPartData(BlockType oreType, bool isMainBody)
        {
            OreType = oreType;
            IsMainBody = isMainBody;
        }
    }
}
