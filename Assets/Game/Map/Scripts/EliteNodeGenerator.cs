using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Replaces eligible Monster nodes on each main path with Elite nodes.
    ///
    /// Constraints:
    ///   — Per main path: at most 3 Elite nodes.
    ///   — No two consecutive Elite nodes on the same main path (floor gap ≥ 2).
    ///   — Min 5 floors away from Mine Entrance (floor 0).
    ///   — Min 5 floors away from the last regular floor (boss connector floor).
    ///   — Only Monster nodes are replaced; Mine / Treasure / Rest etc. are untouched.
    /// </summary>
    public class EliteNodeGenerator
    {
        private const int MaxElitePerPath   = 3;
        private const int MinFloorFromStart = 10; // matches RuleValidator.EarlyFloorMax (floors 0-9 are Mine/Event/Merchant only)
        private const int MinFloorFromBoss  = 5;

        private readonly IRandomProvider _rng;

        public EliteNodeGenerator(IRandomProvider rng)
        {
            _rng = rng;
        }

        public void PlaceEliteNodes(MapData mapData, PathGenerator pathGenerator)
        {
            int floors     = mapData.Floors;
            int pathCount  = pathGenerator.PathCount;
            int floorMin   = MinFloorFromStart;
            int floorMax   = floors - 1 - MinFloorFromBoss; // last regular floor - 5

            if (floorMin > floorMax) return; // map too small for any elites

            for (int pathIdx = 0; pathIdx < pathCount; pathIdx++)
            {
                int[] pathCols = pathGenerator.GetMainPathCols(pathIdx);
                if (pathCols == null) continue;

                // Collect eligible floors for this main path
                var eligible = new List<int>();
                for (int floor = floorMin; floor <= floorMax; floor++)
                {
                    int col = pathCols[floor];
                    MapNode node = mapData.Grid[floor, col];
                    if (node.IsActive && node.RoomType == RoomType.Monster)
                        eligible.Add(floor);
                }

                // Shuffle to randomize which floors are chosen
                ShuffleList(eligible);

                // Greedily place elites with no-consecutive constraint
                int placed = 0;
                int lastEliteFloor = int.MinValue;

                // Sort by floor to apply the consecutive constraint correctly
                var sorted = new List<int>(eligible);
                sorted.Sort();

                foreach (int floor in sorted)
                {
                    if (placed >= MaxElitePerPath) break;
                    if (floor - lastEliteFloor < 2) continue; // consecutive guard

                    int col = pathCols[floor];
                    mapData.Grid[floor, col].RoomType = RoomType.Elite;
                    lastEliteFloor = floor;
                    placed++;
                }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (placed > 0)
                    Debug.Log($"[Map]\nElite Nodes Placed\nPath : {pathIdx}  Count : {placed}");
#endif
            }
        }

        private void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
