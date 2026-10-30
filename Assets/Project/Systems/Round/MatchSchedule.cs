using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Match;

namespace BuyBidBlow.Systems.Round
{
    public enum TimelineSpanKind { Prepare, Match, Round, OpenAuction, SecretAuction }

    // One stretch of a match in server time, open from StartsAt (inclusive) to EndsAt (exclusive).
    // Round is 0 for Prepare and Match.
    [Serializable]
    public struct TimelineSpan
    {
        public TimelineSpanKind Kind;
        public int Round;
        public double StartsAt;
        public double EndsAt;

        public TimelineSpan(TimelineSpanKind kind, int round, double startsAt, double endsAt)
        {
            Kind = kind;
            Round = round;
            StartsAt = startsAt;
            EndsAt = endsAt;
        }

        public bool Contains(double time) => time >= StartsAt && time < EndsAt;
    }

    // Every span of one match, as MatchEngine will run it. The whole match is fixed by the rules at its start.
    public static class MatchSchedule
    {
        public static List<TimelineSpan> Build(MatchSettings settings, double preparesAt, double firstRoundStartsAt, int roundSecondsOverride = 0)
        {
            var clock = new RoundClock(settings, firstRoundStartsAt, roundSecondsOverride);
            var spans = new List<TimelineSpan>();
            if (preparesAt < firstRoundStartsAt) spans.Add(new TimelineSpan(TimelineSpanKind.Prepare, 0, preparesAt, firstRoundStartsAt));
            spans.Add(new TimelineSpan(TimelineSpanKind.Match, 0, clock.FirstRoundStartsAt, clock.MatchEndsAt));
            for (int round = 1; round <= clock.RoundCount; round++)
            {
                double opens = clock.StartsAt(round), closes = clock.ClosesAt(round);
                spans.Add(new TimelineSpan(TimelineSpanKind.Round, round, opens, closes));
                if (!settings.IsAuctionRound(round)) continue;
                double scale = settings.AuctionTimeScale(round, roundSecondsOverride);
                for (int i = 0; i < settings.AuctionSlots.Length && i < 2; i++)
                {
                    var slot = settings.AuctionSlots[i];
                    double starts = opens + slot.StartsAtSeconds * scale;
                    if (starts >= closes) continue;
                    var kind = (settings.SingleAuctionPerRound ? settings.IsSecretAuctionRound(round) : i == 1)
                        ? TimelineSpanKind.SecretAuction : TimelineSpanKind.OpenAuction;
                    spans.Add(new TimelineSpan(kind, round, starts, Math.Min(closes, starts + slot.DurationSeconds * scale)));
                }
            }
            return spans;
        }
    }
}
