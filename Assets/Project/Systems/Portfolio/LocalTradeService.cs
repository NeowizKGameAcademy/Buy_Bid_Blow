using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems
{
    // Local stand-in for the future server-authoritative trade service.
    // The match owns the player asset list and shares it with trading and auctions,
    // so cash reserved by a bid is never spent on stock.
    public sealed class LocalTradeService : ITradeService
    {
        private readonly IReadOnlyDictionary<PlayerId, PlayerAssetState> playerAssets;
        private readonly Func<Company, int> getPrice;

        public LocalTradeService(IReadOnlyDictionary<PlayerId, PlayerAssetState> playerAssets, Func<Company, int> getPrice)
        {
            this.playerAssets = playerAssets ?? throw new ArgumentNullException(nameof(playerAssets));
            this.getPrice = getPrice ?? throw new ArgumentNullException(nameof(getPrice));
        }

        public PlayerAssetSnapshot GetSnapshot(PlayerId playerId)
        {
            if (!playerAssets.TryGetValue(playerId, out PlayerAssetState assets))
                throw new InvalidOperationException($"Player '{playerId}' is not in this match.");

            return assets.CreateSnapshot();
        }

        public TradeResult TryBuy(PlayerId playerId, Company company, int quantity)
        {
            return playerAssets.TryGetValue(playerId, out PlayerAssetState assets)
                ? assets.TryBuy(company, quantity, getPrice(company))
                : NotInMatch(company);
        }

        public TradeResult TrySell(PlayerId playerId, Company company, int quantity)
        {
            return playerAssets.TryGetValue(playerId, out PlayerAssetState assets)
                ? assets.TrySell(company, quantity, getPrice(company))
                : NotInMatch(company);
        }

        private static TradeResult NotInMatch(Company company)
        {
            return new TradeResult(false, "경기에 참가한 플레이어가 아닙니다.", company, 0, 0, 0);
        }
    }
}
