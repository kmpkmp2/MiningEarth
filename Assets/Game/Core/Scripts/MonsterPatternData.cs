using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [Serializable]
    public class PatternStepData
    {
        public IntentType intentType;
        public int value; // Attack=피해량, Heal=회복량, Debuff/Buff=효과 강도 등 intentType별 의미
    }

    // 몬스터 타입별 1개 asset. 마지막 스텝 이후 처음으로 순환한다.
    [CreateAssetMenu(fileName = "MonsterPattern_New", menuName = "DeepEarth/MonsterPatternData")]
    public class MonsterPatternData : ScriptableObject
    {
        public MonsterType monsterType;
        public List<PatternStepData> steps = new List<PatternStepData>();

        public PatternStepData GetStep(int index)
        {
            if (steps.Count == 0) return null;
            return steps[index % steps.Count];
        }
    }
}
