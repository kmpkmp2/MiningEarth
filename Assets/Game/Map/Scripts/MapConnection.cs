namespace DeepEarth.Map
{
    /// <summary>
    /// Represents a directed edge between two nodes in the map grid.
    /// Floors and columns are zero-based indices.
    /// </summary>
    public class MapConnection
    {
        public int FromFloor  { get; }
        public int FromColumn { get; }
        public int ToFloor    { get; }
        public int ToColumn   { get; }

        public MapConnection(int fromFloor, int fromColumn, int toFloor, int toColumn)
        {
            FromFloor  = fromFloor;
            FromColumn = fromColumn;
            ToFloor    = toFloor;
            ToColumn   = toColumn;
        }
    }
}
