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

        public Info(int round, Company companyA, Direction directionA, Company companyB, Direction directionB)
        {
            Round = round;
            CompanyA = companyA;
            DirectionA = directionA;
            CompanyB = companyB;
            DirectionB = directionB;
        }

        public override string ToString() => $"{Round}R/{CompanyA} {DirectionA}·{CompanyB} {DirectionB}";
    }
}
