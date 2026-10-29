namespace BuyBidBlow.Common
{
    public enum MatchNoticeKind { Accepted, Rejected, Trade, BetJoined, BetCancelled, BetSettled }
    public struct MatchNotice
    {
        public MatchNoticeKind Kind;
        public string Message;
        public int Company, Quantity, Amount, Payout;
        public bool Buy, Won;
    }
    public struct AuctionOutcome
    {
        public bool Secret, Sold, Won;
        public string Winner;
        public int Amount;
    }
    // Complete breakdown is sent only after the match ends.
    public struct FinalStanding
    {
        public int Rank, Total, Cash, StockValue, Shares;
        public string Nickname;
        public bool Own;
        public int[] CompanyShares, FinalPrices, AssetHistory;
    }
}
