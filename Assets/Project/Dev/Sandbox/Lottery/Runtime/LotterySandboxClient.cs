using UnityEngine;

namespace BuyBidBlow.Sandbox.Lottery
{
    public static class LotterySandboxClient
    {
        public static LotteryState State;
        public static LotteryOutcome Outcome;
        public static string Reply="";
        public static float NoticeUntil;
        public static void Reset(){State=default;Outcome=default;Reply="";NoticeUntil=0;}
        public static void Show(LotteryOutcome outcome)
        {Outcome=outcome;NoticeUntil=Time.unscaledTime+10;}
    }
}
