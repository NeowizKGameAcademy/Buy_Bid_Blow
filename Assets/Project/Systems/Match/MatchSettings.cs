using System;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;

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

        // Shared range plus a per-company offset, indexed by Company: Straw, Timber, Brick.
        public IntRange StartingPrice;
        public IntRange[] StartingPriceOffsets;
        public int MinPrice;

        public PriceChangeSettings PriceChange;
        public AuctionSettings Auction;

        // Indexed by Company. Same Random as the rest of the match so one seed replays it.
        public int[] RollStartingPrices(Random random)
        {
            var prices = new int[StartingPriceOffsets.Length];
            for (int i = 0; i < prices.Length; i++)
                prices[i] = Math.Max(MinPrice, StartingPrice.Roll(random) + StartingPriceOffsets[i].Roll(random));
            return prices;
        }

        // JsonUtility leaves missing keys at zero, so catch typos here instead of mid-match.
        public void Validate()
        {
            int companyCount = Enum.GetValues(typeof(Company)).Length;
            if (RoundCount <= 0) throw new FormatException("RoundCount must be positive.");
            if (RoundSeconds <= 0) throw new FormatException("RoundSeconds must be positive.");
            if (StartingCash < 0 || StartingShares < 0) throw new FormatException("Starting cash and shares cannot be negative.");
            if (MinPrice <= 0) throw new FormatException("MinPrice must be positive.");
            if (StartingPrice == null || !StartingPrice.IsValid) throw new FormatException("StartingPrice needs Min <= Max.");
            if (StartingPrice.Min < MinPrice) throw new FormatException("StartingPrice cannot start below MinPrice.");
            ValidateOffsets(StartingPriceOffsets, companyCount, "StartingPriceOffsets");
            if (PriceChange?.Amount == null || !PriceChange.Amount.IsValid) throw new FormatException("PriceChange.Amount needs Min <= Max.");
            if (PriceChange.Amount.Min <= 0) throw new FormatException("PriceChange.Amount holds magnitudes and must be positive.");
            ValidateOffsets(PriceChange.AmountOffsets, companyCount, "PriceChange.AmountOffsets");
            if (PriceChange.RiseChances == null || PriceChange.RiseChances.Length != companyCount)
                throw new FormatException($"PriceChange.RiseChances needs {companyCount} entries in Company order.");
            foreach (float chance in PriceChange.RiseChances)
                if (chance < 0f || chance > 1f) throw new FormatException("PriceChange.RiseChances must be between 0 and 1.");
            if (Auction == null) throw new FormatException("Auction is required.");
            if (Auction.CountPerRound == null || !Auction.CountPerRound.IsValid)
                throw new FormatException("Auction.CountPerRound needs Min <= Max.");
            if (Auction.DurationSeconds <= 0 || Auction.DurationSeconds > RoundSeconds)
                throw new FormatException("Auction.DurationSeconds must fit inside the round.");
        }

        private static void ValidateOffsets(IntRange[] offsets, int companyCount, string name)
        {
            if (offsets == null || offsets.Length != companyCount)
                throw new FormatException($"{name} needs {companyCount} entries in Company order.");
            foreach (IntRange offset in offsets)
                if (offset == null || !offset.IsValid) throw new FormatException($"{name} entries need Min <= Max.");
        }
    }

    [Serializable]
    public sealed class AuctionSettings
    {
        public int DurationSeconds;
        public IntRange CountPerRound;
        public int MinGapSeconds;
    }
}
