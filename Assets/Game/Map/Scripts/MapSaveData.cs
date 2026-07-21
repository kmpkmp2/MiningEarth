using System;
using System.Collections.Generic;

namespace DeepEarth.Map
{
    /// <summary>
    /// Serialisable payload for an in-progress route map run.
    /// MapData is fully regenerated from Seed on load; only node states are persisted.
    /// </summary>
    [Serializable]
    public class MapSaveData
    {
        public bool HasActiveMap;
        public int  MapIndex;
        public int  GlobalDepth;
        public int  Seed;

        // Node state persistence — keys are "{floor}_{column}"
        public List<string> CompletedNodeKeys  = new List<string>();
        public List<string> AccessibleNodeKeys = new List<string>();

        // Key of the node currently being played (set on select, cleared on complete).
        // Used to resume from mid-node on app restart.
        public string CurrentNodeKey = "";

        // Key of the last completed node — used to render the "current position" ring.
        // Empty = Mine Entrance is current position.
        public string CurrentPositionKey = "";

        // Floor of the last completed node. -1 = no node completed yet.
        // Nodes at Floor <= CurrentFloor are non-selectable (floor restriction).
        public int CurrentFloor = -1;
    }
}
