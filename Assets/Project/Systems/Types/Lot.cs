namespace BuyBidBlow.Systems.Types
{
    // One auction item: two different rounds (earlier first), two companies each - four directions in total.
    public struct Lot
    {
        public Info First;
        public Info Second;

        public Lot(Info first, Info second)
        {
            First = first;
            Second = second;
        }

        public override string ToString() => $"{First}, {Second}";
    }
}
