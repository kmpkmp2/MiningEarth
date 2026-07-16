namespace DeepEarth.Map
{
    /// <summary>
    /// Holds the fully generated map: an N×M grid of MapNodes plus the conceptual Boss node.
    /// Grid indices are zero-based: Grid[floorIndex, columnIndex].
    /// </summary>
    public class MapData
    {
        /// <summary>Number of columns in the grid.</summary>
        public int Columns { get; }

        /// <summary>Number of playable floors (rows) in the grid.</summary>
        public int Floors  { get; }

        /// <summary>Seed used to generate this map.</summary>
        public int Seed    { get; }

        /// <summary>Grid[floorIndex, columnIndex], zero-based.</summary>
        public MapNode[,] Grid { get; }

        /// <summary>The Boss node at the conceptual floor beyond the last grid floor.</summary>
        public MapNode BossNode  { get; private set; }

        /// <summary>The Mine Entrance node at the conceptual floor below floor 0.</summary>
        public MapNode StartNode { get; private set; }

        public MapData(int columns, int floors, int seed)
        {
            Columns = columns;
            Floors  = floors;
            Seed    = seed;
            Grid    = new MapNode[floors, columns];
        }

        /// <summary>Returns the node at the given zero-based indices, or null if out of range.</summary>
        public MapNode GetNode(int floor, int column)
        {
            if (floor < 0 || floor >= Floors || column < 0 || column >= Columns) return null;
            return Grid[floor, column];
        }

        /// <summary>
        /// Returns the node at the given indices, including the Boss node which lives outside the grid.
        /// Use this instead of GetNode when the caller may receive a Boss connection.
        /// </summary>
        public MapNode GetNodeIncludingBoss(int floor, int column)
        {
            if (BossNode != null && floor == BossNode.Floor && column == BossNode.Column)
                return BossNode;
            return GetNode(floor, column);
        }

        /// <summary>Assigns the boss node. Called exclusively by BossConnector.</summary>
        public void SetBossNode(MapNode bossNode) => BossNode = bossNode;

        /// <summary>Assigns the start (Mine Entrance) node. Called exclusively by EntranceConnector.</summary>
        public void SetStartNode(MapNode startNode) => StartNode = startNode;
    }
}
