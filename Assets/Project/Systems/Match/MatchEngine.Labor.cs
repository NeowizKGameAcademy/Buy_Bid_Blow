using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;
namespace BuyBidBlow.Systems.Match
{
    public sealed partial class MatchEngine
    {
        readonly Dictionary<PlayerId,int[]> laborCounts=new Dictionary<PlayerId,int[]>();
        public int LaborCount(PlayerId player,bool bomb)
        {
            return laborCounts.TryGetValue(player,out var counts)&&counts[0]==Round?counts[bomb?2:1]:0;
        }
        public bool DeliverLabor(PlayerId player,Company company,bool bomb,double now,out int wage)
        {
            wage=0;
            if(!Accept(player,now)||!Enum.IsDefined(typeof(Company),company)||LaborCount(player,bomb)>=(bomb?1:5))return false;
            int pay=bomb?0:Math.Max(1,Price(company)/5);
            if((long)assets[player].Cash+pay>int.MaxValue)return false;
            if(!AddRoundPriceAdjustment(player,company,bomb?-3:1,now))return false;
            if(!laborCounts.TryGetValue(player,out var counts)||counts[0]!=Round)
                laborCounts[player]=counts=new[]{Round,0,0};
            counts[bomb?2:1]++;assets[player].Credit(pay);wage=pay;
            Changed?.Invoke();return true;
        }
    }
}
