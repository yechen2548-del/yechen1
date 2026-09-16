using System;
using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;

namespace MingDynasty.OpenToolkit.Simulation
{
    public enum SimulationTickKind
    {
        Fast,
        Normal,
        Slow
    }

    [Serializable]
    public sealed class SimulationTickSettings
    {
        public double FastIntervalHours = 1d;
        public double NormalIntervalHours = 6d;
        public double SlowIntervalHours = 24d;
        public int MaxTicksPerAdvance = 32;

        public void Validate()
        {
            if (FastIntervalHours <= 0d || NormalIntervalHours <= 0d || SlowIntervalHours <= 0d || MaxTicksPerAdvance <= 0)
            {
                throw new InvalidOperationException("Simulation tick intervals and limits must be positive.");
            }
        }
    }

    public sealed class SimulationTickEvent : IGameEvent
    {
        public SimulationTickEvent(SimulationTickKind kind, long tickIndex, GameTime time)
        {
            Kind = kind;
            TickIndex = tickIndex;
            Time = time;
        }

        public SimulationTickKind Kind { get; private set; }
        public long TickIndex { get; private set; }
        public GameTime Time { get; private set; }
    }

    public interface ISimulationTickService
    {
        long GetTickCount(SimulationTickKind kind);
        void Advance(double gameHours, GameTime currentTime);
    }

    public sealed class SimulationTickService : ISimulationTickService
    {
        private readonly SimulationTickSettings settings;
        private readonly IGameEventBus eventBus;
        private readonly IGameLogger logger;
        private readonly Dictionary<SimulationTickKind, double> accumulators = new Dictionary<SimulationTickKind, double>();
        private readonly Dictionary<SimulationTickKind, long> counts = new Dictionary<SimulationTickKind, long>();

        public SimulationTickService(SimulationTickSettings settings, IGameEventBus eventBus, IGameLogger logger)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.settings.Validate();
            this.eventBus = eventBus;
            this.logger = logger;
            foreach (SimulationTickKind kind in Enum.GetValues(typeof(SimulationTickKind)))
            {
                accumulators.Add(kind, 0d);
                counts.Add(kind, 0L);
            }
        }

        public long GetTickCount(SimulationTickKind kind)
        {
            return counts[kind];
        }

        public void Advance(double gameHours, GameTime currentTime)
        {
            if (gameHours < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(gameHours));
            }

            foreach (SimulationTickKind kind in Enum.GetValues(typeof(SimulationTickKind)))
            {
                double interval = IntervalFor(kind);
                double accumulator = accumulators[kind] + gameHours;
                int emitted = 0;
                while (accumulator >= interval && emitted < settings.MaxTicksPerAdvance)
                {
                    accumulator -= interval;
                    counts[kind]++;
                    emitted++;
                    eventBus?.Publish(new SimulationTickEvent(kind, counts[kind], currentTime));
                }

                accumulators[kind] = accumulator;
                if (emitted == settings.MaxTicksPerAdvance && accumulator >= interval)
                {
                    logger?.Warning(GameLogCategory.Simulation, "Simulation tick catch-up was capped for " + kind + ".");
                }
            }
        }

        private double IntervalFor(SimulationTickKind kind)
        {
            switch (kind)
            {
                case SimulationTickKind.Fast:
                    return settings.FastIntervalHours;
                case SimulationTickKind.Normal:
                    return settings.NormalIntervalHours;
                default:
                    return settings.SlowIntervalHours;
            }
        }
    }

    public interface ISimulationLodService
    {
        SimulationLevel GetLevel(StableId chunkId);
        void Refresh(IReadOnlyList<ChunkData> chunks);
    }

    public sealed class SimulationLodService : ISimulationLodService
    {
        private readonly Dictionary<StableId, SimulationLevel> levels = new Dictionary<StableId, SimulationLevel>();

        public SimulationLevel GetLevel(StableId chunkId)
        {
            SimulationLevel level;
            return levels.TryGetValue(chunkId, out level) ? level : SimulationLevel.Dormant;
        }

        public void Refresh(IReadOnlyList<ChunkData> chunks)
        {
            levels.Clear();
            for (int i = 0; i < chunks.Count; i++)
            {
                ChunkData chunk = chunks[i];
                levels[chunk.Id] = LevelFor(chunk.LoadState);
            }
        }

        private static SimulationLevel LevelFor(ChunkLoadState state)
        {
            switch (state)
            {
                case ChunkLoadState.Active:
                    return SimulationLevel.Full;
                case ChunkLoadState.Loaded:
                    return SimulationLevel.Reduced;
                case ChunkLoadState.DataOnly:
                    return SimulationLevel.Statistical;
                default:
                    return SimulationLevel.Dormant;
            }
        }
    }
}
