using System;
using System.Collections.Generic;
using BuyBidBlow.Systems;
using BuyBidBlow.Systems.Auction;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Types;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Auction
{
    public sealed class AuctionTests
    {
        private static readonly PlayerId A = new PlayerId("a"), B = new PlayerId("b"), C = new PlayerId("c");
        private static readonly Lot AnyLot = new Lot(
            new Info(2, Company.Straw, Direction.Rise, Company.Timber, Direction.Flat),
            new Info(4, Company.Timber, Direction.Fall, Company.Brick, Direction.Rise));

        private Dictionary<PlayerId, PlayerAssetState> players;

        [SetUp]
        public void SetUp()
        {
            players = new Dictionary<PlayerId, PlayerAssetState> { [A] = new PlayerAssetState(100, 0), [B] = new PlayerAssetState(100, 0), [C] = new PlayerAssetState(30, 0) };
        }

        // ---------- Open ----------

        [Test]
        public void OpenFirstBidMustMeetMinimum()
        {
            var open = NewOpen();
            Assert.AreEqual(AuctionError.TooLow, open.PlaceBid(A, 0, 0));
            Assert.AreEqual(AuctionError.None, open.PlaceBid(A, 1, 0));
        }

        [Test]
        public void OpenRaiseMustBeAtLeastOneDollarHigher()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            Assert.AreEqual(AuctionError.TooLow, open.PlaceBid(B, 10, 0));
            Assert.AreEqual(AuctionError.None, open.PlaceBid(B, 11, 0));
        }

        [Test]
        public void OpenHighBidderCannotRaiseOwnBid()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            Assert.AreEqual(AuctionError.AlreadyHighest, open.PlaceBid(A, 20, 0));
        }

        [Test]
        public void OpenOutbidReleasesPreviousReservation()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            Assert.AreEqual(10, players[A].Reserved);

            open.PlaceBid(B, 15, 0);
            Assert.AreEqual(0, players[A].Reserved);
            Assert.AreEqual(15, players[B].Reserved);
        }

        [Test]
        public void OpenBidLimitedByAvailableNotCash()
        {
            var open = NewOpen();
            players[C].TryReserve(25);  // e.g. held by something else
            Assert.AreEqual(AuctionError.NotEnoughCash, open.PlaceBid(C, 10, 0));
            Assert.AreEqual(AuctionError.None, open.PlaceBid(C, 5, 0));
        }

        [Test]
        public void OpenRejectedBidChangesNothing()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            open.PlaceBid(C, 40, 0);  // C only has 30

            Assert.AreEqual(A, open.HighestBidder);
            Assert.AreEqual(10, players[A].Reserved);
            Assert.AreEqual(0, players[C].Reserved);
        }

        [Test]
        public void OpenRejectsAtOrAfterDeadline()
        {
            var open = NewOpen(deadline: 60);
            Assert.AreEqual(AuctionError.None, open.PlaceBid(A, 5, 59.99));
            Assert.AreEqual(AuctionError.Closed, open.PlaceBid(B, 6, 60));
        }

        [Test]
        public void OpenCloseChargesWinner()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            open.PlaceBid(B, 12, 0);

            AuctionResult result = open.Close();
            Assert.IsTrue(result.Sold);
            Assert.AreEqual(B, result.Winner);
            Assert.AreEqual(12, result.Price);
            Assert.AreEqual(88, players[B].Cash);
            Assert.AreEqual(0, players[B].Reserved);
            Assert.AreEqual(100, players[A].Cash);
        }

        [Test]
        public void OpenWithNoBidsIsUnsold()
        {
            Assert.IsFalse(NewOpen().Close().Sold);
        }

        [Test]
        public void OpenCloseTwiceChargesOnce()
        {
            var open = NewOpen();
            open.PlaceBid(A, 10, 0);
            open.Close();
            open.Close();
            Assert.AreEqual(90, players[A].Cash);
        }

        // ---------- Secret ----------

        [Test]
        public void SecretResubmitMovesOnlyTheDifference()
        {
            var secret = NewSecret();
            secret.Submit(C, 20, 0);
            Assert.AreEqual(20, players[C].Reserved);

            // 30 total is fine because the old 20 counts toward it.
            Assert.AreEqual(AuctionError.None, secret.Submit(C, 30, 0));
            Assert.AreEqual(30, players[C].Reserved);

            secret.Submit(C, 5, 0);
            Assert.AreEqual(5, players[C].Reserved);
        }

        [Test]
        public void SecretCancelReleasesAndAllowsResubmit()
        {
            var secret = NewSecret();
            secret.Submit(A, 20, 0);
            Assert.AreEqual(AuctionError.None, secret.Cancel(A, 0));
            Assert.AreEqual(0, players[A].Reserved);
            Assert.AreEqual(AuctionError.NoSubmission, secret.Cancel(A, 0));
            Assert.AreEqual(AuctionError.None, secret.Submit(A, 10, 0));
        }

        [Test]
        public void SecretTieGoesToEarlierSubmission()
        {
            var secret = NewSecret();
            secret.Submit(A, 20, 0);
            secret.Submit(B, 20, 0);
            Assert.AreEqual(A, secret.Close().Result.Winner);
        }

        [Test]
        public void SecretChangingAmountLosesTiePriority()
        {
            var secret = NewSecret();
            secret.Submit(A, 15, 0);
            secret.Submit(B, 20, 0);
            secret.Submit(A, 20, 0);  // A now matches B, but later
            Assert.AreEqual(B, secret.Close().Result.Winner);
        }

        [Test]
        public void SecretSameAmountResubmitKeepsPriority()
        {
            var secret = NewSecret();
            secret.Submit(A, 20, 0);
            secret.Submit(B, 20, 0);
            secret.Submit(A, 20, 0);
            Assert.AreEqual(A, secret.Close().Result.Winner);
        }

        [Test]
        public void SecretCloseChargesWinnerAndReleasesOthers()
        {
            var secret = NewSecret();
            secret.Submit(A, 20, 0);
            secret.Submit(B, 25, 0);
            secret.Submit(C, 10, 0);

            SecretResult result = secret.Close();
            Assert.AreEqual(B, result.Result.Winner);
            Assert.AreEqual(25, result.Result.Price);
            CollectionAssert.AreEquivalent(new[] { A, B, C }, result.Players);
            Assert.AreEqual(75, players[B].Cash);
            Assert.AreEqual(0, players[A].Reserved + players[B].Reserved + players[C].Reserved);
            Assert.AreEqual(100, players[A].Cash);
        }

        [Test]
        public void SecretRejectsAtOrAfterDeadline()
        {
            var secret = NewSecret(deadline: 120);
            secret.Submit(A, 10, 100);
            Assert.AreEqual(AuctionError.Closed, secret.Submit(B, 10, 120));
            Assert.AreEqual(AuctionError.Closed, secret.Cancel(A, 120));
        }

        // ---------- Round timetable ----------

        [Test]
        public void RoundRunsOpenThenSecretOnSchedule()
        {
            var round = NewRound(start: 1000);
            var log = new List<string>();
            round.OpenStarted += _ => log.Add("open");
            round.OpenClosed += _ => log.Add("open closed");
            round.SecretStarted += _ => log.Add("secret");
            round.SecretClosed += _ => log.Add("secret closed");

            round.Tick(1000);
            Assert.AreEqual(AuctionError.None, round.PlaceOpenBid(A, 10, 1030));
            Assert.AreEqual(AuctionError.NotOpen, round.SubmitSecret(B, 10, 1030));

            round.Tick(1060);
            Assert.AreEqual(AuctionError.NotOpen, round.PlaceOpenBid(B, 20, 1060));
            Assert.AreEqual(AuctionError.None, round.SubmitSecret(B, 10, 1090));

            round.Tick(1120);
            Assert.IsTrue(round.IsFinished);
            CollectionAssert.AreEqual(new[] { "open", "open closed", "secret", "secret closed" }, log);
            Assert.AreEqual(90, players[A].Cash);
            Assert.AreEqual(90, players[B].Cash);
        }

        [Test]
        public void RoundCatchesUpAfterLongGap()
        {
            var round = NewRound(start: 0);
            int events = 0;
            round.OpenStarted += _ => events++;
            round.OpenClosed += _ => events++;
            round.SecretStarted += _ => events++;
            round.SecretClosed += _ => events++;

            round.Tick(500);
            Assert.AreEqual(4, events);
            Assert.IsTrue(round.IsFinished);
        }

        [Test]
        public void ReservationBlocksStockPurchaseUntilReleased()
        {
            var round = NewRound(start: 0);
            round.Tick(0);
            round.PlaceOpenBid(C, 25, 1);

            Assert.IsFalse(players[C].TryBuy(Company.Straw, 1, 10).Succeeded);  // only 5 available
            round.PlaceOpenBid(A, 26, 2);                  // C outbid
            Assert.IsTrue(players[C].TryBuy(Company.Straw, 1, 10).Succeeded);
        }

        private OpenAuction NewOpen(double deadline = 60) => new OpenAuction(AnyLot, deadline, 1, players);
        private SecretAuction NewSecret(double deadline = 120) => new SecretAuction(AnyLot, deadline, 1, players);

        private AuctionRound NewRound(double start)
        {
            var slots = new[]
            {
                new AuctionSlot { StartsAtSeconds = 0, DurationSeconds = 60 },
                new AuctionSlot { StartsAtSeconds = 60, DurationSeconds = 60 }
            };
            return new AuctionRound(AnyLot, AnyLot, start, slots, 1, players);
        }
    }
}
