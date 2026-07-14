using System.Text;
using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Editor-side MonoBehaviour for generating and visualising the route map.
    /// Attach to any GameObject in a debug scene.
    /// Use the custom Inspector buttons (MapDebuggerEditor) to trigger generation.
    /// </summary>
    public class MapDebugger : MonoBehaviour
    {
        [Header("생성 설정")]
        [SerializeField] private GridTemplate         gridTemplate;
        [SerializeField] private RoomGenerationConfig roomConfig;
        [SerializeField] private int                  seed = 42;

        [Header("Gizmo 설정")]
        [SerializeField] private float nodeRadius   = 0.4f;
        [SerializeField] private float colSpacing   = 1.5f;
        [SerializeField] private float floorSpacing = 1.2f;

        private MapData _lastGenerated;

        // ─── Public API (called by MapDebuggerEditor) ────────────────────────

        public void Generate()
        {
            if (gridTemplate == null)
            {
                Debug.LogError("[Map]\nMapDebugger: GridTemplate is not assigned");
                return;
            }
            if (roomConfig == null)
            {
                Debug.LogError("[Map]\nMapDebugger: RoomGenerationConfig is not assigned");
                return;
            }

            var rng       = new SeededRandomProvider(seed);
            var generator = new MapGenerator(gridTemplate, roomConfig, rng);
            _lastGenerated = generator.Generate(seed);

            LogToConsole(_lastGenerated);
        }

        public void RandomizeSeed()
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        public int Seed => seed;

        // ─── Console output ───────────────────────────────────────────────────

        private static void LogToConsole(MapData mapData)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[Map] Seed={mapData.Seed}  Columns={mapData.Columns}  Floors={mapData.Floors}");
            sb.AppendLine("──────────────────────────────────────────────────");

            for (int floor = 0; floor < mapData.Floors; floor++)
            {
                bool hasActive = false;
                var line = new StringBuilder();
                line.Append($"  Floor {floor + 1,2} │ ");

                for (int col = 0; col < mapData.Columns; col++)
                {
                    MapNode node = mapData.Grid[floor, col];
                    if (!node.IsActive)
                    {
                        line.Append("──────────── ");
                        continue;
                    }

                    hasActive = true;
                    var outCols = new StringBuilder();
                    foreach (var conn in node.OutgoingConnections)
                        outCols.Append($"c{conn.ToColumn} ");

                    line.Append($"[{node.RoomType,-8}→{outCols.ToString().TrimEnd()}] ");
                }

                if (hasActive) sb.AppendLine(line.ToString());
            }

            if (mapData.BossNode != null)
            {
                sb.AppendLine("──────────────────────────────────────────────────");
                var incoming = new StringBuilder();
                foreach (var conn in mapData.BossNode.IncomingConnections)
                    incoming.Append($"F{conn.FromFloor + 1}C{conn.FromColumn} ");
                sb.AppendLine($"  BOSS ← [{incoming.ToString().TrimEnd()}]");
            }

            Debug.Log(sb.ToString());
        }

        // ─── Gizmo rendering ──────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (_lastGenerated == null) return;

            for (int floor = 0; floor < _lastGenerated.Floors; floor++)
            {
                for (int col = 0; col < _lastGenerated.Columns; col++)
                {
                    MapNode node = _lastGenerated.Grid[floor, col];
                    if (!node.IsActive) continue;

                    Vector3 pos = WorldPos(floor, col);

                    Gizmos.color = RoomColor(node.RoomType);
                    Gizmos.DrawSphere(pos, nodeRadius * 0.5f);

                    Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                    foreach (var conn in node.OutgoingConnections)
                    {
                        Vector3 toPos = WorldPos(conn.ToFloor, conn.ToColumn);
                        Gizmos.DrawLine(pos, toPos);
                    }
                }
            }

            DrawBossNode();
        }

        private void DrawBossNode()
        {
            MapNode boss = _lastGenerated?.BossNode;
            if (boss == null) return;

            Vector3 bossPos = WorldPos(boss.Floor, boss.Column);

            Gizmos.color = RoomColor(RoomType.Boss);
            Gizmos.DrawSphere(bossPos, nodeRadius * 0.8f);

            Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
            foreach (var conn in boss.IncomingConnections)
            {
                Vector3 fromPos = WorldPos(conn.FromFloor, conn.FromColumn);
                Gizmos.DrawLine(fromPos, bossPos);
            }
        }

        private Vector3 WorldPos(int floor, int col)
        {
            float halfW = (_lastGenerated.Columns - 1) * colSpacing * 0.5f;
            float x     = col * colSpacing - halfW;
            float y     = floor * floorSpacing;
            return transform.position + new Vector3(x, y, 0f);
        }

        private static Color RoomColor(RoomType type) => type switch
        {
            RoomType.Mine     => Color.white,
            RoomType.Monster  => Color.gray,
            RoomType.Elite    => Color.red,
            RoomType.Event    => Color.yellow,
            RoomType.Merchant => Color.blue,
            RoomType.Rest     => Color.green,
            RoomType.Treasure => Color.cyan,
            RoomType.Boss     => Color.magenta,
            _                 => Color.white,
        };
    }
}
