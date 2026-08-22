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

        // 광물별 출현 가중치 — 같은 깊이에서 동시에 해금된 광물들 사이의 상대적 출현 확률.
        // 기본값 1 = 기존 완전 균등 랜덤과 동일. MiningSystem.ChooseBlockTypeByDepth가 가중 랜덤에 사용.
        [Min(0.01f)] public float spawnWeight = 1f;
    }
}
