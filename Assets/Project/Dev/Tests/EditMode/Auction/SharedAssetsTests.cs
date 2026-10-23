using System.Collections.Generic;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    // Trading and auctions share one PlayerPortfolio per player, as the server match (GameNetworkManager) wires them.
    public sealed class SharedAssetsTests
    {
        private static readonly PlayerId A = new PlayerId("a");
        private static readonly Lot AnyLot = new Lot(
            new Info(2, Company.Straw, Direction.Rise, Company.Timber, Direction.Flat),
            new Info(4, Company.Timber, Direction.Fall, Company.Brick, Direction.Rise));

        [Test]
        public void ReserveReleaseAndSpendKeepCashConsistent()
        {
            var assets = new PlayerPortfolio(100, 0);

            Assert.IsTrue(assets.TryReserve(30));
            Assert.AreEqual(70, assets.Available);
            Assert.IsFalse(assets.TryReserve(71));
            Assert.AreEqual(30, assets.Reserved);

            assets.Release(10);
            Assert.AreEqual(20, assets.Reserved);

            assets.SpendReserved(20);
            Assert.AreEqual(80, assets.Cash);
            Assert.AreEqual(0, assets.Reserved);
        }

        [Test]
        public void SnapshotCarriesReserved()
        {
            var assets = new PlayerPortfolio(100, 0);
            assets.TryReserve(25);

            PortfolioSnapshot snapshot = assets.CreateSnapshot();
            Assert.AreEqual(100, snapshot.Cash);
            Assert.AreEqual(25, snapshot.Reserved);
            Assert.AreEqual(75, snapshot.Available);
        }

        [Test]
        public void TradeServiceSeesAuctionReservation()
        {
            var players = new Dictionary<PlayerId, PlayerPortfolio> { [A] = new PlayerPortfolio(100, 0) };
            var trade = new TradeService(players, _ => 10);
            var open = new OpenAuction(AnyLot, 60, 1, players);

            open.PlaceBid(A, 95, 0);

            Assert.IsFalse(trade.TryBuy(A, Company.Brick, 1).Succeeded);  // 5 available, costs 10
            Assert.AreEqual(95, trade.GetSnapshot(A).Reserved);

            open.Close();
            Assert.AreEqual(5, trade.GetSnapshot(A).Cash);
        }

        [Test]
        public void TradeServiceRejectsPlayerNotInMatch()
        {
            var trade = new TradeService(new Dictionary<PlayerId, PlayerPortfolio>(), _ => 10);
            Assert.IsFalse(trade.TryBuy(A, Company.Straw, 1).Succeeded);
        }
    }
}
