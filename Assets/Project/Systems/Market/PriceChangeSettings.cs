using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    [Serializable]
    public sealed class PriceChangeSettings
    {
        // Shared magnitude range; the generator picks the direction.
        public IntRange Amount;

        // Indexed by Company: Straw, Timber, Brick.
        public IntRange[] AmountOffsets;

        // Chance (0..1) that the change goes up, indexed by Company.
        public float[] RiseChances;
    }
}
