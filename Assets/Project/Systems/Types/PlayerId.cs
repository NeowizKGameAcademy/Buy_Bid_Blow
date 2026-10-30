using System;

namespace BuyBidBlow.Systems.Types
{
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        // ID 문자열을 설정 (null이면 빈 문자열)
        public PlayerId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        // ID 문자열이 같은지 비교
        public bool Equals(PlayerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        // PlayerId이고 ID가 같은지 비교
        public override bool Equals(object obj)
        {
            return obj is PlayerId other && Equals(other);
        }

        // ID 문자열 기반 해시 코드를 반환
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        // ID 문자열을 반환
        public override string ToString()
        {
            return Value;
        }
    }
}
