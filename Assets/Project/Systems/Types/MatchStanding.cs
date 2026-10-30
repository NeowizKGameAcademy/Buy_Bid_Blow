namespace BuyBidBlow.Systems.Types
{
    // Public final ranking. Cash and individual holdings remain owner-only.
    public struct MatchStanding
    {
        public string PlayerId;
        public string Nickname;
        public int Rank;
        public int TotalAssets;
    }
}
