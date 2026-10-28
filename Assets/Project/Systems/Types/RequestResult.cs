namespace BuyBidBlow.Systems.Types
{
    public enum RequestKind { Buy, Sell, Bet, Bid, Seat, StartMatch, ReturnToLobby }

    // A reply identifies its request independently of whichever panel is currently open.
    public struct RequestResult
    {
        public RequestKind Kind;
        public bool Succeeded;
        public string Message;
        public Company Company;
        public uint StateRevision;

        public RequestResult(RequestKind kind, bool succeeded, string message, Company company = default)
        {
            Kind = kind;
            Succeeded = succeeded;
            Message = message;
            Company = company;
            StateRevision = 0;
        }
    }
}
