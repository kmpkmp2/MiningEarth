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
    }
}
