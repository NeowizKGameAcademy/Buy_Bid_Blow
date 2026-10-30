using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    public static class PriceChangeGenerator
    {
        public const int HistoryRoundCount = 5;

        // Independent stream preserves the match's future prices and auction random draws.
        // Returned rows are relative closes -5..0; the last row is the actual starting price.
        public static int[][] GenerateHistory(PriceChangeSettings settings, int[] startingPrices, int minimumPrice, int seed)
        {
            if (settings == null || startingPrices == null) throw new ArgumentNullException();
            if (startingPrices.Length != Enum.GetValues(typeof(Company)).Length) throw new ArgumentException("Expected one starting price per company.");
            var random = new Random(unchecked(seed ^ (int)0x6D2B79F5));
            var history = new int[HistoryRoundCount + 1][];
            for (int i = 0; i < history.Length; i++) history[i] = new int[startingPrices.Length];
            for (int company = 0; company < startingPrices.Length; company++)
            {
                if (startingPrices[company] < minimumPrice) throw new ArgumentOutOfRangeException(nameof(startingPrices));
                bool accepted = false;
                for (int attempt = 0; attempt < 10000; attempt++)
                {
                    history[HistoryRoundCount][company] = startingPrices[company];
                    bool valid = true;
                    for (int i = HistoryRoundCount - 1; i >= 0; i--)
                    {
                        int change = Generate(settings, random)[company].Amount;
                        long previous = (long)history[i + 1][company] - change;
                        if (previous < minimumPrice || previous > int.MaxValue) { valid = false; break; }
                        history[i][company] = (int)previous;
                    }
                    if (valid) { accepted = true; break; }
                }
                if (!accepted) throw new InvalidOperationException("Could not generate valid history for company " + company + ". Try another seed or price settings.");
            }
            return history;
        }

        // One change per company, indexed by Company. The caller owns the Random so one seed replays a whole match.
        public static PriceChange[] Generate(PriceChangeSettings settings, Random random)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            var changes = new PriceChange[companies.Length];

            for (int i = 0; i < companies.Length; i++)
            {
                int amount;
                if (settings.UsesAmountBands)
                {
                    var weights = settings.CompanyWeights[i];
                    double roll = random.NextDouble();
                    int band = roll < weights.Small ? 0 : roll < weights.Small + weights.Medium ? 1 : 2;
                    amount = settings.AmountBands[band].Roll(random);
                }
                else amount = Math.Max(1, settings.Amount.Roll(random) + settings.AmountOffsets[i].Roll(random));
                int direction = random.NextDouble() < settings.RiseChances[i] ? 1 : -1;
                changes[i] = new PriceChange(companies[i], amount * direction);
            }

            return changes;
        }
    }
}
