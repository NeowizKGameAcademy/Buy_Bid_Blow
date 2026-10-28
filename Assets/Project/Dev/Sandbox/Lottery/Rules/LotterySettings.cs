using System;

namespace BuyBidBlow.Systems.Lottery
{
    [Serializable]
    public sealed class LotterySettings
    {
        public int TicketCount = 100;
        public int TicketPrice = 5;
        public int PerPlayerLimit = 10;
        public int InitialBasePrize = 30;
        public int BasePrizeIncrement = 20;

        public void Validate(int drawCount)
        {
            if (TicketCount < 1 || TicketPrice < 1 || PerPlayerLimit < 1 || PerPlayerLimit > TicketCount ||
                InitialBasePrize < 0 || BasePrizeIncrement < 0 || drawCount < 1)
                throw new ArgumentException("Invalid lottery settings.");
            // Even an all-rollover match must fit the portfolio's integer currency.
            checked
            {
                long maximum = (long)drawCount * TicketCount * TicketPrice + (long)drawCount * InitialBasePrize +
                    (long)BasePrizeIncrement * drawCount * (drawCount - 1) / 2;
                if (maximum > int.MaxValue) throw new ArgumentException("Lottery prize exceeds currency limit.");
            }
        }
    }
}
