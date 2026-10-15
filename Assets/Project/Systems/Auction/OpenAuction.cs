using System.Collections.Generic;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Auction
{
    // 1st lot: public ascending bids. Bidder and amount are public; bids can't be withdrawn.
    // Bids reserve cash on the match's shared PlayerAssetState, so it can't be spent on stock meanwhile.
    public sealed class OpenAuction
    {
        private readonly IReadOnlyDictionary<PlayerId, PlayerAssetState> players;
        private bool closed;

        public Lot Lot { get; }
        public double Deadline { get; }
        public int MinBid { get; }

        public bool HasBid { get; private set; }
        public PlayerId HighestBidder { get; private set; }  // meaningful only when HasBid
        public int HighestBid { get; private set; }

        public OpenAuction(Lot lot, double deadline, int minBid, IReadOnlyDictionary<PlayerId, PlayerAssetState> players)
        {
            Lot = lot;
            Deadline = deadline;
            MinBid = minBid;
            this.players = players;
        }

        public AuctionError PlaceBid(PlayerId player, int amount, double now)
        {
            if (closed || now >= Deadline) return AuctionError.Closed;
            if (!players.TryGetValue(player, out PlayerAssetState assets)) return AuctionError.UnknownPlayer;
            if (HasBid && player.Equals(HighestBidder)) return AuctionError.AlreadyHighest;

            int minimum = HasBid ? HighestBid + 1 : MinBid;
            if (amount < minimum) return AuctionError.TooLow;

            // Reserve is the only step that can fail, so it goes last among the checks.
            if (!assets.TryReserve(amount)) return AuctionError.NotEnoughCash;

            if (HasBid)
                players[HighestBidder].Release(HighestBid);

            HasBid = true;
            HighestBidder = player;
            HighestBid = amount;
            return AuctionError.None;
        }

        // Charges the winner. Sold is false when nobody bid.
        public AuctionResult Close()
        {
            if (!closed)
            {
                closed = true;
                if (HasBid)
                    players[HighestBidder].SpendReserved(HighestBid);
            }

            return new AuctionResult(Lot, HasBid, HighestBidder, HighestBid);
        }
    }

    public readonly struct AuctionResult
    {
        public readonly Lot Lot;
        public readonly bool Sold;
        public readonly PlayerId Winner;  // meaningful only when Sold
        public readonly int Price;

        public AuctionResult(Lot lot, bool sold, PlayerId winner, int price)
        {
            Lot = lot;
            Sold = sold;
            Winner = winner;
            Price = price;
        }
    }
}
