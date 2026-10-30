namespace BuyBidBlow.Systems.Types
{
    // One auction item. Production lots contain the current round; older two-round fixtures remain readable.
    public struct Lot
    {
        public Info First;
        public Info Second;

        public bool HasSecond => Second.Round > 0;

        public Lot(Info information)
        {
            First = information;
            Second = default;
        }

        // 두 라운드 정보를 설정
        public Lot(Info first, Info second)
        {
            First = first;
            Second = second;
        }

        // 두 라운드 정보를 문자열로 반환
        public override string ToString() => HasSecond ? $"{First}, {Second}" : First.ToString();
    }
}
