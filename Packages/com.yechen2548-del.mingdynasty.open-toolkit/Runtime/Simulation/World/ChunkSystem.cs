using System;
using System.Collections.Generic;
using MingDynasty.OpenToolkit.Core;

namespace MingDynasty.OpenToolkit.Simulation
{
    public enum ChunkLoadState
    {
        Unloaded,
        DataOnly,
        Loaded,
        Active
    }

    public enum SimulationLevel
    {
        Full,
        Reduced,
        Statistical,
        Dormant
    }

    public sealed class ChunkData
    {
        public ChunkData(StableId id, ChunkCoordinate coordinate, WorldBounds bounds)
        {
            Id = id;
            Coordinate = coordinate;
            Bounds = bounds;
            LoadState = ChunkLoadState.DataOnly;
        }

        public StableId Id { get; private set; }
        public ChunkCoordinate Coordinate { get; private set; }
        public WorldBounds Bounds { get; private set; }
        public ChunkLoadState LoadState { get; internal set; }
    }

    public sealed class ChunkStateChangedEvent : IGameEvent
    {
        public ChunkStateChangedEvent(StableId chunkId, ChunkLoadState previous, ChunkLoadState current)
        {
            ChunkId = chunkId;
            Previous = previous;
            Current = current;
        }

        public StableId ChunkId { get; private set; }
        public ChunkLoadState Previous { get; private set; }
        public ChunkLoadState Current { get; private set; }
    }

    public interface IChunkRegistry
    {
        int Count { get; }
        void Register(ChunkData chunk);
        bool TryGet(StableId id, out ChunkData chunk);
        bool TryGet(ChunkCoordinate coordinate, out ChunkData chunk);
        IReadOnlyList<ChunkData> GetAll();
    }

    public sealed class ChunkRegistry : IChunkRegistry
    {
        private readonly Dictionary<StableId, ChunkData> byId = new Dictionary<StableId, ChunkData>();
        private readonly Dictionary<ChunkCoordinate, ChunkData> byCoordinate = new Dictionary<ChunkCoordinate, ChunkData>();

        public int Count { get { return byId.Count; } }

        public void Register(ChunkData chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (byId.ContainsKey(chunk.Id) || byCoordinate.ContainsKey(chunk.Coordinate))
            {
                throw new InvalidOperationException("Duplicate chunk ID or coordinate: " + chunk.Id + ".");
            }

            byId.Add(chunk.Id, chunk);
            byCoordinate.Add(chunk.Coordinate, chunk);
        }

        public bool TryGet(StableId id, out ChunkData chunk)
        {
            return byId.TryGetValue(id, out chunk);
        }

        public bool TryGet(ChunkCoordinate coordinate, out ChunkData chunk)
        {
            return byCoordinate.TryGetValue(coordinate, out chunk);
        }

        public IReadOnlyList<ChunkData> GetAll()
        {
            List<ChunkData> result = new List<ChunkData>(byId.Values);
            result.Sort(delegate(ChunkData left, ChunkData right)
            {
                return left.Coordinate.CompareTo(right.Coordinate);
            });
            return result;
        }
    }

    public sealed class ChunkLifecycleSettings
    {
        public int ActiveRadiusChunks = 1;
        public int LoadRadiusChunks = 2;
        public int UnloadRadiusChunks = 3;

        public void Validate()
        {
            if (ActiveRadiusChunks < 0 || LoadRadiusChunks < ActiveRadiusChunks || UnloadRadiusChunks < LoadRadiusChunks)
            {
                throw new InvalidOperationException("Chunk radii must satisfy active <= load <= unload.");
            }
        }
    }

    public interface IChunkLifecycleService
    {
        ChunkCoordinate FocusChunk { get; }
        int LoadedCount { get; }
        int ActiveCount { get; }
        void UpdateFocus(WorldCoordinate focus);
        bool SetState(StableId chunkId, ChunkLoadState state);
        bool TryGet(StableId chunkId, out ChunkData chunk);
        IReadOnlyList<ChunkData> GetAll();
    }

    public sealed class ChunkLifecycleService : IChunkLifecycleService
    {
        private readonly IChunkRegistry registry;
        private readonly CoordinateTransform coordinates;
        private readonly ChunkLifecycleSettings settings;
        private readonly IGameEventBus eventBus;
        private readonly IGameLogger logger;

        public ChunkLifecycleService(
            IChunkRegistry registry,
            CoordinateTransform coordinates,
            ChunkLifecycleSettings settings,
            IGameEventBus eventBus,
            IGameLogger logger)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.settings.Validate();
            this.eventBus = eventBus;
            this.logger = logger;
        }

        public ChunkCoordinate FocusChunk { get; private set; }

        public int LoadedCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<ChunkData> chunks = registry.GetAll();
                for (int i = 0; i < chunks.Count; i++)
                {
                    if (chunks[i].LoadState != ChunkLoadState.Unloaded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                IReadOnlyList<ChunkData> chunks = registry.GetAll();
                for (int i = 0; i < chunks.Count; i++)
                {
                    if (chunks[i].LoadState == ChunkLoadState.Active)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void UpdateFocus(WorldCoordinate focus)
        {
            FocusChunk = coordinates.WorldToChunk(focus);
            IReadOnlyList<ChunkData> chunks = registry.GetAll();
            for (int i = 0; i < chunks.Count; i++)
            {
                ChunkData chunk = chunks[i];
                int distance = Math.Max(Math.Abs(chunk.Coordinate.X - FocusChunk.X), Math.Abs(chunk.Coordinate.Z - FocusChunk.Z));
                ChunkLoadState target = chunk.LoadState;
                if (distance <= settings.ActiveRadiusChunks)
                {
                    target = ChunkLoadState.Active;
                }
                else if (distance <= settings.LoadRadiusChunks)
                {
                    target = ChunkLoadState.Loaded;
                }
                else if (distance > settings.UnloadRadiusChunks)
                {
                    target = ChunkLoadState.Unloaded;
                }

                MoveTo(chunk, target);
            }
        }

        public bool SetState(StableId chunkId, ChunkLoadState state)
        {
            ChunkData chunk;
            if (!registry.TryGet(chunkId, out chunk))
            {
                return false;
            }

            MoveTo(chunk, state);
            return true;
        }

        public bool TryGet(StableId chunkId, out ChunkData chunk)
        {
            return registry.TryGet(chunkId, out chunk);
        }

        public IReadOnlyList<ChunkData> GetAll()
        {
            return registry.GetAll();
        }

        private void MoveTo(ChunkData chunk, ChunkLoadState target)
        {
            while (chunk.LoadState != target)
            {
                ChunkLoadState previous = chunk.LoadState;
                chunk.LoadState = NextState(previous, target);
                eventBus?.Publish(new ChunkStateChangedEvent(chunk.Id, previous, chunk.LoadState));
                logger?.Info(GameLogCategory.Simulation, "Chunk " + chunk.Id + " changed state " + previous + " -> " + chunk.LoadState + ".");
            }
        }

        private static ChunkLoadState NextState(ChunkLoadState current, ChunkLoadState target)
        {
            if (current == target)
            {
                return current;
            }

            return target > current
                ? (ChunkLoadState)((int)current + 1)
                : (ChunkLoadState)((int)current - 1);
        }
    }
}
