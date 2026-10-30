using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Round;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;
using UnityEngine;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    public sealed class CurrentRoundMatchTests
    {
        private static readonly PlayerId A = new PlayerId("a"), B = new PlayerId("b");
        private static MatchSettings Rules() => JsonUtility.FromJson<MatchSettings>(File.ReadAllText("Assets/Project/Content/Match/MatchSettings.json"));

        [Test]
        public void 전체라운드_현재정보만한번지급한다()
        {
            var rules = Rules();
            var game = new MatchEngine(rules, 42, new[] { A, B }, 100);
            var schedule = MatchSchedule.Build(rules, 95, 100);
            for (int round = 1; round <= rules.RoundCount; round++)
            {
                double start = 100 + (round - 1) * 120;
                Assert.AreEqual(start + 120, game.Deadline);
                bool secret = round % 2 == 0;
                var lot = secret ? game.Auction.Secret.Lot : game.Auction.Open.Lot;
                Assert.AreEqual(round, lot.First.Round);
                Assert.IsFalse(lot.HasSecond);
                Assert.AreNotEqual(lot.First.CompanyA, lot.First.CompanyB);
                Assert.IsTrue(game.Bid(A, 1, false, start).Succeeded);
                Assert.IsTrue(game.Bid(B, 2, false, start + 1).Succeeded);
                if (secret)
                {
                    Assert.IsTrue(game.Bid(A, 0, true, start + 2).Succeeded);
                    Assert.AreEqual(0, game.Snapshot(A).Reserved);
                }
                int before = game.InformationOf(B).Count;
                game.Tick(start + 60);
                Assert.IsTrue(game.Auction.IsFinished);
                Assert.AreEqual(before + 1, game.InformationOf(B).Count);
                Assert.AreEqual(0, game.InformationOf(A).Count);
                Assert.AreEqual(0, game.Snapshot(A).Reserved);
                Assert.AreEqual(0, game.Snapshot(B).Reserved);
                Assert.IsFalse(game.Bid(A, 3, false, start + 60).Succeeded);
                Assert.IsTrue(game.Trade(A, Company.Timber, 1, true, start + 61).Succeeded);
                Assert.IsTrue(game.Trade(A, Company.Timber, 1, false, start + 62).Succeeded);
                game.Tick(start + 60);
                Assert.AreEqual(before + 1, game.InformationOf(B).Count, "repeated ticks do not duplicate awards");
                var span = schedule.Single(s => s.Round == round && (s.Kind == TimelineSpanKind.OpenAuction || s.Kind == TimelineSpanKind.SecretAuction));
                Assert.AreEqual(secret ? TimelineSpanKind.SecretAuction : TimelineSpanKind.OpenAuction, span.Kind);
                Assert.AreEqual(start, span.StartsAt);
                Assert.AreEqual(start + 60, span.EndsAt);
                game.Tick(start + 120);
            }
            Assert.IsTrue(game.Complete);
            Assert.AreEqual(rules.RoundCount, game.InformationOf(B).Count);
            Assert.AreEqual(rules.StartingCash - rules.RoundCount * 2, game.Snapshot(B).Cash);
            Assert.AreEqual(rules.RoundCount + 1, game.AssetHistory(A).Length);
        }

        [Test]
        public void 단축일정_실제경매마감과일치한다()
        {
            var rules = Rules();
            var game = new MatchEngine(rules, 42, new[] { A }, 0, 4);
            var spans = MatchSchedule.Build(rules, 0, 0, 4);
            for (int round = 1; round <= rules.RoundCount; round++)
            {
                var span = spans.Single(s => s.Round == round && (s.Kind == TimelineSpanKind.OpenAuction || s.Kind == TimelineSpanKind.SecretAuction));
                double end = game.Auction.Open != null ? game.Auction.Open.Deadline : game.Auction.Secret.Deadline;
                Assert.AreEqual(end, span.EndsAt);
                game.Tick(end);
                Assert.IsTrue(game.Auction.IsFinished);
                game.Tick(game.Deadline);
            }
            Assert.IsTrue(game.Complete);
        }

        [Test]
        public void 마지막라운드_정보방향이실제정산과일치한다()
        {
            var rules = Rules();
            var game = new MatchEngine(rules, 12, new[] { A }, 0);
            for (int round = 1; round < rules.RoundCount; round++) game.Tick(game.Deadline);
            var lot = game.Auction.Open != null ? game.Auction.Open.Lot : game.Auction.Secret.Lot;
            Assert.AreEqual(rules.RoundCount, lot.First.Round);
            int a = game.Price(lot.First.CompanyA), b = game.Price(lot.First.CompanyB);
            game.Tick(game.Deadline);
            Assert.AreEqual(DirectionOf(game.Price(lot.First.CompanyA) - a), lot.First.DirectionA);
            Assert.AreEqual(DirectionOf(game.Price(lot.First.CompanyB) - b), lot.First.DirectionB);
        }

        private static Direction DirectionOf(int value) => value > 0 ? Direction.Rise : value < 0 ? Direction.Fall : Direction.Flat;
    }
}
