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
        private readonly GridTemplate    _template;
        private readonly GridGenerator   _gridGenerator;
        private readonly PathGenerator   _pathGenerator;
        private readonly RoomGenerator   _roomGenerator;
        private readonly RuleValidator   _ruleValidator;
        private readonly BossConnector   _bossConnector;

        public MapGenerator(GridTemplate template, RoomGenerationConfig roomConfig, IRandomProvider rng)
        {
            _template      = template;
            _gridGenerator = new GridGenerator(template);
            _pathGenerator = new PathGenerator(template, rng);
            _roomGenerator = new RoomGenerator(roomConfig, rng);
            _ruleValidator = new RuleValidator(roomConfig, rng);
            _bossConnector = new BossConnector();
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

            Debug.Log($"[Map]\nGenerated: seed={seed} columns={_template.Columns} floors={_template.Floors} paths={_template.PathCount}");
            return mapData;
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
