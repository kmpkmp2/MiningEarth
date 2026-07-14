namespace DeepEarth.Map
{
    /// <summary>
    /// Abstraction over random number generation.
    /// Allows both seeded (deterministic) and Unity-backed implementations.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns a random integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Returns a random float in [min, max).</summary>
        float Range(float min, float max);

        /// <summary>Returns a random float in [0, 1).</summary>
        float Value { get; }
    }

    /// <summary>
    /// Deterministic random provider backed by System.Random.
    /// Identical seeds always produce identical sequences.
    /// </summary>
    public sealed class SeededRandomProvider : IRandomProvider
    {
        private readonly System.Random _rng;

        public SeededRandomProvider(int seed)
        {
            _rng = new System.Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
            => _rng.Next(minInclusive, maxExclusive);

        public float Range(float min, float max)
            => (float)(_rng.NextDouble() * ((double)max - min) + min);

        public float Value
            => (float)_rng.NextDouble();
    }
}
