using System;

namespace MingDynasty.OpenToolkit.Simulation
{
    public readonly struct WorldCoordinate : IEquatable<WorldCoordinate>
    {
        public WorldCoordinate(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double X { get; }
        public double Z { get; }

        public bool Equals(WorldCoordinate other)
        {
            return X.Equals(other.X) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldCoordinate && Equals((WorldCoordinate)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Z.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X.ToString("0.##") + ", " + Z.ToString("0.##") + ")";
        }
    }

    public readonly struct LocalCoordinate
    {
        public LocalCoordinate(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double X { get; }
        public double Z { get; }
    }

    public readonly struct RegionCoordinate : IEquatable<RegionCoordinate>
    {
        public RegionCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }
        public int Z { get; }

        public bool Equals(RegionCoordinate other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionCoordinate && Equals((RegionCoordinate)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }
    }

    public readonly struct ChunkCoordinate : IEquatable<ChunkCoordinate>, IComparable<ChunkCoordinate>
    {
        public ChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }
        public int Z { get; }

        public bool Equals(ChunkCoordinate other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkCoordinate && Equals((ChunkCoordinate)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public int CompareTo(ChunkCoordinate other)
        {
            int x = X.CompareTo(other.X);
            return x != 0 ? x : Z.CompareTo(other.Z);
        }

        public override string ToString()
        {
            return X + "," + Z;
        }
    }

    public readonly struct WorldBounds
    {
        public WorldBounds(double minX, double minZ, double maxX, double maxZ)
        {
            if (maxX <= minX || maxZ <= minZ)
            {
                throw new ArgumentException("World bounds must have positive size.");
            }

            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public double MinX { get; }
        public double MinZ { get; }
        public double MaxX { get; }
        public double MaxZ { get; }

        public bool Contains(WorldCoordinate coordinate)
        {
            return coordinate.X >= MinX && coordinate.X <= MaxX && coordinate.Z >= MinZ && coordinate.Z <= MaxZ;
        }
    }

    public sealed class CoordinateTransform
    {
        public CoordinateTransform(WorldCoordinate origin, double regionSize, double chunkSize)
        {
            if (regionSize <= 0d || chunkSize <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(regionSize));
            }

            Origin = origin;
            RegionSize = regionSize;
            ChunkSize = chunkSize;
        }

        public WorldCoordinate Origin { get; private set; }
        public double RegionSize { get; private set; }
        public double ChunkSize { get; private set; }

        public RegionCoordinate WorldToRegion(WorldCoordinate coordinate)
        {
            return new RegionCoordinate(
                FloorToInt((coordinate.X - Origin.X) / RegionSize),
                FloorToInt((coordinate.Z - Origin.Z) / RegionSize));
        }

        public LocalCoordinate WorldToLocal(WorldCoordinate coordinate)
        {
            return new LocalCoordinate(coordinate.X - Origin.X, coordinate.Z - Origin.Z);
        }

        public WorldCoordinate LocalToWorld(LocalCoordinate coordinate)
        {
            return new WorldCoordinate(Origin.X + coordinate.X, Origin.Z + coordinate.Z);
        }

        public ChunkCoordinate WorldToChunk(WorldCoordinate coordinate)
        {
            return new ChunkCoordinate(
                FloorToInt((coordinate.X - Origin.X) / ChunkSize),
                FloorToInt((coordinate.Z - Origin.Z) / ChunkSize));
        }

        public WorldCoordinate ChunkToWorldCenter(ChunkCoordinate coordinate)
        {
            return new WorldCoordinate(
                Origin.X + (coordinate.X + 0.5d) * ChunkSize,
                Origin.Z + (coordinate.Z + 0.5d) * ChunkSize);
        }

        public WorldCoordinate ChunkLocalToWorld(ChunkCoordinate chunk, LocalCoordinate local)
        {
            return new WorldCoordinate(
                Origin.X + chunk.X * ChunkSize + local.X,
                Origin.Z + chunk.Z * ChunkSize + local.Z);
        }

        public LocalCoordinate WorldToChunkLocal(WorldCoordinate coordinate)
        {
            ChunkCoordinate chunk = WorldToChunk(coordinate);
            return new LocalCoordinate(
                coordinate.X - (Origin.X + chunk.X * ChunkSize),
                coordinate.Z - (Origin.Z + chunk.Z * ChunkSize));
        }

        public WorldBounds ChunkToWorldBounds(ChunkCoordinate coordinate)
        {
            double minX = Origin.X + coordinate.X * ChunkSize;
            double minZ = Origin.Z + coordinate.Z * ChunkSize;
            return new WorldBounds(minX, minZ, minX + ChunkSize, minZ + ChunkSize);
        }

        private static int FloorToInt(double value)
        {
            return (int)Math.Floor(value);
        }
    }
}
