namespace BuyBidBlow.Systems.Auction
{
    // Why a bid, submission or cancel was refused. None means it went through.
    public enum AuctionError
    {
        None,
        NotOpen,         // No lot of this kind is taking bids right now.
        Closed,          // Request reached the server at or after the deadline.
        UnknownPlayer,
        TooLow,          // Below the minimum, or not at least $1 above the Open high bid.
        AlreadyHighest,  // The Open high bidder can't raise their own bid.
        NotEnoughCash,   // Available cash (Money - Reserved) is short.
        NoSubmission     // Secret cancel with nothing submitted.
    }
}
