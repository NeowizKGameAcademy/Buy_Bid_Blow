using System;
using System.Collections.Generic;
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
        private MatchSettings settings;

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

            CurrentRound = 0;
            ApplyPrices(scheduledPrices[CurrentRound]);
            TradeService = new LocalTradeService(GetPrice, settings.StartingCash, settings.StartingShares);
        }

        private void ApplyPrices(int[] roundPrices)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            for (int i = 0; i < companies.Length; i++)
                prices[companies[i]] = roundPrices[i];
        }
    }
}
