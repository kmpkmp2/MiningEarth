namespace DeepEarth.Map
{
    /// <summary>
    /// Assigns RoomType to every active node in the grid.
    ///
    /// Order (must not be changed):
    ///   1. Assign weighted-random types to all active nodes.
    ///   2. Override fixed floors (Treasure / Rest) regardless of the random result.
    ///
    /// Rule validation is handled separately by RuleValidator (Step 5 in the pipeline).
    /// RoomType must NOT be assigned during path generation.
    /// </summary>
    public class RoomGenerator
    {
        private readonly RoomGenerationConfig _config;
        private readonly IRandomProvider      _rng;
        private readonly float                _eventWeightMultiplier;

        public RoomGenerator(RoomGenerationConfig config, IRandomProvider rng, float eventWeightMultiplier = 1f)
        {
            _config = config;
            _rng    = rng;
            _eventWeightMultiplier = eventWeightMultiplier;
        }

        /// <summary>Mutates every active node in <paramref name="mapData"/> with a RoomType.</summary>
        public void AssignRoomTypes(MapData mapData)
        {
            // Pass 1 — Weighted random for all active nodes
            for (int floor = 0; floor < mapData.Floors; floor++)
            {
                for (int col = 0; col < mapData.Columns; col++)
                {
                    MapNode node = mapData.Grid[floor, col];
                    if (!node.IsActive) continue;

                    RoomType picked = _config.PickRoomType(_rng, _eventWeightMultiplier);

                    // Rule 3: Mine cannot branch — avoid assigning Mine to a node with
                    // multiple outgoing connections. RuleValidator will also enforce this,
                    // but an early guard reduces the number of iterations required.
                    if (picked == RoomType.Mine && node.OutgoingConnections.Count > 1)
                    {
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            picked = _config.PickRoomType(_rng, _eventWeightMultiplier);
                            if (picked != RoomType.Mine) break;
                        }
                        if (picked == RoomType.Mine) picked = RoomType.Monster;
                    }

                    node.RoomType = picked;
                }
            }

            // Pass 2 — Override fixed floors
            SetFixedFloor(mapData, 0,                          RoomType.Mine);     // Start Nodes are always Mine
            SetFixedFloor(mapData, _config.TreasureFloorIndex, RoomType.Treasure);
            SetFixedFloor(mapData, _config.RestFloorIndex,     RoomType.Rest);
        }

        private static void SetFixedFloor(MapData mapData, int floorIndex, RoomType type)
        {
            if (floorIndex < 0 || floorIndex >= mapData.Floors) return;

            for (int col = 0; col < mapData.Columns; col++)
            {
                MapNode node = mapData.Grid[floorIndex, col];
                if (node.IsActive) node.RoomType = type;
            }
        }
    }
}
