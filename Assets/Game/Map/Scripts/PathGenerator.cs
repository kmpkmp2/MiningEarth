using System.Collections.Generic;
using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Generates independent paths through the map grid following Slay the Spire's algorithm.
    ///
    /// Rules (applied strictly in this order):
    ///   1. Each path starts at Floor 0, random column.
    ///   2. The first two paths must start at different columns.
    ///   3. Each step moves from Floor f to Floor f+1 (column ±1 or same, clamped to grid).
    ///   4. No two path segments at the same floor may cross (form an X).
    ///   5. Multiple paths converging at the same node are allowed.
    ///   6. If all candidates are blocked, the entire current path is retried from scratch.
    ///
    /// Node activation (IsActive) is handled in Step 3 by the MapGenerator after all paths commit.
    /// </summary>
    public class PathGenerator
    {
        private const int MaxPathRetries = 1000;

        private readonly int              _columns;
        private readonly int              _floors;
        private readonly int              _pathCount;
        private readonly IRandomProvider  _rng;

        /// <summary>All edges committed from successfully generated paths so far.</summary>
        private readonly List<(int floor, int fromCol, int toCol)> _committed
            = new List<(int, int, int)>();

        public PathGenerator(GridTemplate template, IRandomProvider rng)
        {
            _columns   = template.Columns;
            _floors    = template.Floors;
            _pathCount = template.PathCount;
            _rng       = rng;
        }

        /// <summary>
        /// Generates all paths and writes their connections into <paramref name="mapData"/>.
        /// Does NOT set IsActive on nodes — that is Step 3 (MapGenerator).
        /// </summary>
        public void GeneratePaths(MapData mapData)
        {
            _committed.Clear();

            int firstStartCol = -1;

            for (int pathIndex = 0; pathIndex < _pathCount; pathIndex++)
            {
                int retries = 0;
                while (true)
                {
                    if (++retries > MaxPathRetries)
                    {
                        Debug.LogError($"[Map]\nPathGenerator: path {pathIndex} exceeded {MaxPathRetries} retries. Map seed may produce an unusable layout.");
                        break;
                    }

                    int startCol = PickStartColumn(pathIndex, firstStartCol);
                    List<(int, int, int)> edges = TryBuildPath(startCol);

                    if (edges != null)
                    {
                        if (pathIndex == 0) firstStartCol = startCol;
                        _committed.AddRange(edges);
                        CommitEdgesToMapData(mapData, edges);
                        break;
                    }
                }
            }
        }

        // ─── Start column selection ────────────────────────────────────────────

        private int PickStartColumn(int pathIndex, int firstStartCol)
        {
            if (pathIndex == 1 && firstStartCol >= 0)
            {
                // Path 1 must start at a different column than path 0.
                int col;
                do { col = _rng.Range(0, _columns); }
                while (col == firstStartCol);
                return col;
            }
            return _rng.Range(0, _columns);
        }

        // ─── Single path construction ──────────────────────────────────────────

        /// <summary>
        /// Attempts to build one path from floor 0 to floor (_floors - 1).
        /// Returns the edge list on success, or null if a floor has no valid candidates.
        /// </summary>
        private List<(int floor, int fromCol, int toCol)> TryBuildPath(int startCol)
        {
            var edges      = new List<(int, int, int)>(_floors - 1);
            int currentCol = startCol;

            for (int floor = 0; floor < _floors - 1; floor++)
            {
                List<int> candidates = BuildCandidates(currentCol);
                List<int> valid      = FilterByCrossDetection(candidates, floor, currentCol);

                if (valid.Count == 0) return null;

                int nextCol = valid[_rng.Range(0, valid.Count)];
                edges.Add((floor, currentCol, nextCol));
                currentCol = nextCol;
            }

            return edges;
        }

        // ─── Candidate construction ────────────────────────────────────────────

        private List<int> BuildCandidates(int col)
        {
            var list = new List<int>(3);
            if (col > 0)               list.Add(col - 1);
            list.Add(col);
            if (col < _columns - 1)    list.Add(col + 1);
            return list;
        }

        // ─── Cross detection ───────────────────────────────────────────────────

        /// <summary>
        /// Returns only those candidates that, if connected from (floor, fromCol),
        /// would NOT cross any already-committed edge at the same floor.
        ///
        /// Cross condition: two edges at the same floor from (a1 → a2) and (b1 → b2)
        /// cross when their from-columns and to-columns swap relative order.
        /// Algebraically: (a1 - b1) * (a2 - b2) &lt; 0.
        /// </summary>
        private List<int> FilterByCrossDetection(List<int> candidates, int floor, int fromCol)
        {
            var valid = new List<int>(candidates.Count);
            foreach (int toCol in candidates)
            {
                if (!WouldCrossAnyCommitted(floor, fromCol, toCol))
                    valid.Add(toCol);
            }
            return valid;
        }

        private bool WouldCrossAnyCommitted(int floor, int fromCol, int toCol)
        {
            foreach (var (ef, ec1, ec2) in _committed)
            {
                if (ef == floor && EdgesCross(fromCol, toCol, ec1, ec2))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true when segments (a1→a2) and (b1→b2) cross at the same floor.
        /// Identical or sharing-endpoint segments do NOT count as crossing.
        /// </summary>
        private static bool EdgesCross(int a1, int a2, int b1, int b2)
            => (a1 - b1) * (a2 - b2) < 0;

        // ─── Applying edges to MapData ─────────────────────────────────────────

        private static void CommitEdgesToMapData(
            MapData mapData,
            List<(int floor, int fromCol, int toCol)> edges)
        {
            foreach (var (floor, fromCol, toCol) in edges)
            {
                var conn = new MapConnection(floor, fromCol, floor + 1, toCol);
                mapData.Grid[floor,     fromCol].AddOutgoingConnection(conn);
                mapData.Grid[floor + 1, toCol  ].AddIncomingConnection(conn);
            }
        }
    }
}
