using System;
using BuyBidBlow.Systems.Market;

namespace BuyBidBlow.Systems.Match
{
    // Loaded from MatchSettings.json by the Game assembly (JsonUtility), so fields stay public and enum-free.
    [Serializable]
    public sealed class MatchSettings
    {
        public int RoundCount;
        public int RoundSeconds;
        public int StartingCash;
        public int StartingShares;

        // Indexed by Company: Straw, Timber, Brick.
        public int[] StartingPrices;
        public int MinPrice;

        public PriceChangeSettings PriceChange;
        public AuctionSlot[] AuctionSlots;

        // JsonUtility leaves missing keys at zero, so catch typos here instead of mid-match.
        public void Validate()
        {
            int companyCount = Enum.GetValues(typeof(Types.Company)).Length;
            if (RoundCount <= 0) throw new FormatException("RoundCount must be positive.");
            if (RoundSeconds <= 0) throw new FormatException("RoundSeconds must be positive.");
            if (StartingCash < 0 || StartingShares < 0) throw new FormatException("Starting cash and shares cannot be negative.");
            if (MinPrice <= 0) throw new FormatException("MinPrice must be positive.");
            if (StartingPrices == null || StartingPrices.Length != companyCount)
                throw new FormatException($"StartingPrices needs {companyCount} entries in Company order.");
            foreach (int price in StartingPrices)
                if (price < MinPrice) throw new FormatException("StartingPrices cannot be below MinPrice.");
            if (PriceChange?.Amounts == null || PriceChange.Amounts.Length == 0)
                throw new FormatException("PriceChange.Amounts is required.");
            foreach (int amount in PriceChange.Amounts)
                if (amount <= 0) throw new FormatException("PriceChange.Amounts holds magnitudes and must be positive.");
            if (PriceChange.DistinctAmounts && PriceChange.Amounts.Length < companyCount)
                throw new FormatException("DistinctAmounts needs at least one amount per company.");
            if (AuctionSlots == null) throw new FormatException("AuctionSlots is required.");
            foreach (AuctionSlot slot in AuctionSlots)
                if (slot.StartsAtSeconds < 0 || slot.DurationSeconds <= 0 || slot.StartsAtSeconds + slot.DurationSeconds > RoundSeconds)
                    throw new FormatException("Each auction slot must fit inside the round.");
        }
    }

    [Serializable]
    public sealed class AuctionSlot
    {
        // Elapsed seconds from round start.
        public int StartsAtSeconds;
        public int DurationSeconds;
    }
}
