using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Default 5-column × 50-floor grid template.
    /// Generates 2-3 independent Main Paths with Short Branches (length 2-4) merging back to main paths.
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultGridTemplate", menuName = "DeepEarth/Map/DefaultGridTemplate")]
    public sealed class DefaultGridTemplate : GridTemplate
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int _columns = 5;
        [SerializeField] private int _floors  = 50;

        [Header("Main Paths")]
        [SerializeField] private int _minPathCount              = 2;
        [SerializeField] private int _maxPathCount              = 3;
        [SerializeField] private int _minPathIndependenceFloors = 10;

        [Header("Branches")]
        [SerializeField] private int   _branchMinLength   = 2;
        [SerializeField] private int   _branchMaxLength   = 4;
        [SerializeField] [Range(0f, 1f)] private float _branchProbability = 0.25f;
        [SerializeField] private int   _branchCooldown    = 5;

        [Header("Constraints")]
        [SerializeField] private int _maxActiveNodesPerFloor = 3;

        // ─── GridTemplate overrides ───────────────────────────────────────────

        public override int   Columns                   => _columns;
        public override int   Floors                    => _floors;
        public override int   MinPathCount              => _minPathCount;
        public override int   MaxPathCount              => _maxPathCount;
        public override int   MinPathIndependenceFloors => _minPathIndependenceFloors;
        public override int   BranchMinLength           => _branchMinLength;
        public override int   BranchMaxLength           => _branchMaxLength;
        public override float BranchProbability         => _branchProbability;
        public override int   BranchCooldown            => _branchCooldown;
        public override int   MaxActiveNodesPerFloor    => _maxActiveNodesPerFloor;
    }
}
