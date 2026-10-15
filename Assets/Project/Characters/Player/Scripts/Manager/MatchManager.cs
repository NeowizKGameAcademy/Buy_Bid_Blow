using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems;
using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow
{
    public class MatchManager : MonoBehaviour
    {
        [SerializeField] private TextAsset matchSettingsFile;
        [SerializeField] private int randomSeed;

        private readonly Dictionary<Company, int> prices = new Dictionary<Company, int>();
        private readonly List<PriceChange[]> scheduledPriceChanges = new List<PriceChange[]>();
        private readonly List<int[]> scheduledPrices = new List<int[]>();
        // Every participant's cash, reservations and shares. Trading and auctions share these same
        // objects, so a bid's reserved cash can't be spent on stock. Moves into Match on the server later.
        private readonly Dictionary<PlayerId, PlayerAssetState> playerAssets = new Dictionary<PlayerId, PlayerAssetState>();
        private MatchSettings settings;
        private LotGenerator lotGenerator;

        // Minimum bid is still undecided in the rules; $1 until it is.
        private const int MinBid = 1;

        public int CurrentRound { get; private set; }
        public bool IsMatchComplete => settings != null && CurrentRound >= settings.RoundCount;
        public ITradeService TradeService { get; private set; }

        public event Action<int, IReadOnlyList<PriceChange>> RoundAdvanced;

        public static MatchManager instance = null;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            InitializeMatch();
        }

        public int GetPrice(Company company)
        {
            if (!prices.TryGetValue(company, out int price))
                throw new InvalidOperationException($"No price has been initialized for {company}.");

            return price;
        }

        public int GetPriceForRound(Company company, int round)
        {
            if (round < 0 || round >= scheduledPrices.Count)
                throw new ArgumentOutOfRangeException(nameof(round));

            return scheduledPrices[round][(int)company];
        }

        public IReadOnlyList<PriceChange> GetChangesForRound(int round)
        {
            if (round < 1 || round > scheduledPriceChanges.Count)
                throw new ArgumentOutOfRangeException(nameof(round));

            return scheduledPriceChanges[round - 1];
        }

        // Registers a participant with the starting assets. Calling again for the same player keeps their assets.
        // Locally the player's own controller calls this; later the lobby does it when the match starts.
        public void AddPlayer(PlayerId playerId)
        {
            if (settings == null)
                throw new InvalidOperationException("MatchManager is not initialized.");

            if (!playerAssets.ContainsKey(playerId))
                playerAssets.Add(playerId, new PlayerAssetState(settings.StartingCash, settings.StartingShares));
        }

        // Lots come out in auction order: 1st (Open) then 2nd (Secret) of each auction round.
        public Lot NextLot() => lotGenerator.Next();

        // Call at the start of an auction round, then feed it Tick(now) and player requests.
        // Bids reserve cash on the same PlayerAssetState objects that trading uses.
        public AuctionRound CreateAuctionRound(double roundStart)
        {
            if (settings == null)
                throw new InvalidOperationException("MatchManager is not initialized.");

            Lot first = NextLot();
            Lot second = NextLot();
            return new AuctionRound(first, second, roundStart, settings.AuctionSlots, MinBid, playerAssets);
        }

        public void AdvanceRound()
        {
            if (settings == null || IsMatchComplete)
                return;

            CurrentRound++;

            PriceChange[] changes = scheduledPriceChanges[CurrentRound - 1];
            ApplyPrices(scheduledPrices[CurrentRound]);
            RoundAdvanced?.Invoke(CurrentRound, changes);
        }

        private void InitializeMatch()
        {
            if (matchSettingsFile == null)
            {
                Debug.LogError("MatchManager needs a MatchSettings JSON file.", this);
                enabled = false;
                return;
            }

            settings = JsonUtility.FromJson<MatchSettings>(matchSettingsFile.text);
            settings.Validate();

            int seed = randomSeed != 0 ? randomSeed : Environment.TickCount;
            var random = new System.Random(seed);

            int[] startingPrices = settings.RollStartingPrices(random);
            scheduledPriceChanges.Clear();
            scheduledPrices.Clear();
            scheduledPrices.Add((int[])startingPrices.Clone());

            var nextPrices = (int[])startingPrices.Clone();
            for (int round = 0; round < settings.RoundCount; round++)
            {
                PriceChange[] changes = PriceChangeGenerator.Generate(settings.PriceChange, random);
                scheduledPriceChanges.Add(changes);

                foreach (PriceChange change in changes)
                    nextPrices[(int)change.Company] = Math.Max(
                        settings.MinPrice,
                        nextPrices[(int)change.Company] + change.Amount);

                scheduledPrices.Add((int[])nextPrices.Clone());
            }

            lotGenerator = new LotGenerator(settings.AuctionRounds, scheduledPrices, random);

            CurrentRound = 0;
            ApplyPrices(scheduledPrices[CurrentRound]);
            playerAssets.Clear();
            TradeService = new LocalTradeService(playerAssets, GetPrice);
        }

        private void ApplyPrices(int[] roundPrices)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            for (int i = 0; i < companies.Length; i++)
                prices[companies[i]] = roundPrices[i];
        }
    }
}
