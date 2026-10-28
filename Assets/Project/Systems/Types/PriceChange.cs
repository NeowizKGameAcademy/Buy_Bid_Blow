namespace BuyBidBlow.Systems.Types
{
    // Hidden until Closing Bell, then sent to clients as-is.
    public struct PriceChange
    {
        public Company Company;
        public int Amount;

        // 회사와 변동량을 설정
        public PriceChange(Company company, int amount)
        {
            Company = company;
            Amount = amount;
        }

        // 회사와 부호 붙은 변동량을 문자열로 반환
        public override string ToString() => $"{Company} {Amount:+#;-#;0}";
    }
}
