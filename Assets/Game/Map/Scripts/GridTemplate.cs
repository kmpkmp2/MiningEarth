using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Abstract ScriptableObject that defines all structural and generation parameters of a map grid.
    /// Extend this class to support alternative grid shapes or act configurations.
    /// </summary>
    public abstract class GridTemplate : ScriptableObject
    {
        // ─── Grid dimensions ─────────────────────────────────────────────────

        /// <summary>Number of columns in the grid.</summary>
        public abstract int Columns { get; }

        /// <summary>Number of playable floors (rows) in the grid, excluding the boss floor.</summary>
        public abstract int Floors { get; }

        // ─── Main Path ───────────────────────────────────────────────────────

        /// <summary>Minimum number of independent main paths generated per map.</summary>
        public abstract int MinPathCount { get; }

        /// <summary>Maximum number of independent main paths generated per map.</summary>
        public abstract int MaxPathCount { get; }

        /// <summary>
        /// Number of floors during which main paths must remain independent (stay in their home zones).
        /// Paths may converge only after this many floors have passed.
        /// </summary>
        public abstract int MinPathIndependenceFloors { get; }

        // ─── Branches ────────────────────────────────────────────────────────

        /// <summary>Minimum length (in floors) of a short branch.</summary>
        public abstract int BranchMinLength { get; }

        /// <summary>Maximum length (in floors) of a short branch.</summary>
        public abstract int BranchMaxLength { get; }

        /// <summary>Probability (0–1) of a branch spawning at an eligible floor on each main path.</summary>
        public abstract float BranchProbability { get; }

        /// <summary>Minimum floors between consecutive branches on the same main path.</summary>
        public abstract int BranchCooldown { get; }

        // ─── Constraints ─────────────────────────────────────────────────────

        /// <summary>Maximum number of simultaneously active nodes (distinct columns) on any single floor.</summary>
        public abstract int MaxActiveNodesPerFloor { get; }
    }
}
