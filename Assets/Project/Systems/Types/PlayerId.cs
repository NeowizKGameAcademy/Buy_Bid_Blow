using System;

namespace BuyBidBlow.Systems.Types
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public PlayerId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        public bool Equals(PlayerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
