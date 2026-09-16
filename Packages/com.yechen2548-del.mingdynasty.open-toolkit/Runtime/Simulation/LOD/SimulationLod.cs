using System;
using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;

namespace MingDynasty.OpenToolkit.Simulation
{
    public sealed class SimulationLodPolicy
    {
        public SimulationLodPolicy(float fullRadius, float reducedRadius, float statisticalRadius)
        {
            if (fullRadius < 0f || reducedRadius < fullRadius || statisticalRadius < reducedRadius) throw new ArgumentException("Simulation LOD radii must be ordered and non-negative.");
            FullRadius = fullRadius;
            ReducedRadius = reducedRadius;
            StatisticalRadius = statisticalRadius;
        }

        public float FullRadius { get; private set; }
        public float ReducedRadius { get; private set; }
        public float StatisticalRadius { get; private set; }

        public SimulationLevel Resolve(float distanceToFocus, bool chunkActive)
        {
            if (!chunkActive || distanceToFocus < 0f) return SimulationLevel.Dormant;
            if (distanceToFocus <= FullRadius) return SimulationLevel.Full;
            if (distanceToFocus <= ReducedRadius) return SimulationLevel.Reduced;
            if (distanceToFocus <= StatisticalRadius) return SimulationLevel.Statistical;
            return SimulationLevel.Dormant;
        }

        public double TickIntervalHours(SimulationLevel level)
        {
            switch (level)
            {
                case SimulationLevel.Full: return 1d / 12d;
                case SimulationLevel.Reduced: return 0.5d;
                case SimulationLevel.Statistical: return 6d;
                default: return double.PositiveInfinity;
            }
        }

        public int EntityBudget(SimulationLevel level)
        {
            switch (level)
            {
                case SimulationLevel.Full: return 64;
                case SimulationLevel.Reduced: return 24;
                case SimulationLevel.Statistical: return 1;
                default: return 0;
            }
        }
    }

    public sealed class ChunkWorkScheduler
    {
        private int cursor;

        public int Cursor { get { return cursor; } }

        public IReadOnlyList<StableId> Take(IReadOnlyList<StableId> sortedEntityIds, int budget)
        {
            List<StableId> result = new List<StableId>();
            if (sortedEntityIds == null || sortedEntityIds.Count == 0 || budget <= 0) return result;
            if (cursor >= sortedEntityIds.Count) cursor = 0;
            int count = Math.Min(budget, sortedEntityIds.Count);
            for (int i = 0; i < count; i++) result.Add(sortedEntityIds[(cursor + i) % sortedEntityIds.Count]);
            cursor = (cursor + count) % sortedEntityIds.Count;
            return result;
        }

        public void Reset() { cursor = 0; }
    }

    public sealed class ChunkSimulationBudget
    {
        public ChunkSimulationBudget(ChunkCoordinate coordinate)
        {
            Coordinate = coordinate;
            Level = SimulationLevel.Dormant;
        }

        public ChunkCoordinate Coordinate { get; private set; }
        public SimulationLevel Level { get; private set; }
        public int FullEntityCount { get; private set; }
        public int ReducedEntityCount { get; private set; }
        public int StatisticalPopulation { get; private set; }
        public double LastProcessedHour { get; private set; }

        public void Apply(SimulationLevel level, int fullEntityCount, int reducedEntityCount, int statisticalPopulation, double gameHours)
        {
            Level = level;
            FullEntityCount = Math.Max(0, fullEntityCount);
            ReducedEntityCount = Math.Max(0, reducedEntityCount);
            StatisticalPopulation = Math.Max(0, statisticalPopulation);
            LastProcessedHour = Math.Max(0d, gameHours);
        }
    }
}
