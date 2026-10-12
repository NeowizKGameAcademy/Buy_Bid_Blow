using System;

namespace BuyBidBlow.Systems.Market
{
    [Serializable]
    public sealed class PriceChangeSettings
    {
        // Magnitudes only; the generator picks the direction.
        public int[] Amounts;
        public bool DistinctAmounts;
    }
}
