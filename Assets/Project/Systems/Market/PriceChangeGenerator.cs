using System;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    public static class PriceChangeGenerator
    {
        // 회사별 변동 폭과 방향을 무작위로 정해 가격 변동 목록을 생성
        // One change per company, indexed by Company. The caller owns the Random so one seed replays a whole match.
        public static PriceChange[] Generate(PriceChangeSettings settings, Random random)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            var changes = new PriceChange[companies.Length];

            for (int i = 0; i < companies.Length; i++)
            {
                int amount = Math.Max(1, settings.Amount.Roll(random) + settings.AmountOffsets[i].Roll(random));
                int direction = random.NextDouble() < settings.RiseChances[i] ? 1 : -1;
                changes[i] = new PriceChange(companies[i], amount * direction);
            }

            return changes;
        }
    }
}
