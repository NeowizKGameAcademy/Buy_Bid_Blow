using UnityEngine;
namespace BuyBidBlow.Sandbox.StockGacha
{
    public static class StockGachaSandboxClient
    {
        public static StockGachaState State;
        public static StockGachaOutcome Outcome;
        public static string Reply="";
        public static float NoticeUntil;
        public static bool Pending;
        static uint request;
        public static void Reset(){State=default;Outcome=default;Reply="";NoticeUntil=0;Pending=false;request=0;}
        public static uint NextRequest()=>++request;
        public static void Show(StockGachaOutcome outcome){Outcome=outcome;NoticeUntil=Time.unscaledTime+4;}
    }
}
