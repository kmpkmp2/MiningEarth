using DeepEarth.Common;

namespace DeepEarth.Mining
{
    public class RewardModel
    {
        public BlockType Type;
        public string ItemID;
        public int Base;
        public int DepthBonus;
        public int RelicBonus;
        public int BuffBonus;
        public int EventBonus;
        public int FinalAmount;
        public bool LuckyMineTriggered;
        public bool MineHealTriggered;
    }
}
