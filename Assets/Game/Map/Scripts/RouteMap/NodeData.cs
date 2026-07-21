using System;

namespace DeepEarth.Map
{
    /// <summary>
    /// Immutable snapshot of a selected map node, passed from RouteMapPresenter to GameManager.
    /// Decouples the presentation layer (MapNode) from the game execution layer.
    /// </summary>
    public sealed class NodeData
    {
        public readonly string   NodeKey;     // "{floor}_{column}"
        public readonly RoomType NodeType;
        public readonly int      Floor;
        public readonly int      Column;
        public readonly int      ContentSeed; // map seed XOR-derived value for deterministic content

        public NodeData(string nodeKey, RoomType nodeType, int floor, int column, int contentSeed)
        {
            NodeKey     = nodeKey;
            NodeType    = nodeType;
            Floor       = floor;
            Column      = column;
            ContentSeed = contentSeed;
        }

        public override string ToString() => $"{NodeType} [{NodeKey}]";
    }
}
