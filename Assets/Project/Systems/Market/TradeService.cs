using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Market
{
    // Buys and sells stock for match players. The match owns the portfolios and shares them
    // with trading and auctions, so cash reserved by a bid is never spent on stock.
    public sealed class TradeService
    {
        private readonly IReadOnlyDictionary<PlayerId, PlayerPortfolio> playerAssets;
        private readonly Func<Company, int> getPrice;
        private readonly IDictionary<Company,int> inventory;

        // inventory: shared market stock per company (finite supply); null means unlimited.
        public TradeService(IReadOnlyDictionary<PlayerId, PlayerPortfolio> playerAssets, Func<Company, int> getPrice, IDictionary<Company,int> inventory = null)
        {
            this.playerAssets = playerAssets ?? throw new ArgumentNullException(nameof(playerAssets));
            this.getPrice = getPrice ?? throw new ArgumentNullException(nameof(getPrice));
            this.inventory = inventory;
        }

        public PortfolioSnapshot GetSnapshot(PlayerId playerId)
        {
            if (!playerAssets.TryGetValue(playerId, out PlayerPortfolio assets))
                throw new InvalidOperationException($"Player '{playerId}' is not in this match.");

            return assets.CreateSnapshot();
        }

        public TradeResult TryBuy(PlayerId playerId, Company company, int quantity)
        {
            if(!playerAssets.TryGetValue(playerId,out var assets)) return NotInMatch(company);
            if(inventory != null && (!inventory.TryGetValue(company,out int stock) || quantity > stock))
                return new TradeResult(false,"시장 재고가 부족합니다.",company,assets.Cash,assets.GetShares(company),0);
            var result=assets.TryBuy(company,quantity,getPrice(company));
            if(result.Succeeded && inventory != null) inventory[company]-=quantity;
            return result;
        }
        public TradeResult TrySell(PlayerId playerId, Company company, int quantity)
        {
            if(!playerAssets.TryGetValue(playerId,out var assets)) return NotInMatch(company);
            var result=assets.TrySell(company,quantity,getPrice(company));
            if(result.Succeeded && inventory != null) inventory[company]+=quantity;
            return result;
        }
        private static TradeResult NotInMatch(Company company)
        {
            return new TradeResult(false, "경기에 참가한 플레이어가 아닙니다.", company, 0, 0, 0);
        }
    }
}
