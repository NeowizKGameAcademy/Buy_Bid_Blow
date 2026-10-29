using System;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    public sealed partial class MatchEngine
    {
        public const int ContributionLimit=5;
        DirectionLockedPricePlan contributionPlan;
        int[][] contributionNet;

        public int RoundPriceAdjustment(Company company)
        {
            int raw=contributionNet==null?0:contributionNet[Round][(int)company];
            return Math.Max(-ContributionLimit,Math.Min(ContributionLimit,raw));
        }
        // Called by a future server-side block delivery after validating the carried object.
        // No client RPC exposes this method, and salary/carrying are deliberately separate.
        public bool AddRoundPriceAdjustment(PlayerId player,Company company,int amount,double now)
        {
            if(!Accept(player,now)||!Enum.IsDefined(typeof(Company),company)||(amount!=1&&amount!=-3))return false;
            double started=Deadline-(roundSecondsOverride>0?roundSecondsOverride:Settings.GetRoundSeconds(Round));
            if(now<started)return false;
            if(contributionPlan==null)
            {
                contributionPlan=new DirectionLockedPricePlan(prices,Settings.MinPrice);
                contributionNet=new int[Settings.RoundCount+1][];
                for(int r=0;r<contributionNet.Length;r++)contributionNet[r]=new int[3];
            }
            long next=(long)contributionNet[Round][(int)company]+amount;
            if(next<int.MinValue||next>int.MaxValue)return false;
            contributionNet[Round][(int)company]=(int)next;
            contributionPlan.Apply(prices,Round,(int)company,RoundPriceAdjustment(company));
            Changed?.Invoke();return true;
        }
    }
}
