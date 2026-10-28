using System;

namespace BuyBidBlow.Systems.Types
{
    // Inclusive on both ends; Min == Max pins the value.
    [Serializable]
    public sealed class IntRange
    {
        public int Min;
        public int Max;

        public bool IsValid => Min <= Max;

        // 범위 안에서 정수를 무작위로 뽑음
        public int Roll(Random random) => random.Next(Min, Max + 1);
    }
}
