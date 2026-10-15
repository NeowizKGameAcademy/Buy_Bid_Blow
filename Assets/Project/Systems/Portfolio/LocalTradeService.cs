using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems
{
    // Local stand-in for the future server-authoritative trade service.
    public sealed class LocalTradeService : ITradeService
    {
        private readonly Dictionary<PlayerId, PlayerAssetState> playerAssets = new Dictionary<PlayerId, PlayerAssetState>();
        private readonly Func<Company, int> getPrice;
        private readonly int startingCash;
        private readonly int startingShares;

        public LocalTradeService(Func<Company, int> getPrice, int startingCash, int startingShares)
        {
            this.getPrice = getPrice ?? throw new ArgumentNullException(nameof(getPrice));
            this.startingCash = startingCash;
            this.startingShares = startingShares;
        }

        public PlayerAssetSnapshot GetSnapshot(PlayerId playerId)
        {
            return GetOrCreate(playerId).CreateSnapshot();
        }

        public TradeResult TryBuy(PlayerId playerId, Company company, int quantity)
        {
            return GetOrCreate(playerId).TryBuy(company, quantity, getPrice(company));
        }

        public TradeResult TrySell(PlayerId playerId, Company company, int quantity)
        {
            return GetOrCreate(playerId).TrySell(company, quantity, getPrice(company));
        }

        private PlayerAssetState GetOrCreate(PlayerId playerId)
        {
            if (!playerAssets.TryGetValue(playerId, out PlayerAssetState assets))
            {
                assets = new PlayerAssetState(startingCash, startingShares);
                playerAssets.Add(playerId, assets);
            }

            return assets;
        }
    }
}
