using System;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    // Loaded from MatchSettings.json by the Game assembly (JsonUtility), so fields stay public and enum-free.
    [Serializable]
    public sealed class MatchSettings
    {
        public int MinPlayerCount;
        public int DefaultPlayerCount;
        public int MaxPlayerCount;
        public int SharesPerCompanyPerPlayer;
        public int AdditionalSharesPerCompany;
        public int RoundCount;
        public int RoundSeconds;
        public int[] AuctionRounds;
        public int AuctionRoundSeconds;
        public int AuctionDurationSeconds;
        public int StartingCash;
        public int StartingShares;

        // Shared range plus a per-company offset, indexed by Company: Straw, Timber, Brick.
        public IntRange StartingPrice;
        public IntRange[] StartingPriceOffsets;
        public int MinPrice;

        public PriceChangeSettings PriceChange;
        public AuctionSlot[] AuctionSlots;

        // 참가 인원에 따른 회사별 초기 시장 재고를 계산
        // Initial finite supply, fixed using the participant count at match start.
        public int GetInitialCompanySupply(int playerCount)
        {
            if (playerCount < MinPlayerCount || playerCount > MaxPlayerCount)
                throw new ArgumentOutOfRangeException(nameof(playerCount));
            return checked(playerCount * SharesPerCompanyPerPlayer + AdditionalSharesPerCompany);
        }

        // 해당 라운드가 경매 라운드인지 확인
        public bool IsAuctionRound(int round)
        {
            return AuctionRounds != null && Array.IndexOf(AuctionRounds, round) >= 0;
        }

        // 라운드 길이(초)를 반환, 경매 라운드는 별도 길이
        public int GetRoundSeconds(int round)
        {
            if (round < 1 || round > RoundCount) throw new ArgumentOutOfRangeException(nameof(round));
            return IsAuctionRound(round) ? AuctionRoundSeconds : RoundSeconds;
        }

        // 회사별 시작 가격을 무작위로 정함 (최저가 이상)
        // Indexed by Company. Same Random as the rest of the match so one seed replays it.
        public int[] RollStartingPrices(Random random)
        {
            var prices = new int[StartingPriceOffsets.Length];
            for (int i = 0; i < prices.Length; i++)
                prices[i] = Math.Max(MinPrice, StartingPrice.Roll(random) + StartingPriceOffsets[i].Roll(random));
            return prices;
        }

        // 설정 값이 올바른지 검사하고 잘못되면 예외 발생
        // JsonUtility leaves missing keys at zero, so catch typos here instead of mid-match.
        public void Validate()
        {
            int companyCount = Enum.GetValues(typeof(Company)).Length;
            if (MinPlayerCount < 1 || DefaultPlayerCount < MinPlayerCount || MaxPlayerCount < DefaultPlayerCount)
                throw new FormatException("Player counts must satisfy 1 <= minimum <= default <= maximum.");
            if (SharesPerCompanyPerPlayer <= 0)
                throw new FormatException("SharesPerCompanyPerPlayer must be positive.");
            if (AdditionalSharesPerCompany < 0) throw new FormatException("AdditionalSharesPerCompany must not be negative.");
            if (RoundCount <= 0) throw new FormatException("RoundCount must be positive.");
            if (RoundSeconds <= 0) throw new FormatException("RoundSeconds must be positive.");
            if (AuctionRounds == null) throw new FormatException("AuctionRounds is required.");
            if (AuctionRoundSeconds <= 0 || AuctionDurationSeconds <= 0)
                throw new FormatException("Auction round and auction durations must be positive.");
            for (int i = 0; i < AuctionRounds.Length; i++)
                if (AuctionRounds[i] < 1 || (long)AuctionRounds[i] + LotGenerator.RoundsPerAuction - 1 > RoundCount ||
                    Array.IndexOf(AuctionRounds, AuctionRounds[i]) != i)
                    throw new FormatException("AuctionRounds must contain unique round numbers inside the match.");
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
            if (AuctionSlots == null || AuctionSlots.Length != 2) throw new FormatException("Exactly two AuctionSlots are required.");
            // In order: public, then secret. Relative to each auction round's start.
            foreach (AuctionSlot slot in AuctionSlots)
                if (slot == null || slot.StartsAtSeconds < 0 || slot.DurationSeconds <= 0 ||
                    (long)slot.StartsAtSeconds + slot.DurationSeconds > AuctionRoundSeconds)
                    throw new FormatException("Each auction slot must have positive duration and fit inside an auction round.");
            if ((long)AuctionSlots[0].StartsAtSeconds + AuctionSlots[0].DurationSeconds > AuctionSlots[1].StartsAtSeconds)
                throw new FormatException("Public and secret auction slots must not overlap.");
        }

        // 회사별 오프셋 배열의 길이와 범위를 검사
        private static void ValidateOffsets(IntRange[] offsets, int companyCount, string name)
        {
            if (offsets == null || offsets.Length != companyCount)
                throw new FormatException($"{name} needs {companyCount} entries in Company order.");
            foreach (IntRange offset in offsets)
                if (offset == null || !offset.IsValid) throw new FormatException($"{name} entries need Min <= Max.");
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
