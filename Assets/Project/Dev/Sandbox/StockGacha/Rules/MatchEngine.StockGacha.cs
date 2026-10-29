using System;
using BuyBidBlow.Systems.StockGacha;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    public sealed partial class MatchEngine
    {
        public StockGachaGame StockGacha { get; private set; }
        double stockGachaOpensAt;
        public void EnableStockGacha(StockGachaSettings settings,int seed,double startsAt)
        {
            if(StockGacha!=null)throw new InvalidOperationException("Stock gacha already configured.");
            StockGacha=new StockGachaGame(settings,assets,seed);stockGachaOpensAt=startsAt;
        }
        public StockGachaResult SpinStockGacha(PlayerId player,double now)
        {
            if(StockGacha==null || !Accept(player,now) || now<stockGachaOpensAt)
                return new StockGachaResult(false,player,-1,0,0,StockGacha?.Remaining??0,"지금은 뽑기를 이용할 수 없습니다.");
            var result=StockGacha.Spin(player);
            if(result.Succeeded)Changed?.Invoke();
            return result;
        }
    }
}
