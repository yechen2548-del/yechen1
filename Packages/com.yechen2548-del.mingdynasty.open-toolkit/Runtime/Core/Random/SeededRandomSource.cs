using System;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IRandomSource
    {
        int Seed { get; }
        int NextInt(int minimumInclusive, int maximumExclusive);
        float NextFloat();
    }

    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly System.Random random;

        public SeededRandomSource(int seed)
        {
            Seed = seed;
            random = new System.Random(seed);
        }

        public int Seed { get; private set; }

        public int NextInt(int minimumInclusive, int maximumExclusive)
        {
            return random.Next(minimumInclusive, maximumExclusive);
        }

        public float NextFloat()
        {
            return (float)random.NextDouble();
        }
    }
}
