using System;
using System.Collections.Generic;
using UnityEngine;
using DeepEarth.Common;

namespace DeepEarth.Core
{
    [CreateAssetMenu(fileName = "Elite_SpawnTable", menuName = "DeepEarth/Elite/EliteSpawnTable")]
    public class EliteSpawnTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public MonsterType monsterType;
            [Min(0)] public int minDepth;
            [Min(1)] public int weight = 1;
        }

        public List<Entry> entries = new List<Entry>();

        public MonsterType PickElite(int depth)
        {
            var eligible = new List<Entry>();
            foreach (var e in entries)
                if (depth >= e.minDepth) eligible.Add(e);

            if (eligible.Count == 0) return MonsterType.BigSlime;

            int total = 0;
            foreach (var e in eligible) total += e.weight;

            int roll = UnityEngine.Random.Range(0, total);
            int acc = 0;
            foreach (var e in eligible)
            {
                acc += e.weight;
                if (roll < acc) return e.monsterType;
            }
            return eligible[eligible.Count - 1].monsterType;
        }
    }
}
