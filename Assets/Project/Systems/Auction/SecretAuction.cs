using System.Collections.Generic;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Auction
{
    // 2nd lot: sealed bids. Players may change or cancel until the deadline.
    // Each player only learns whether they won; nothing else is public.
    public sealed class SecretAuction
    {
        private readonly IReadOnlyDictionary<PlayerId, PlayerPortfolio> players;
        private readonly Dictionary<PlayerId, Submission> submissions = new Dictionary<PlayerId, Submission>();
        private long nextOrder;
        private bool closed;

        public Lot Lot { get; }
        public double Deadline { get; }
        public int MinBid { get; }

        // 물건·마감 시각·최소 입찰가·플레이어 정보를 설정
        public SecretAuction(Lot lot, double deadline, int minBid, IReadOnlyDictionary<PlayerId, PlayerPortfolio> players)
        {
            Lot = lot;
            Deadline = deadline;
            MinBid = minBid;
            this.players = players;
        }

        // 플레이어가 제출한 비밀 입찰 금액을 반환 (없으면 0)
        // 0 when the player has nothing submitted. Only for that player's own screen.
        public int GetSubmitted(PlayerId player) => submissions.TryGetValue(player, out Submission s) ? s.Amount : 0;

        // 비밀 입찰 금액을 제출·변경하고 차액만큼 현금을 예약 또는 해제
        public AuctionError Submit(PlayerId player, int amount, double now)
        {
            if (closed || now >= Deadline) return AuctionError.Closed;
            if (!players.TryGetValue(player, out PlayerPortfolio assets)) return AuctionError.UnknownPlayer;
            if (amount < MinBid) return AuctionError.TooLow;

            int previous = GetSubmitted(player);
            if (amount == previous) return AuctionError.None;  // Same amount keeps its tie priority.

            // Only the difference moves, so the old reservation counts toward the new amount.
            if (amount > previous)
            {
                if (!assets.TryReserve(amount - previous)) return AuctionError.NotEnoughCash;
            }
            else
            {
                assets.Release(previous - amount);
            }

            // A changed amount goes to the back of the tie order, like a fresh submission.
            submissions[player] = new Submission(amount, nextOrder++);
            return AuctionError.None;
        }

        // 제출한 비밀 입찰을 취소하고 예약금을 해제
        public AuctionError Cancel(PlayerId player, double now)
        {
            if (closed || now >= Deadline) return AuctionError.Closed;
            if (!submissions.TryGetValue(player, out Submission submission)) return AuctionError.NoSubmission;

            players[player].Release(submission.Amount);
            submissions.Remove(player);
            return AuctionError.None;
        }

        // 최고가(동점이면 먼저 제출) 낙찰자를 정해 결제하고 나머지 예약을 해제
        // Highest amount wins; ties go to whoever submitted that amount first.
        // Charges the winner and releases everyone else. Players = everyone who had a submission.
        public SecretResult Close()
        {
            bool sold = false;
            PlayerId winner = default;
            Submission best = default;

            foreach (KeyValuePair<PlayerId, Submission> pair in submissions)
            {
                Submission s = pair.Value;
                if (!sold || s.Amount > best.Amount || (s.Amount == best.Amount && s.Order < best.Order))
                {
                    sold = true;
                    winner = pair.Key;
                    best = s;
                }
            }

            var bidders = new List<PlayerId>(submissions.Keys);
            if (!closed)
            {
                closed = true;
                foreach (KeyValuePair<PlayerId, Submission> pair in submissions)
                {
                    if (pair.Key.Equals(winner))
                        players[pair.Key].SpendReserved(pair.Value.Amount);
                    else
                        players[pair.Key].Release(pair.Value.Amount);
                }
            }

            return new SecretResult(new AuctionResult(Lot, sold, winner, best.Amount), bidders);
        }

        private readonly struct Submission
        {
            public readonly int Amount;
            public readonly long Order;  // Server processing order; lower submitted first.

            // 제출 금액과 처리 순서를 설정
            public Submission(int amount, long order)
            {
                Amount = amount;
                Order = order;
            }
        }
    }

    public readonly struct SecretResult
    {
        // Server-side only: winner and price are never broadcast.
        public readonly AuctionResult Result;

        // Everyone who had a submission at the deadline; each is told only won or lost.
        public readonly IReadOnlyList<PlayerId> Players;

        // 경매 결과와 제출자 목록을 설정
        public SecretResult(AuctionResult result, IReadOnlyList<PlayerId> players)
        {
            Result = result;
            Players = players;
        }
    }
}
