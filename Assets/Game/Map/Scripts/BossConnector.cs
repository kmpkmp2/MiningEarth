using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Connects every active node on the final floor to a single Boss node.
    ///
    /// The Boss node lives at a conceptual floor index equal to MapData.Floors
    /// (one beyond the last grid floor, corresponding to the "Floor 51" design concept).
    /// There is always exactly one Boss node per map.
    /// </summary>
    public class BossConnector
    {
        private const int BossColumn = 0;

        /// <summary>
        /// Creates the Boss node and links all active nodes on the last floor to it.
        /// Must be called after RuleValidator has finished (Step 5 in the pipeline).
        /// </summary>
        public void Connect(MapData mapData)
        {
            int lastFloor = mapData.Floors - 1;
            int bossFloor = mapData.Floors; // conceptual floor beyond the grid

            var bossNode = new MapNode(bossFloor, BossColumn);
            bossNode.RoomType = RoomType.Boss;
            bossNode.IsActive = true;

            for (int col = 0; col < mapData.Columns; col++)
            {
                MapNode node = mapData.Grid[lastFloor, col];
                if (!node.IsActive) continue;

                var conn = new MapConnection(lastFloor, col, bossFloor, BossColumn);
                node.AddOutgoingConnection(conn);
                bossNode.AddIncomingConnection(conn);
            }

            mapData.SetBossNode(bossNode);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[Map]\nBoss Node Created\nDepth : {bossNode.Floor}");
#endif
        }
    }
}
