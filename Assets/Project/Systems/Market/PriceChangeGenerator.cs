using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    public static class PriceChangeGenerator
    {
        // One change per company, indexed by Company. The caller owns the Random so one seed replays a whole match.
        public static PriceChange[] Generate(PriceChangeSettings settings, Random random)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            int[] pool = (int[])settings.Amounts.Clone();
            var changes = new PriceChange[companies.Length];

            for (int i = 0; i < companies.Length; i++)
            {
                int amount;
                if (settings.DistinctAmounts)
                {
                    // Partial Fisher-Yates: pool[i] becomes a value no earlier company took.
                    int pick = random.Next(i, pool.Length);
                    (pool[i], pool[pick]) = (pool[pick], pool[i]);
                    amount = pool[i];
                }
                else
                {
                    amount = pool[random.Next(pool.Length)];
                }

                int direction = random.Next(2) == 0 ? -1 : 1;
                changes[i] = new PriceChange(companies[i], amount * direction);
            }

            return changes;
        }
    }
}
