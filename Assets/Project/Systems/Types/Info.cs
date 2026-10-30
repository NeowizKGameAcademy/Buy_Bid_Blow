namespace BuyBidBlow.Systems.Types
{
    // One target round of a lot: two different companies, CompanyA before CompanyB in Company order.
    // Round and companies are public once the lot is up; directions go to the winner only.
    public struct Info
    {
        public int Round;
        public Company CompanyA;
        public Direction DirectionA;
        public Company CompanyB;
        public Direction DirectionB;

        // 라운드와 두 회사의 등락 정보를 설정
        public Info(int round, Company companyA, Direction directionA, Company companyB, Direction directionB)
        {
            Round = round;
            CompanyA = companyA;
            DirectionA = directionA;
            CompanyB = companyB;
            DirectionB = directionB;
        }

        // 라운드·회사·등락을 문자열로 반환
        public override string ToString() => $"{Round}R/{CompanyA.DisplayName()} {DirectionA}·{CompanyB.DisplayName()} {DirectionB}";
    }
}
