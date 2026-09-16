using System;

namespace MingDynasty.OpenToolkit.Core
{
    /// <summary>
    /// Stable identifier for persisted domain objects. Never use a GameObject name as a domain ID.
    /// </summary>
    [Serializable]
    public readonly struct StableId : IEquatable<StableId>, IComparable<StableId>
    {
        public StableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID cannot be empty.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(Value); }
        }

        public static StableId New(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("An ID prefix cannot be empty.", nameof(prefix));
            }

            return new StableId(prefix.Trim() + "_" + Guid.NewGuid().ToString("N"));
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public bool Equals(StableId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StableId && Equals((StableId)obj);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public int CompareTo(StableId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public static bool operator ==(StableId left, StableId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StableId left, StableId right)
        {
            return !left.Equals(right);
        }
    }
}
