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

        public int Roll(Random random) => random.Next(Min, Max + 1);
    }
}
