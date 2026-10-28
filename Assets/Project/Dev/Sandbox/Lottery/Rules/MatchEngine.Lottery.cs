using System;
using BuyBidBlow.Systems.Lottery;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    public sealed partial class MatchEngine
    {
        public LotteryGame Lottery { get; private set; }
        double lotteryOpensAt;
        public double LotteryDeadline { get; private set; }
        double[] lotteryDeadlines;

        public void EnableLottery(LotterySettings settings,int seed,double startsAt)
        {
            if(Lottery!=null)throw new InvalidOperationException("Lottery already configured.");
            Lottery=new LotteryGame(settings,Settings.AuctionRounds,assets,seed);
            lotteryOpensAt=startsAt;
            var clock=new RoundClock(Settings,startsAt,roundSecondsOverride);
            lotteryDeadlines=new double[Settings.AuctionRounds.Length];
            for(int i=0;i<lotteryDeadlines.Length;i++)lotteryDeadlines[i]=clock.ClosesAt(Settings.AuctionRounds[i]);
            LotteryDeadline=lotteryDeadlines[0];
        }
        public string BuyLottery(PlayerId player,int draw,int quantity,double now)
        {
            if(Lottery==null || !Accept(player,now) || now<lotteryOpensAt || now>=LotteryDeadline)
                return "지금은 복권을 구매할 수 없습니다.";
            var reply=Lottery.Buy(player,draw,quantity);Changed?.Invoke();return reply;
        }
        void CloseLotteryRound(int round)
        {
            if(Lottery==null)return;
            Lottery.CloseRound(round);
            LotteryDeadline=Lottery.Finished?0:lotteryDeadlines[Lottery.DrawIndex-1];
        }
    }
}
