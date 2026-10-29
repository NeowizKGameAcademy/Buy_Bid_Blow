using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.StockGacha
{
    public readonly struct StockGachaResult
    {
        public readonly bool Succeeded;
        // -1 means a blank. Company order is Straw / Timber / Brick.
        public readonly int Company, Cost, Spin, Remaining;
        public readonly PlayerId Player;
        public readonly string Message;
        public StockGachaResult(bool succeeded,PlayerId player,int company,int cost,int spin,int remaining,string message)
        {Succeeded=succeeded;Player=player;Company=company;Cost=cost;Spin=spin;Remaining=remaining;Message=message;}
    }

    // A shared finite bag, separate from the market. Neither blanks nor shares are replaced.
    public sealed class StockGachaGame
    {
        readonly IReadOnlyDictionary<PlayerId,PlayerPortfolio> players;
        readonly Dictionary<PlayerId,int> ownSpins=new Dictionary<PlayerId,int>();
        readonly Random random;
        readonly int[] shares;
        public StockGachaSettings Settings { get; }
        public int Blanks { get; private set; }
        public int InitialCount { get; }
        public int Version { get; private set; }
        public int Remaining => Blanks+shares[0]+shares[1]+shares[2];
        public bool Finished => Remaining==0;
        public StockGachaResult LastResult { get; private set; }
        public event Action<StockGachaResult> Spun;
        public int Stock(Company company)=>shares[(int)company];
        public int OwnSpins(PlayerId player)=>ownSpins.TryGetValue(player,out int count)?count:0;

        public StockGachaGame(StockGachaSettings settings,IReadOnlyDictionary<PlayerId,PlayerPortfolio> players,int seed)
        {
            settings.Validate();
            Settings=new StockGachaSettings{Price=settings.Price,Blanks=settings.Blanks,SharesPerCompany=settings.SharesPerCompany};
            this.players=players;
            Blanks=Settings.Blanks;shares=new[]{Settings.SharesPerCompany,Settings.SharesPerCompany,Settings.SharesPerCompany};
            InitialCount=Remaining;random=new Random(unchecked(seed^(int)0x4B8E1593));
        }
        StockGachaResult Reject(PlayerId player,string message)
            =>new StockGachaResult(false,player,-1,0,Version,Remaining,message);
        public StockGachaResult Spin(PlayerId player)
        {
            if(Finished)return Reject(player,"모든 캡슐이 소진되었습니다.");
            if(!players.TryGetValue(player,out var portfolio))return Reject(player,"경기 참가자가 아닙니다.");
            if(portfolio.Available<Settings.Price)return Reject(player,"입찰 예약금을 제외한 현금이 부족합니다.");
            // Complete all validations before consuming random state, cash or stock.
            for(int c=0;c<3;c++)
                if(shares[c]>0 && portfolio.GetShares((Company)c)==int.MaxValue)
                    return Reject(player,"보유 주식 한도를 초과합니다.");

            int pick=random.Next(Remaining),company=-1;
            if(pick>=Blanks)
            {
                pick-=Blanks;
                for(int c=0;c<3;c++)
                {
                    if(pick<shares[c]){company=c;break;}
                    pick-=shares[c];
                }
            }
            if(company<0)
            {
                portfolio.TrySpendAvailable(Settings.Price);
                Blanks--;
            }
            else
            {
                // Reuse atomic cash + share accounting without touching TradeService's market inventory.
                var trade=portfolio.TryBuy((Company)company,1,Settings.Price);
                if(!trade.Succeeded)throw new InvalidOperationException("Validated gacha grant failed.");
                shares[company]--;
            }
            Version++;ownSpins[player]=OwnSpins(player)+1;
            LastResult=new StockGachaResult(true,player,company,Settings.Price,Version,Remaining,
                company<0?$"꽝 · ${Settings.Price} 사용":$"{(Company)company} 1주 획득 · ${Settings.Price} 사용");
            Spun?.Invoke(LastResult);return LastResult;
        }
    }
}
