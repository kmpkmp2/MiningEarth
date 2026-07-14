using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Abstract ScriptableObject that defines the structural parameters of a map grid.
    /// Extend this class to support alternative grid shapes or act configurations.
    /// </summary>
    public abstract class GridTemplate : ScriptableObject
    {
        /// <summary>Number of columns in the grid.</summary>
        public abstract int Columns   { get; }

        /// <summary>Number of playable floors (rows) in the grid, excluding the boss floor.</summary>
        public abstract int Floors    { get; }

        /// <summary>Total number of independent paths to generate through the grid.</summary>
        public abstract int PathCount { get; }
    }
}
