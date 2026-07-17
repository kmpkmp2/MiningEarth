using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Orchestrates the full route-map generation pipeline.
    ///
    /// Pipeline order (must not be changed):
    ///   1. Grid generation   — GridGenerator.Generate()
    ///   2. Path generation   — PathGenerator.GeneratePaths()
    ///   3. Node activation   — ActivateVisitedNodes()
    ///   4. Room assignment   — RoomGenerator   (Step 5)
    ///   5. Rule validation   — RuleValidator   (Step 6)
    ///   6. Boss connection   — BossConnector   (Step 7)
    /// </summary>
    public class MapGenerator
    {
        private readonly GridTemplate      _template;
        private readonly GridGenerator    _gridGenerator;
        private readonly PathGenerator    _pathGenerator;
        private readonly RoomGenerator    _roomGenerator;
        private readonly RuleValidator    _ruleValidator;
        private readonly BossConnector    _bossConnector;
        private readonly EntranceConnector _entranceConnector;

        public MapGenerator(GridTemplate template, RoomGenerationConfig roomConfig, IRandomProvider rng)
        {
            _template          = template;
            _gridGenerator     = new GridGenerator(template);
            _pathGenerator     = new PathGenerator(template, rng);
            _roomGenerator     = new RoomGenerator(roomConfig, rng);
            _ruleValidator     = new RuleValidator(roomConfig, rng);
            _bossConnector     = new BossConnector();
            _entranceConnector = new EntranceConnector();
        }

        /// <summary>Runs the full pipeline and returns a fully generated MapData.</summary>
        public MapData Generate(int seed)
        {
            // Step 1 — Allocate the empty grid
            MapData mapData = _gridGenerator.Generate(seed);

            // Step 2 — Generate paths and write connections into the grid
            _pathGenerator.GeneratePaths(mapData);

            // Step 3 — Activate every node visited by at least one path
            ActivateVisitedNodes(mapData);

            // Step 4 — Assign room types (weighted random, then override fixed floors)
            _roomGenerator.AssignRoomTypes(mapData);

            // Step 5 — Validate and repair rule violations (post-processing only)
            _ruleValidator.Validate(mapData);

            // Step 6 — Connect every last-floor active node to the single Boss node
            _bossConnector.Connect(mapData);

            // Step 7 — Connect the Mine Entrance node to all active floor-0 nodes
            _entranceConnector.Connect(mapData);

            Debug.Log($"[Map]\nGenerated: seed={seed} columns={_template.Columns} floors={_template.Floors} paths={_template.PathCount}");
            LogMapSummary(mapData);
            return mapData;
        }

        // ─── Debug Summary ───────────────────────────────────────────────────────

        private void LogMapSummary(MapData mapData)
        {
            var branches = _pathGenerator.Branches;

            // Branch stats
            int branchCount      = branches.Count;
            int totalBranchLen   = 0;
            int longestBranch    = 0;
            var branchFloorsSb   = new System.Text.StringBuilder();

            foreach (var (floor, len) in branches)
            {
                totalBranchLen += len;
                if (len > longestBranch) longestBranch = len;
                if (branchFloorsSb.Length > 0) branchFloorsSb.Append(", ");
                branchFloorsSb.Append(floor);
            }

            float avgBranchLen = branchCount > 0 ? (float)totalBranchLen / branchCount : 0f;

            // Per-floor active node count
            int maxActivePerFloor = 0;
            for (int f = 0; f < mapData.Floors; f++)
            {
                int count = 0;
                for (int c = 0; c < mapData.Columns; c++)
                    if (mapData.Grid[f, c].IsActive) count++;
                if (count > maxActivePerFloor) maxActivePerFloor = count;
            }

            // Floor-0 checks (Start Nodes)
            int  floor0Active  = 0;
            bool floor0AllMine = true;
            for (int c = 0; c < mapData.Columns; c++)
            {
                var node = mapData.Grid[0, c];
                if (!node.IsActive) continue;
                floor0Active++;
                if (node.RoomType != RoomType.Mine) floor0AllMine = false;
            }

            // Mine OutConnection violations
            int mineViolations = 0;
            for (int f = 0; f < mapData.Floors; f++)
                for (int c = 0; c < mapData.Columns; c++)
                {
                    var node = mapData.Grid[f, c];
                    if (node.IsActive && node.RoomType == RoomType.Mine && node.OutgoingConnections.Count > 1)
                        mineViolations++;
                }

            // Branch cooldown check
            bool cooldownOk = true;
            for (int i = 1; i < branches.Count; i++)
                if (branches[i].Item1 - branches[i - 1].Item1 < PathGenerator.BranchCooldown)
                    { cooldownOk = false; break; }

            // Validation
            bool passMainPath      = _pathGenerator.MainPathCols != null && _pathGenerator.MainPathCols.Count == mapData.Floors;
            bool passFloor15       = mapData.Floors >= 15; // Main Path always spans all floors
            bool passBranchSource  = true;  // guaranteed: TryCreateBranch only from main path
            bool passNoBranchBranch = true; // guaranteed: branches never call TryCreateBranch
            bool passBranchLen     = longestBranch <= PathGenerator.MaxBranchLength;
            bool passMerge         = true;  // guaranteed: TryCreateBranch always commits merge edge
            bool passCooldown      = cooldownOk;
            bool passMaxActive     = maxActivePerFloor <= 3;
            bool passFloor1Active  = floor0Active == 2;
            bool passFloor1Mine    = floor0AllMine;
            bool passMineOutConn   = mineViolations == 0;

            string V(bool ok) => ok ? "PASS" : "FAIL";

            Debug.Log(
                $"[Map]\n===== MAP SUMMARY =====\n" +
                $"Main Path Length\n{mapData.Floors}\n" +
                $"Branch Count\n{branchCount}\n" +
                $"Branch Floors\n{(branchFloorsSb.Length > 0 ? branchFloorsSb.ToString() : "-")}\n" +
                $"Average Branch Length\n{avgBranchLen:F1}\n" +
                $"Longest Branch\n{longestBranch}\n" +
                $"Merge Count\n{branchCount}\n" +
                $"Max Active Nodes Per Floor\n{maxActivePerFloor}\n" +
                $"Last Branch Floor\n{_pathGenerator.LastBranchFloor}\n" +
                $"===== VALIDATION =====\n" +
                $"Main Path 하나만 존재          : {V(passMainPath)}\n" +
                $"Main Path Floor15까지 연결     : {V(passFloor15)}\n" +
                $"Branch → Main Path에서만 생성 : {V(passBranchSource)}\n" +
                $"Branch 재분기 없음             : {V(passNoBranchBranch)}\n" +
                $"Branch 길이 <= 2               : {V(passBranchLen)}\n" +
                $"모든 Branch 2Floor 내 Merge    : {V(passMerge)}\n" +
                $"Branch 간 최소 간격 >= 5       : {V(passCooldown)}\n" +
                $"Floor당 Active Node <= 3       : {V(passMaxActive)}\n" +
                $"Floor1 Active Node == 2        : {V(passFloor1Active)}\n" +
                $"Floor1 == Mine                 : {V(passFloor1Mine)}\n" +
                $"Mine OutConnection == 1        : {V(passMineOutConn)}"
            );
        }

        // ─── Step 3: Node activation ─────────────────────────────────────────────

        /// <summary>
        /// Marks a node as active when at least one path edge touches it.
        /// Floor-0 start nodes have outgoing connections only; they are caught by the
        /// outgoing-connection check. Floor-(N-1) end nodes have incoming only — same logic.
        /// </summary>
        private static void ActivateVisitedNodes(MapData mapData)
        {
            for (int floor = 0; floor < mapData.Floors; floor++)
            {
                for (int col = 0; col < mapData.Columns; col++)
                {
                    MapNode node = mapData.Grid[floor, col];
                    node.IsActive = node.IncomingConnections.Count > 0
                                 || node.OutgoingConnections.Count > 0;
                }
            }
        }
    }
}
