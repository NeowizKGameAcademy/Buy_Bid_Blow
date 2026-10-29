using System;
using System.IO;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    public sealed class MatchRegressionTests
    {
        private static readonly PlayerId A = new PlayerId("a"), B = new PlayerId("b"), C = new PlayerId("c");
        private static MatchSettings Rules() => JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));

        [Test]
        public void FastRounds_SettleBothAuctionsAndRetainOnlyWinnersInformation()
        {
            // 4 s rounds scale the 120 s auction round: open 4..6, secret 6..7.
            var game = new MatchEngine(Rules(), 1234, new[] { A, B, C }, 0, 4);
            game.Tick(4);
            Assert.IsTrue(game.Bid(A, 10, false, 4).Succeeded);
            game.Tick(6);
            Assert.AreEqual(190, game.Snapshot(A).Cash);
            Assert.AreEqual(0, game.Snapshot(A).Reserved);
            Assert.AreEqual(2, game.InformationOf(A).Count);
            Assert.AreEqual(0, game.InformationOf(B).Count);
            Assert.IsTrue(game.Bid(A, 20, false, 6).Succeeded);
            Assert.IsTrue(game.Bid(B, 21, false, 6.1).Succeeded);
            game.Tick(7);
            Assert.AreEqual(179, game.Snapshot(B).Cash);
            Assert.AreEqual(2, game.InformationOf(B).Count);
            Assert.AreEqual(2, game.InformationOf(A).Count);
            Assert.AreEqual(0, game.Snapshot(A).Reserved);
            game.Tick(10000);
            Assert.IsTrue(game.Complete);
            foreach (var player in new[] { A, B, C }) Assert.AreEqual(0, game.Snapshot(player).Reserved);
            Assert.AreEqual(3, game.GetStandings().Length);
        }

        [Test]
        public void FailedRequests_AreTypedAndLeaveAssetsAndSupplyUnchanged()
        {
            var game = new MatchEngine(Rules(), 1234, new[] { A, B, C }, 0);
            int stock = game.Stock(Company.Timber);
            var rejected = game.Trade(A, Company.Timber, int.MaxValue, true, 0);
            Assert.IsFalse(rejected.Succeeded);
            Assert.AreEqual(RequestKind.Buy, rejected.Kind);
            Assert.AreEqual(Company.Timber, rejected.Company);
            Assert.AreEqual(200, game.Snapshot(A).Cash);
            Assert.AreEqual(stock, game.Stock(Company.Timber));
            Assert.IsFalse(game.ToggleBet(A, (Company)99, 0).Succeeded);
            Assert.IsFalse(game.Trade(A, (Company)99, 1, true, 0).Succeeded);
            Assert.IsFalse(game.Trade(A, Company.Straw, 1, true, game.Deadline).Succeeded);
        }

        [Test]
        public void InvalidAuctionSchedule_FailsValidationBeforeMatchCreation()
        {
            var rules = Rules(); rules.AuctionRounds = new[] { 15 };
            Assert.Throws<FormatException>(() => rules.Validate());
            rules = Rules(); rules.AuctionSlots = new[] { rules.AuctionSlots[0] };
            Assert.Throws<FormatException>(() => rules.Validate());
            rules = Rules(); rules.AuctionSlots[1].StartsAtSeconds = 0;
            Assert.Throws<FormatException>(() => rules.Validate());
        }

        [Test]
        public void RepeatedTicks_DoNotDuplicateAwardsOrChargeTwice()
        {
            var game = new MatchEngine(Rules(), 1234, new[] { A, B, C }, 0, 4);
            int awards = 0; game.InformationAwarded += _ => awards++;
            game.Tick(4); game.Bid(A, 10, false, 4); game.Tick(6);
            game.Tick(6); game.Tick(6);
            Assert.AreEqual(1, awards);
            Assert.AreEqual(190, game.Snapshot(A).Cash);
            Assert.AreEqual(2, game.InformationOf(A).Count);
        }
    }
}
