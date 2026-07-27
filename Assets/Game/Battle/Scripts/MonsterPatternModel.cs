using DeepEarth.Core;

namespace DeepEarth.Battle
{
    // 몬스터 1마리의 패턴 진행 상태(현재 스텝 인덱스) 관리.
    public class MonsterPatternModel
    {
        private readonly MonsterPatternData _data;
        private int _stepIndex;

        public MonsterPatternModel(MonsterPatternData data)
        {
            _data = data;
        }

        public PatternStepData CurrentStep => _data != null ? _data.GetStep(_stepIndex) : null;

        // 현재 스텝 실행 완료 후 호출 — 다음 스텝으로 진행하고 그 스텝을 반환한다(Intent 갱신용).
        public PatternStepData Advance()
        {
            _stepIndex++;
            return CurrentStep;
        }
    }
}
