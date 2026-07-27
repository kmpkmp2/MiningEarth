using DeepEarth.Core;

namespace DeepEarth.Battle
{
    // 현재 표시 중인 몬스터 Intent(다음 턴 예고 행동) 보관.
    public class IntentModel
    {
        public IntentType CurrentIntent { get; set; }
        public int CurrentValue { get; set; }
    }
}
