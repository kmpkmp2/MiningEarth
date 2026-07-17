using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Generates a Main Path + Short Branch + Quick Merge map structure.
    ///
    /// Pipeline:
    ///   1. Two Start Nodes at Floor 0; both connect into the single Main Path at Floor 1.
    ///   2. One Main Path runs from Floor 0 to Floor (Floors-1), one active node per floor.
    ///      Direction weights: straight 70 %, left 15 %, right 15 %.
    ///      No two consecutive steps in the same lateral direction.
    ///   3. Short Branches (length 1-2) spawn from Main Path nodes with BranchProbability,
    ///      subject to BranchCooldown floors between consecutive branches.
    ///      Branches never spawn from Floor 0 (Start/Mine nodes) and never re-branch.
    ///   4. Every branch merges back to the Main Path within MaxBranchLength floors.
    ///
    /// Cross-detection is not needed: branches always occupy the column adjacent to the
    /// Main Path and never cross it.
    /// </summary>
    public class PathGenerator
    {
        public const float BranchProbability = 0.25f; // ~25 % (20-30 % range)
        public const int   BranchCooldown    = 5;     // floors between branches
        public const int   MaxBranchLength   = 2;     // max branch length in floors

        private readonly int             _columns;
        private readonly int             _floors;
        private readonly IRandomProvider _rng;

        private int[]                              _mainPathCols;
        private readonly List<(int floor, int len)> _branches = new();

        // ─── Public metadata (read after GeneratePaths) ────────────────────────

        public IReadOnlyList<int>               MainPathCols    => _mainPathCols;
        public IReadOnlyList<(int floor, int)>  Branches        => _branches;
        public int                              LastBranchFloor { get; private set; } = -1;

        // ─── Construction ──────────────────────────────────────────────────────

        public PathGenerator(GridTemplate template, IRandomProvider rng)
        {
            _columns = template.Columns;
            _floors  = template.Floors;
            _rng     = rng;
        }

        // ─── Entry point ───────────────────────────────────────────────────────

        public void GeneratePaths(MapData mapData)
        {
            _branches.Clear();
            LastBranchFloor = -1;

            GenerateMainPath(mapData);
            GenerateBranches(mapData);
        }

        // ─── Main Path ─────────────────────────────────────────────────────────

        private void GenerateMainPath(MapData mapData)
        {
            // Two distinct start columns at Floor 0
            int startColA = _rng.Range(0, _columns);
            int startColB;
            do { startColB = _rng.Range(0, _columns); }
            while (startColB == startColA);

            _mainPathCols    = new int[_floors];
            _mainPathCols[0] = startColA;

            int currentCol = startColA;
            int lastDelta  = 0;

            for (int floor = 0; floor < _floors - 1; floor++)
            {
                int nextCol = PickMainPathNext(currentCol, lastDelta);
                _mainPathCols[floor + 1] = nextCol;
                CommitEdge(mapData, floor, currentCol, nextCol);
                lastDelta  = nextCol - currentCol;
                currentCol = nextCol;
            }

            // Second start node (startColB) merges into the Main Path at floor 1
            CommitEdge(mapData, 0, startColB, _mainPathCols[1]);
        }

        private int PickMainPathNext(int col, int lastDelta)
        {
            int roll  = _rng.Range(0, 100);
            int delta = roll < 70 ? 0 : roll < 85 ? -1 : 1; // straight / left / right

            // No two consecutive lateral moves in the same direction
            if (delta == lastDelta && delta != 0) delta = 0;

            return Mathf.Clamp(col + delta, 0, _columns - 1);
        }

        // ─── Short Branches ────────────────────────────────────────────────────

        private void GenerateBranches(MapData mapData)
        {
            int lastBranchFloor = -(BranchCooldown + 1); // allow branch as early as floor 1

            // Floor 0 is always Start/Mine (cannot branch); stop at floors-3 so merge fits
            for (int floor = 1; floor < _floors - 2; floor++)
            {
                if (floor - lastBranchFloor < BranchCooldown) continue;
                if (_rng.Range(0, 100) >= Mathf.RoundToInt(BranchProbability * 100f)) continue;

                // Prefer random length; fall back to shorter if preferred fails
                int preferred = _rng.Range(0, 2) == 0 ? 1 : MaxBranchLength;
                bool created  = TryCreateBranch(mapData, floor, preferred);
                if (!created && preferred == MaxBranchLength)
                    created = TryCreateBranch(mapData, floor, 1);

                if (created)
                {
                    lastBranchFloor = floor;
                    LastBranchFloor = floor;
                }
            }
        }

        /// <summary>
        /// Attempts to create a branch of <paramref name="branchLen"/> floors starting at
        /// <paramref name="startFloor"/>. Returns true and commits edges on success.
        /// </summary>
        private bool TryCreateBranch(MapData mapData, int startFloor, int branchLen)
        {
            int mergeFloor = startFloor + branchLen + 1;
            if (mergeFloor >= _floors) return false;

            int mainColStart = _mainPathCols[startFloor];
            int mergeCol     = _mainPathCols[mergeFloor];

            // Branch column 1: adjacent to mainColStart, NOT on the main path at startFloor+1
            var col1Cands = new List<int>(2);
            for (int dc = -1; dc <= 1; dc += 2) // only left / right, never straight (that's main)
            {
                int c = mainColStart + dc;
                if (c < 0 || c >= _columns) continue;
                if (c == _mainPathCols[startFloor + 1]) continue;
                col1Cands.Add(c);
            }
            if (col1Cands.Count == 0) return false;

            int bc1 = col1Cands[_rng.Range(0, col1Cands.Count)];

            if (branchLen == 1)
            {
                // bc1 must be able to reach mergeCol in one step
                if (Mathf.Abs(bc1 - mergeCol) > 1) return false;

                CommitEdge(mapData, startFloor,     mainColStart, bc1);
                CommitEdge(mapData, startFloor + 1, bc1,         mergeCol);
                _branches.Add((startFloor, 1));
                return true;
            }

            // branchLen == 2: need a second branch node
            var col2Cands = new List<int>(3);
            for (int c = bc1 - 1; c <= bc1 + 1; c++)
            {
                if (c < 0 || c >= _columns) continue;
                if (c == _mainPathCols[startFloor + 2]) continue; // stay off main path
                if (Mathf.Abs(c - mergeCol) > 1) continue;        // must reach merge
                col2Cands.Add(c);
            }
            if (col2Cands.Count == 0) return false;

            int bc2 = col2Cands[_rng.Range(0, col2Cands.Count)];

            CommitEdge(mapData, startFloor,     mainColStart, bc1);
            CommitEdge(mapData, startFloor + 1, bc1,         bc2);
            CommitEdge(mapData, startFloor + 2, bc2,         mergeCol);
            _branches.Add((startFloor, 2));
            return true;
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        private static void CommitEdge(MapData mapData, int fromFloor, int fromCol, int toCol)
        {
            var conn = new MapConnection(fromFloor, fromCol, fromFloor + 1, toCol);
            mapData.Grid[fromFloor,     fromCol].AddOutgoingConnection(conn);
            mapData.Grid[fromFloor + 1, toCol  ].AddIncomingConnection(conn);
        }
    }
}
