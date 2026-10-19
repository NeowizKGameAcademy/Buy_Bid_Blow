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

        public TradeService(IReadOnlyDictionary<PlayerId, PlayerPortfolio> playerAssets, Func<Company, int> getPrice)
        {
            this.playerAssets = playerAssets ?? throw new ArgumentNullException(nameof(playerAssets));
            this.getPrice = getPrice ?? throw new ArgumentNullException(nameof(getPrice));
        }

        public PortfolioSnapshot GetSnapshot(PlayerId playerId)
        {
            if (!playerAssets.TryGetValue(playerId, out PlayerPortfolio assets))
                throw new InvalidOperationException($"Player '{playerId}' is not in this match.");

            return assets.CreateSnapshot();
        }

        public TradeResult TryBuy(PlayerId playerId, Company company, int quantity)
        {
            return playerAssets.TryGetValue(playerId, out PlayerPortfolio assets)
                ? assets.TryBuy(company, quantity, getPrice(company))
                : NotInMatch(company);
        }

        public TradeResult TrySell(PlayerId playerId, Company company, int quantity)
        {
            return playerAssets.TryGetValue(playerId, out PlayerPortfolio assets)
                ? assets.TrySell(company, quantity, getPrice(company))
                : NotInMatch(company);
        }

        private static TradeResult NotInMatch(Company company)
        {
            return new TradeResult(false, "경기에 참가한 플레이어가 아닙니다.", company, 0, 0, 0);
        }
    }
}
