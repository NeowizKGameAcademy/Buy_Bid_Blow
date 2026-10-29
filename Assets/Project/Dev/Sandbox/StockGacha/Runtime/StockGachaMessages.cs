using Mirror;
namespace BuyBidBlow.Sandbox.StockGacha
{
    public struct StockGachaPurchase : NetworkMessage { public string Session; public uint Request; }
    public struct StockGachaStart : NetworkMessage { }
    public struct StockGachaState : NetworkMessage
    {
        public string Session;
        public int Initial, Remaining, Price, Blanks, Straw, Timber, Brick, OwnSpins, Version;
        public bool Finished;
    }
    public struct StockGachaOutcome : NetworkMessage
    {
        public int Spin, Company, Cost, Remaining;
        public string Player;
    }
    public struct StockGachaReply : NetworkMessage { public string Message; public uint Request; }
}
