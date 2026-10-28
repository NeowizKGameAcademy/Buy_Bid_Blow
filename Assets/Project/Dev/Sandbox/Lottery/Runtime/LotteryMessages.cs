using Mirror;

namespace BuyBidBlow.Sandbox.Lottery
{
    public struct LotteryPurchase : NetworkMessage { public int Draw, Quantity; }
    public struct LotteryStart : NetworkMessage { public bool Quick; }
    public struct LotteryState : NetworkMessage
    {
        public int Draw, DrawRound, Capacity, Price, Limit, Sold, Own, Base, Carry, Sales, Prize;
        public double Deadline;
        public bool Finished;
    }
    public struct LotteryOutcome : NetworkMessage
    {
        public int Draw, Number, Prize, Sold, Carried;
        public bool Won, Final;
        public string Winner;
    }
    public struct LotteryReply : NetworkMessage { public string Message; }
}
