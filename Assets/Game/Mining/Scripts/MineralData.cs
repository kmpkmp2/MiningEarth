using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Mining
{
    [CreateAssetMenu(menuName = "DeepEarth/Mining/MineralData", fileName = "Mineral_New")]
    public class MineralData : ScriptableObject
    {
        public BlockType blockType;
        public string itemID;
        public int baseHits;
        public int baseRewardCount;
        public int unlockDepth;
    }
}
