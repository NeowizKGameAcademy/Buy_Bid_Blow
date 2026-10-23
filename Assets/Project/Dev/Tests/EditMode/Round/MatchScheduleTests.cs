using System.Linq;
using BuyBidBlow.Systems.Match;
using BuyBidBlow.Systems.Round;
using NUnit.Framework;

namespace BuyBidBlow.Dev.Tests.EditMode.Round
{
    public sealed class MatchScheduleTests
    {
        private const double Prepare = 95, Start = 100;

        private static MatchSettings Settings() => new MatchSettings
        {
            RoundCount = 4,
            RoundSeconds = 60,
            AuctionRounds = new[] { 2 },
            AuctionRoundSeconds = 180,
            AuctionSlots = new[]
            {
                new AuctionSlot { StartsAtSeconds = 0, DurationSeconds = 60 },
                new AuctionSlot { StartsAtSeconds = 60, DurationSeconds = 60 },
            },
        };

        [Test]
        public void RoundsMatchRoundClock()
        {
            var clock = new RoundClock(Settings(), Start);
            var rounds = MatchSchedule.Build(Settings(), Prepare, Start).Where(s => s.Kind == TimelineSpanKind.Round).ToArray();
            Assert.AreEqual(4, rounds.Length);
            for (int r = 1; r <= 4; r++)
            {
                Assert.AreEqual(r, rounds[r - 1].Round);
                Assert.AreEqual(clock.StartsAt(r), rounds[r - 1].StartsAt);
                Assert.AreEqual(clock.ClosesAt(r), rounds[r - 1].EndsAt);
            }
        }

        [Test]
        public void PrepareAndMatchFrameTheRounds()
        {
            var spans = MatchSchedule.Build(Settings(), Prepare, Start);
            var prepare = spans.Single(s => s.Kind == TimelineSpanKind.Prepare);
            var match = spans.Single(s => s.Kind == TimelineSpanKind.Match);
            Assert.AreEqual(Prepare, prepare.StartsAt);
            Assert.AreEqual(Start, prepare.EndsAt);
            Assert.AreEqual(Start, match.StartsAt);
            Assert.AreEqual(Start + 60 + 180 + 60 + 60, match.EndsAt);
        }

        [Test]
        public void NoPrepareSpanWithoutCountdown()
        {
            Assert.IsFalse(MatchSchedule.Build(Settings(), Start, Start).Any(s => s.Kind == TimelineSpanKind.Prepare));
        }

        [Test]
        public void AuctionRoundCarriesOpenThenSecretSlots()
        {
            var spans = MatchSchedule.Build(Settings(), Prepare, Start);
            var open = spans.Single(s => s.Kind == TimelineSpanKind.OpenAuction);
            var secret = spans.Single(s => s.Kind == TimelineSpanKind.SecretAuction);
            Assert.AreEqual(2, open.Round);
            Assert.AreEqual(Start + 60, open.StartsAt);
            Assert.AreEqual(Start + 120, open.EndsAt);
            Assert.AreEqual(Start + 120, secret.StartsAt);
            Assert.AreEqual(Start + 180, secret.EndsAt);
        }

        [Test]
        public void ShortRoundOverrideCutsSlotsAtTheClose()
        {
            var spans = MatchSchedule.Build(Settings(), Prepare, Start, 30);
            var open = spans.Single(s => s.Kind == TimelineSpanKind.OpenAuction);
            Assert.AreEqual(Start + 30, open.StartsAt);
            Assert.AreEqual(Start + 60, open.EndsAt);
            Assert.IsFalse(spans.Any(s => s.Kind == TimelineSpanKind.SecretAuction), "secret slot starts after the round closed");
        }

        [Test]
        public void ContainsIsStartInclusiveEndExclusive()
        {
            var span = new TimelineSpan(TimelineSpanKind.Round, 1, 10, 20);
            Assert.IsTrue(span.Contains(10));
            Assert.IsFalse(span.Contains(20));
        }
    }
}
