using UnityEngine;

namespace DeepEarth.Map
{
    /// <summary>
    /// Default 5-column × 50-floor grid template with 6 paths.
    /// Matches the Slay the Spire Act map structure scaled to 50 floors.
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultGridTemplate", menuName = "DeepEarth/Map/DefaultGridTemplate")]
    public sealed class DefaultGridTemplate : GridTemplate
    {
        [SerializeField] private int _columns   = 5;
        [SerializeField] private int _floors    = 50;
        [SerializeField] private int _pathCount = 6;

        public override int Columns   => _columns;
        public override int Floors    => _floors;
        public override int PathCount => _pathCount;
    }
}
