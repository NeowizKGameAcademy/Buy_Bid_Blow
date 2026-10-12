namespace BuyBidBlow.Systems.Types
{
    // Hidden until Closing Bell, then sent to clients as-is.
    public struct PriceChange
    {
        public Company Company;
        public int Amount;

        public PriceChange(Company company, int amount)
        {
            Company = company;
            Amount = amount;
        }

        public override string ToString() => $"{Company} {Amount:+#;-#;0}";
    }
}
