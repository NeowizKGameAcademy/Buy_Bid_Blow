using System;
using System.Text;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Match;

namespace BuyBidBlow.Dev.Editor
{
    // Preview only. Not the server's Match state or trading/betting implementation.
    public sealed class MatchPriceSimulation
    {
        public const int HistoryRoundCount = 5;
        public int Seed;
        public int[] StartingPrices;
        // Six prices at -5..0, five forward changes. Index 5 is the real match start.
        public int[][] HistoryPrices;
        public int[][] HistoryChanges;
        public PriceRoundPreview[] Rounds;

        public static MatchPriceSimulation Generate(MatchSettings settings, int seed)
        {
            settings.Validate();
            var random = new Random(seed);
            var result = new MatchPriceSimulation {
                Seed = seed, StartingPrices = settings.RollStartingPrices(random),
                Rounds = new PriceRoundPreview[settings.RoundCount]
            };
            var prices = (int[])result.StartingPrices.Clone();
            int elapsed = 0;
            for (int round = 1; round <= settings.RoundCount; round++)
            {
                int seconds = settings.GetRoundSeconds(round);
                elapsed += seconds;
                var raw = PriceChangeGenerator.Generate(settings.PriceChange, random);
                var row = new PriceRoundPreview {
                    Round = round, Seconds = seconds, ClosesAtSeconds = elapsed,
                    HasAuctions = settings.IsAuctionRound(round),
                    Prices = new int[prices.Length], RawChanges = new int[prices.Length],
                    AppliedChanges = new int[prices.Length]
                };
                for (int i = 0; i < prices.Length; i++)
                {
                    row.RawChanges[i] = raw[i].Amount;
                    row.Prices[i] = Math.Max(settings.MinPrice, prices[i] + raw[i].Amount);
                    row.AppliedChanges[i] = row.Prices[i] - prices[i];
                    prices[i] = row.Prices[i];
                }
                result.Rounds[round - 1] = row;
            }
            // Independent stream: enabling history must never consume the match's random draws.
            result.GenerateHistory(settings, new Random(unchecked(seed ^ (int)0x6D2B79F5)));
            return result;
        }

        private void GenerateHistory(MatchSettings settings, Random random)
        {
            int count = StartingPrices.Length;
            HistoryPrices = new int[HistoryRoundCount + 1][];
            HistoryChanges = new int[HistoryRoundCount][];
            for (int i = 0; i < HistoryPrices.Length; i++) HistoryPrices[i] = new int[count];
            for (int i = 0; i < HistoryChanges.Length; i++) HistoryChanges[i] = new int[count];

            for (int company = 0; company < count; company++)
            {
                bool accepted = false;
                for (int attempt = 0; attempt < 10000; attempt++)
                {
                    HistoryPrices[HistoryRoundCount][company] = StartingPrices[company];
                    bool valid = true;
                    for (int i = HistoryRoundCount - 1; i >= 0; i--)
                    {
                        int change = PriceChangeGenerator.Generate(settings.PriceChange, random)[company].Amount;
                        long previous = (long)HistoryPrices[i + 1][company] - change;
                        if (previous < settings.MinPrice || previous > int.MaxValue) { valid = false; break; }
                        HistoryPrices[i][company] = (int)previous;
                        HistoryChanges[i][company] = change;
                    }
                    if (valid) { accepted = true; break; }
                }
                if (!accepted) throw new InvalidOperationException("Could not generate valid history for company " + company + ". Try another seed or price settings.");
            }
        }

        public string ToCsv()
        {
            var csv = new StringBuilder("Seed,Phase,Round,Seconds,ClosesAtSeconds,HasAuctions,Straw,Timber,Brick,StrawRaw,TimberRaw,BrickRaw,StrawApplied,TimberApplied,BrickApplied\n");
            for (int i = 0; i <= HistoryRoundCount; i++)
            {
                // Historical indices are relative closes, not extra elapsed match time.
                csv.Append(Seed).Append(i == HistoryRoundCount ? ",Start," : ",History,")
                    .Append(i - HistoryRoundCount).Append(",0,0,false");
                foreach (int value in HistoryPrices[i]) csv.Append(',').Append(value);
                for (int repeat = 0; repeat < 2; repeat++)
                    for (int c = 0; c < StartingPrices.Length; c++)
                        csv.Append(',').Append(i == 0 ? 0 : HistoryChanges[i - 1][c]);
                csv.AppendLine();
            }
            foreach (var row in Rounds)
            {
                csv.Append(Seed).Append(",Match,").Append(row.Round).Append(',').Append(row.Seconds).Append(',')
                    .Append(row.ClosesAtSeconds).Append(',').Append(row.HasAuctions ? "true" : "false");
                foreach (int value in row.Prices) csv.Append(',').Append(value);
                foreach (int value in row.RawChanges) csv.Append(',').Append(value);
                foreach (int value in row.AppliedChanges) csv.Append(',').Append(value);
                csv.AppendLine();
            }
            return csv.ToString();
        }
    }

    public sealed class PriceRoundPreview
    {
        public int Round;
        public int Seconds;
        public int ClosesAtSeconds;
        public bool HasAuctions;
        public int[] Prices;
        public int[] RawChanges;
        public int[] AppliedChanges;
    }
}
