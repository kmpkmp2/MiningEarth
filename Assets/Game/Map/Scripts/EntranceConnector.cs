using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Creates the Mine Entrance (Start) node and links it to all active Floor 0 nodes.
    ///
    /// Mirrors BossConnector which connects the last floor to a Boss node at the top.
    /// The Start node lives at a conceptual floor index of -1 (below the grid),
    /// representing the player's entry point into each 50-floor mining zone.
    /// </summary>
    public class EntranceConnector
    {
        private const int EntranceFloor  = -1;
        private const int EntranceColumn =  0; // View가 항상 가로 중앙에 렌더링

        /// <summary>
        /// Creates the Mine Entrance node and connects it to all active Floor 0 nodes.
        /// Must be called after BossConnector (Step 6) so floor-0 active states are final.
        /// </summary>
        public void Connect(MapData mapData)
        {
            var startNode = new MapNode(EntranceFloor, EntranceColumn);
            startNode.RoomType = RoomType.Start;
            startNode.IsActive = true;

            for (int col = 0; col < mapData.Columns; col++)
            {
                MapNode node = mapData.Grid[0, col];
                if (!node.IsActive) continue;

                var conn = new MapConnection(EntranceFloor, EntranceColumn, 0, col);
                startNode.AddOutgoingConnection(conn);
                node.AddIncomingConnection(conn);
            }

            mapData.SetStartNode(startNode);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[Map]\nMine Entrance Created");
#endif
        }
    }
}
