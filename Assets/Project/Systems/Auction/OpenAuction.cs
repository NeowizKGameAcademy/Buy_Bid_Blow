using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Auction
{
    // 1st lot: public ascending bids. Bidder and amount are public; bids can't be withdrawn.
    public sealed class OpenAuction
    {
        public const int NoBidder = -1;

        private readonly IReadOnlyDictionary<int, IWallet> wallets;
        private bool closed;

        public Lot Lot { get; }
        public double Deadline { get; }
        public int MinBid { get; }

        public int HighestBidder { get; private set; } = NoBidder;
        public int HighestBid { get; private set; }

        public OpenAuction(Lot lot, double deadline, int minBid, IReadOnlyDictionary<int, IWallet> wallets)
        {
            Lot = lot;
            Deadline = deadline;
            MinBid = minBid;
            this.wallets = wallets;
        }

        public AuctionError PlaceBid(int player, int amount, double now)
        {
            if (closed || now >= Deadline) return AuctionError.Closed;
            if (!wallets.TryGetValue(player, out IWallet wallet)) return AuctionError.UnknownPlayer;
            if (player == HighestBidder) return AuctionError.AlreadyHighest;

            int minimum = HighestBidder == NoBidder ? MinBid : HighestBid + 1;
            if (amount < minimum) return AuctionError.TooLow;

            // Reserve is the only step that can fail, so it goes last among the checks.
            if (!wallet.TryReserve(amount)) return AuctionError.NotEnoughCash;

            if (HighestBidder != NoBidder)
                wallets[HighestBidder].Release(HighestBid);

            HighestBidder = player;
            HighestBid = amount;
            return AuctionError.None;
        }

        // Charges the winner. Winner is NoBidder when nobody bid (no sale).
        public AuctionResult Close()
        {
            if (!closed)
            {
                closed = true;
                if (HighestBidder != NoBidder)
                    wallets[HighestBidder].SpendReserved(HighestBid);
            }

            return new AuctionResult(Lot, HighestBidder, HighestBid);
        }
    }

    public readonly struct AuctionResult
    {
        public readonly Lot Lot;
        public readonly int Winner;   // OpenAuction.NoBidder when unsold.
        public readonly int Price;

        public bool Sold => Winner != OpenAuction.NoBidder;

        public AuctionResult(Lot lot, int winner, int price)
        {
            Lot = lot;
            Winner = winner;
            Price = price;
        }
    }
}
