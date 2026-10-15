using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems
{
    public interface ITradeService
    {
        PlayerAssetSnapshot GetSnapshot(PlayerId playerId);
        TradeResult TryBuy(PlayerId playerId, Company company, int quantity);
        TradeResult TrySell(PlayerId playerId, Company company, int quantity);
    }
}
