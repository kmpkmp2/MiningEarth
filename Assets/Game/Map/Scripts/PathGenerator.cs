using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Generates 2-3 independent Main Paths plus Short Branches that merge back to Main Paths.
    ///
    /// Pipeline:
    ///   1. Pick path count (MinPathCount..MaxPathCount).
    ///   2. Assign evenly-spaced home columns and generate each Main Path sequentially.
    ///      During MinPathIndependenceFloors: each path stays within 1 column of its home zone.
    ///      After that: free movement (straight 70 %, left 15 %, right 15 %).
    ///      Later paths avoid occupying the same column as earlier paths during independence.
    ///   3. Generate Short Branches from each Main Path node:
    ///      length BranchMinLength..BranchMaxLength; each branch merges into the nearest Main Path.
    ///      BranchProbability per eligible floor, subject to BranchCooldown per path.
    ///
    /// Duplicate-edge detection prevents the same (from→to) connection being committed twice,
    /// which can happen when multiple paths share a column after the independence period.
    /// </summary>
    public class PathGenerator
    {
        private readonly int              _columns;
        private readonly int              _floors;
        private readonly int              _minPathCount;
        private readonly int              _maxPathCount;
        private readonly int              _minIndependenceFloors;
        private readonly int              _branchMinLength;
        private readonly int              _branchMaxLength;
        private readonly int              _branchProbabilityPct; // 0-100
        private readonly int              _branchCooldown;
        private readonly int              _maxNodesPerFloor;
        private readonly IRandomProvider  _rng;

        // Per-generation state
        private int     _pathCount;
        private int[]   _homeCol;
        private int[][] _mainPathCols;                              // [pathIndex][floor]

        private readonly HashSet<(int, int, int, int)> _committedEdges     = new();
        private readonly HashSet<int>[]                _activeColsPerFloor;  // [floor] → active columns

        private readonly List<(int floor, int len)> _branches = new();

        // ─── Public metadata ───────────────────────────────────────────────

        public int                              PathCount       => _pathCount;
        public IReadOnlyList<(int floor, int)>  Branches        => _branches;
        public int                              LastBranchFloor { get; private set; } = -1;

        // ─── Construction ──────────────────────────────────────────────────

        public PathGenerator(GridTemplate template, IRandomProvider rng)
        {
            _columns               = template.Columns;
            _floors                = template.Floors;
            _minPathCount          = template.MinPathCount;
            _maxPathCount          = template.MaxPathCount;
            _minIndependenceFloors = template.MinPathIndependenceFloors;
            _branchMinLength       = template.BranchMinLength;
            _branchMaxLength       = template.BranchMaxLength;
            _branchProbabilityPct  = Mathf.RoundToInt(template.BranchProbability * 100f);
            _branchCooldown        = template.BranchCooldown;
            _maxNodesPerFloor      = template.MaxActiveNodesPerFloor;
            _rng                   = rng;

            _activeColsPerFloor = new HashSet<int>[_floors];
            for (int f = 0; f < _floors; f++)
                _activeColsPerFloor[f] = new HashSet<int>();
        }

        // ─── Entry point ───────────────────────────────────────────────────

        public void GeneratePaths(MapData mapData)
        {
            _branches.Clear();
            _committedEdges.Clear();
            LastBranchFloor = -1;

            for (int f = 0; f < _floors; f++)
                _activeColsPerFloor[f].Clear();

            _pathCount = _rng.Range(_minPathCount, _maxPathCount + 1);

            GenerateMainPaths(mapData);
            GenerateBranches(mapData);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Map]\nMain Path Count : {_pathCount}");
#endif
        }

        // ─── Phase 1: Main Paths ───────────────────────────────────────────

        private void GenerateMainPaths(MapData mapData)
        {
            // Spread home columns evenly across the grid
            _homeCol      = new int[_pathCount];
            _mainPathCols = new int[_pathCount][];

            for (int i = 0; i < _pathCount; i++)
            {
                _homeCol[i] = _pathCount == 1
                    ? _columns / 2
                    : Mathf.RoundToInt((float)i / (_pathCount - 1) * (_columns - 1));
            }

            // Generate paths sequentially so later paths can check earlier ones
            for (int pathIdx = 0; pathIdx < _pathCount; pathIdx++)
            {
                _mainPathCols[pathIdx]    = new int[_floors];
                _mainPathCols[pathIdx][0] = _homeCol[pathIdx];
                _activeColsPerFloor[0].Add(_homeCol[pathIdx]);

                int currentCol = _homeCol[pathIdx];
                int lastDelta  = 0;

                for (int floor = 0; floor < _floors - 1; floor++)
                {
                    int nextCol = PickMainPathNext(currentCol, lastDelta, _homeCol[pathIdx], floor, pathIdx);
                    _mainPathCols[pathIdx][floor + 1] = nextCol;
                    _activeColsPerFloor[floor + 1].Add(nextCol);

                    lastDelta  = nextCol - currentCol;
                    currentCol = nextCol;
                }

                CommitMainPathEdges(mapData, pathIdx);
            }
        }

        private int PickMainPathNext(int col, int lastDelta, int homeCol, int floor, int pathIndex)
        {
            int next;

            if (floor < _minIndependenceFloors)
            {
                // Biased toward home column; stay within 1 step of home
                int towardHome = homeCol > col ? 1 : homeCol < col ? -1 : 0;
                int roll       = _rng.Range(0, 100);
                int delta;

                if (towardHome != 0)
                {
                    if (roll < 60)      delta = towardHome;   // 60 % toward home
                    else if (roll < 90) delta = 0;            // 30 % straight
                    else                delta = -towardHome;  // 10 % away
                }
                else
                {
                    // Already at home column
                    if (roll < 70)      delta = 0;   // 70 % straight
                    else if (roll < 85) delta = -1;  // 15 % left
                    else                delta = 1;   // 15 % right
                }

                next = Mathf.Clamp(col + delta, 0, _columns - 1);

                // Clamp to within 1 step of home
                if (Mathf.Abs(next - homeCol) > 1)
                    next = col;

                // Avoid occupying the same column as an already-computed earlier path
                for (int p = 0; p < pathIndex; p++)
                {
                    if (_mainPathCols[p][floor + 1] != next) continue;

                    // Conflict — try straight, then the other lateral direction
                    if (next != col)
                    {
                        next = col;
                    }
                    else
                    {
                        int alt1 = col + 1;
                        int alt2 = col - 1;
                        if (alt1 < _columns && Mathf.Abs(alt1 - homeCol) <= 1 &&
                            !IsMainPathAtExcluding(alt1, floor + 1, pathIndex))
                            next = alt1;
                        else if (alt2 >= 0 && Mathf.Abs(alt2 - homeCol) <= 1 &&
                                 !IsMainPathAtExcluding(alt2, floor + 1, pathIndex))
                            next = alt2;
                        // Rare edge case: stay and allow collision
                    }
                    break;
                }
            }
            else
            {
                // After independence period — free movement
                int roll  = _rng.Range(0, 100);
                int delta = roll < 70 ? 0 : roll < 85 ? -1 : 1;

                // No two consecutive lateral moves in the same direction
                if (delta == lastDelta && delta != 0) delta = 0;

                next = Mathf.Clamp(col + delta, 0, _columns - 1);
            }

            return next;
        }

        private void CommitMainPathEdges(MapData mapData, int pathIndex)
        {
            int[] cols = _mainPathCols[pathIndex];
            for (int floor = 0; floor < _floors - 1; floor++)
                CommitEdge(mapData, floor, cols[floor], cols[floor + 1]);
        }

        // ─── Phase 2: Branches ─────────────────────────────────────────────

        private void GenerateBranches(MapData mapData)
        {
            int[] lastBranchFloor = new int[_pathCount];
            for (int i = 0; i < _pathCount; i++)
                lastBranchFloor[i] = -(_branchCooldown + 1);

            // Ensure merge floor can fit within the grid
            int floorLimit = _floors - _branchMinLength - 2;

            for (int floor = 1; floor <= floorLimit; floor++)
            {
                for (int pathIdx = 0; pathIdx < _pathCount; pathIdx++)
                {
                    if (floor - lastBranchFloor[pathIdx] < _branchCooldown) continue;
                    if (_rng.Range(0, 100) >= _branchProbabilityPct) continue;

                    // Try preferred length, then fallback to minimum
                    int preferred = _rng.Range(_branchMinLength, _branchMaxLength + 1);
                    bool created  = TryCreateBranch(mapData, pathIdx, floor, preferred);

                    if (!created && preferred > _branchMinLength)
                        created = TryCreateBranch(mapData, pathIdx, floor, _branchMinLength);

                    if (created)
                    {
                        lastBranchFloor[pathIdx] = floor;
                        LastBranchFloor          = Mathf.Max(LastBranchFloor, floor);
                    }
                }
            }
        }

        private bool TryCreateBranch(MapData mapData, int pathIdx, int startFloor, int branchLen)
        {
            int mergeFloor = startFloor + branchLen + 1;
            if (mergeFloor >= _floors) return false;

            int mainCol  = _mainPathCols[pathIdx][startFloor];
            int mergeCol = FindNearestMainPathCol(mergeFloor, mainCol);

            if (!TryBuildBranchPath(mainCol, branchLen, mergeCol, startFloor, out List<int> branchCols))
                return false;

            // Commit branch edges and track active columns
            CommitEdge(mapData, startFloor, mainCol, branchCols[0]);
            _activeColsPerFloor[startFloor + 1].Add(branchCols[0]);

            for (int i = 1; i < branchCols.Count; i++)
            {
                CommitEdge(mapData, startFloor + i, branchCols[i - 1], branchCols[i]);
                _activeColsPerFloor[startFloor + i + 1].Add(branchCols[i]);
            }

            // Merge edge (last branch node → main path)
            CommitEdge(mapData, startFloor + branchLen, branchCols[branchCols.Count - 1], mergeCol);

            _branches.Add((startFloor, branchLen));

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Map]\nBranch Generated\nFloor : {startFloor}  Len : {branchLen}  Merge : Floor {mergeFloor} Col {mergeCol}");
#endif
            return true;
        }

        private bool TryBuildBranchPath(int mainCol, int branchLen, int mergeCol,
                                         int startFloor, out List<int> branchCols)
        {
            branchCols = null;
            var cols   = new List<int>(branchLen);

            // First branch node: adjacent to mainCol, prefer side toward mergeCol
            int prefer   = mergeCol >= mainCol ? 1 : -1;
            int[] firsts = { mainCol + prefer, mainCol - prefer };

            int bc = -1;
            foreach (int c in firsts)
            {
                if (IsValidBranchNode(c, startFloor + 1)) { bc = c; break; }
            }
            if (bc == -1) return false;
            cols.Add(bc);

            // Intermediate nodes: step toward mergeCol
            for (int step = 1; step < branchLen; step++)
            {
                int prev  = cols[step - 1];
                int floor = startFloor + step + 1;

                int toward = mergeCol > prev ? prev + 1 : mergeCol < prev ? prev - 1 : prev;
                int away   = 2 * prev - toward;
                int[] candidates = { toward, prev, away };

                int next = -1;
                foreach (int c in candidates)
                {
                    if (IsValidBranchNode(c, floor)) { next = c; break; }
                }
                if (next == -1) return false;
                cols.Add(next);
            }

            // Final check: last branch node must be adjacent to mergeCol
            if (Mathf.Abs(cols[branchLen - 1] - mergeCol) > 1) return false;

            branchCols = cols;
            return true;
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        private bool IsValidBranchNode(int col, int floor)
        {
            if (col < 0 || col >= _columns || floor >= _floors) return false;
            if (IsOnAnyMainPath(col, floor)) return false;

            // Node count guard — allow if column already active (shared), block only if truly new
            bool isNewCol = !_activeColsPerFloor[floor].Contains(col);
            if (isNewCol && _activeColsPerFloor[floor].Count >= _maxNodesPerFloor) return false;

            return true;
        }

        private bool IsOnAnyMainPath(int col, int floor)
        {
            for (int p = 0; p < _pathCount; p++)
            {
                if (_mainPathCols[p] != null && _mainPathCols[p][floor] == col)
                    return true;
            }
            return false;
        }

        private bool IsMainPathAtExcluding(int col, int floor, int excludePathIdx)
        {
            for (int p = 0; p < _pathCount; p++)
            {
                if (p == excludePathIdx) continue;
                if (_mainPathCols[p] != null && _mainPathCols[p][floor] == col) return true;
            }
            return false;
        }

        private int FindNearestMainPathCol(int floor, int referenceCol)
        {
            int bestCol  = _mainPathCols[0][floor];
            int bestDist = Mathf.Abs(referenceCol - bestCol);

            for (int p = 1; p < _pathCount; p++)
            {
                int col  = _mainPathCols[p][floor];
                int dist = Mathf.Abs(referenceCol - col);
                if (dist < bestDist) { bestDist = dist; bestCol = col; }
            }

            return bestCol;
        }

        private void CommitEdge(MapData mapData, int fromFloor, int fromCol, int toCol)
        {
            var key = (fromFloor, fromCol, fromFloor + 1, toCol);
            if (!_committedEdges.Add(key)) return; // duplicate — skip

            var conn = new MapConnection(fromFloor, fromCol, fromFloor + 1, toCol);
            mapData.Grid[fromFloor,     fromCol].AddOutgoingConnection(conn);
            mapData.Grid[fromFloor + 1, toCol  ].AddIncomingConnection(conn);
        }
    }
}
