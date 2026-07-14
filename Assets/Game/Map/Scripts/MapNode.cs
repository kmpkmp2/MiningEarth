using System.Collections.Generic;

namespace DeepEarth.Map
{
    /// <summary>
    /// A single cell in the map grid.
    /// Holds its grid position, room type, activation state, and connection lists.
    /// </summary>
    public class MapNode
    {
        /// <summary>Zero-based floor index (row).</summary>
        public int Floor    { get; }

        /// <summary>Zero-based column index.</summary>
        public int Column   { get; }

        /// <summary>Assigned room type. Unknown until Step 4 is complete.</summary>
        public RoomType RoomType { get; set; }

        /// <summary>Whether any path passes through this node.</summary>
        public bool IsActive { get; set; }

        private readonly List<MapConnection> _incoming = new List<MapConnection>();
        private readonly List<MapConnection> _outgoing = new List<MapConnection>();

        public IReadOnlyList<MapConnection> IncomingConnections => _incoming;
        public IReadOnlyList<MapConnection> OutgoingConnections => _outgoing;

        public MapNode(int floor, int column)
        {
            Floor    = floor;
            Column   = column;
            RoomType = RoomType.Unknown;
            IsActive = false;
        }

        /// <summary>Registers an incoming connection. Silently ignores duplicates.</summary>
        public void AddIncomingConnection(MapConnection connection)
        {
            if (!_incoming.Contains(connection))
                _incoming.Add(connection);
        }

        /// <summary>Registers an outgoing connection. Silently ignores duplicates.</summary>
        public void AddOutgoingConnection(MapConnection connection)
        {
            if (!_outgoing.Contains(connection))
                _outgoing.Add(connection);
        }

        /// <summary>Removes all incoming and outgoing connections.</summary>
        public void ClearConnections()
        {
            _incoming.Clear();
            _outgoing.Clear();
        }
    }
}
