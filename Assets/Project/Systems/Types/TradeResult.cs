using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Types
{
    public readonly struct TradeResult
    {
        public TradeResult(bool succeeded, string message, Company company, int cash, int shares, int total)
        {
            Succeeded = succeeded;
            Message = message;
            Company = company;
            Cash = cash;
            Shares = shares;
            Total = total;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public Company Company { get; }
        public int Cash { get; }
        public int Shares { get; }
        public int Total { get; }
    }
}
