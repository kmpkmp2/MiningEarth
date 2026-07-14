namespace DeepEarth.Map
{
    /// <summary>
    /// Generates the initial N×M grid of MapNodes from a GridTemplate.
    /// All nodes start with IsActive=false and RoomType=Unknown.
    /// No connections are created here — that is Step 2 (PathGenerator).
    /// </summary>
    public class GridGenerator
    {
        private readonly GridTemplate _template;

        public GridGenerator(GridTemplate template)
        {
            _template = template;
        }

        /// <summary>
        /// Allocates a fresh MapData grid. Every node is inactive and untyped.
        /// </summary>
        public MapData Generate(int seed)
        {
            var mapData = new MapData(_template.Columns, _template.Floors, seed);

            for (int floor = 0; floor < _template.Floors; floor++)
            {
                for (int col = 0; col < _template.Columns; col++)
                {
                    mapData.Grid[floor, col] = new MapNode(floor, col);
                }
            }

            return mapData;
        }
    }
}
