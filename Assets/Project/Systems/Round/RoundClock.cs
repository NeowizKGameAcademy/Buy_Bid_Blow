using System;
using BuyBidBlow.Systems.Match;

namespace BuyBidBlow.Systems.Round
{
    // Fixed round schedule for one match, in server time (double seconds).
    // A round is open from StartsAt (inclusive) to ClosesAt (exclusive): a request processed
    // at ClosesAt already belongs to the next round, matching the rule that deadlines reject at >=.
    public sealed class RoundClock
    {
        private readonly double[] starts;

        // 설정에 따라 각 라운드의 시작 시각표를 계산
        // roundSecondsOverride > 0 gives every round that length; used for quick playtests.
        public RoundClock(MatchSettings settings, double firstRoundStartsAt, int roundSecondsOverride = 0)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (roundSecondsOverride < 0) throw new ArgumentOutOfRangeException(nameof(roundSecondsOverride));

            RoundCount = settings.RoundCount;
            // starts[r - 1] is round r's start; starts[RoundCount] is the end of the match.
            starts = new double[RoundCount + 1];
            starts[0] = firstRoundStartsAt;
            for (int round = 1; round <= RoundCount; round++)
            {
                int seconds = roundSecondsOverride > 0 ? roundSecondsOverride : settings.GetRoundSeconds(round);
                starts[round] = starts[round - 1] + seconds;
            }
        }

        public int RoundCount { get; }
        public double FirstRoundStartsAt => starts[0];
        public double MatchEndsAt => starts[RoundCount];

        // 라운드 시작 시각을 반환
        public double StartsAt(int round)
        {
            CheckRound(round);
            return starts[round - 1];
        }

        // 라운드 마감 시각을 반환
        public double ClosesAt(int round)
        {
            CheckRound(round);
            return starts[round];
        }

        // 주어진 시각이 속한 라운드 번호를 반환
        // 0 before the first round, 1..RoundCount while a round is open, RoundCount + 1 once the last round closed.
        public int RoundAt(double now)
        {
            if (now < starts[0]) return 0;
            for (int round = 1; round <= RoundCount; round++)
                if (now < starts[round]) return round;
            return RoundCount + 1;
        }

        // 라운드 번호가 범위를 벗어나면 예외 발생
        private void CheckRound(int round)
        {
            if (round < 1 || round > RoundCount) throw new ArgumentOutOfRangeException(nameof(round));
        }
    }
}
