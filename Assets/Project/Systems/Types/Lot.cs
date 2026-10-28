namespace BuyBidBlow.Systems.Types
{
    // One auction item: two different rounds (earlier first), two companies each - four directions in total.
    public struct Lot
    {
        public Info First;
        public Info Second;

        // 두 라운드 정보를 설정
        public Lot(Info first, Info second)
        {
            First = first;
            Second = second;
        }

        // 두 라운드 정보를 문자열로 반환
        public override string ToString() => $"{First}, {Second}";
    }
}
