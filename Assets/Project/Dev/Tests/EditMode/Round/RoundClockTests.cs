using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Round;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Round
{
    public sealed class RoundClockTests
    {
        private const double Start = 100;

        private static MatchSettings Settings() => new MatchSettings
        {
            RoundCount = 17,
            RoundSeconds = 60,
            AuctionRounds = new[] { 2, 6, 10, 14 },
            AuctionRoundSeconds = 240,
        };

        [Test]
        public void BeforeFirstRound_ReturnsZero()
        {
            Assert.AreEqual(0, new RoundClock(Settings(), Start).RoundAt(Start - .01));
        }

        [Test]
        public void AtFirstRoundStart_ReturnsRoundOne()
        {
            Assert.AreEqual(1, new RoundClock(Settings(), Start).RoundAt(Start));
        }

        [Test]
        public void AtCloseTime_BelongsToNextRound()
        {
            var clock = new RoundClock(Settings(), Start);
            Assert.AreEqual(1, clock.RoundAt(clock.ClosesAt(1) - .001));
            Assert.AreEqual(2, clock.RoundAt(clock.ClosesAt(1)));
        }

        [Test]
        public void AuctionRound_Lasts240Seconds()
        {
            var clock = new RoundClock(Settings(), Start);
            Assert.AreEqual(60, clock.ClosesAt(1) - clock.StartsAt(1), 1e-9);
            Assert.AreEqual(240, clock.ClosesAt(2) - clock.StartsAt(2), 1e-9);
            Assert.AreEqual(clock.ClosesAt(1), clock.StartsAt(2), 1e-9);
        }

        [Test]
        public void WholeMatch_Lasts29Minutes()
        {
            var clock = new RoundClock(Settings(), Start);
            Assert.AreEqual(29 * 60, clock.MatchEndsAt - Start, 1e-9);
        }

        [Test]
        public void AfterLastRoundCloses_ReturnsRoundCountPlusOne()
        {
            var clock = new RoundClock(Settings(), Start);
            Assert.AreEqual(17, clock.RoundAt(clock.MatchEndsAt - .001));
            Assert.AreEqual(18, clock.RoundAt(clock.MatchEndsAt));
        }

        [Test]
        public void SecondsOverride_AppliesToEveryRound()
        {
            var clock = new RoundClock(Settings(), Start, 5);
            Assert.AreEqual(5, clock.ClosesAt(2) - clock.StartsAt(2), 1e-9);
            Assert.AreEqual(17 * 5, clock.MatchEndsAt - Start, 1e-9);
        }
    }
}
