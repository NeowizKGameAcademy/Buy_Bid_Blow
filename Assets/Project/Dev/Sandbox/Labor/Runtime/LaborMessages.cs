using Mirror;

namespace BuyBidBlow.Sandbox.Labor
{
    public enum LaborAction : byte { Pickup, Return, Deliver }

    // 클라이언트 → 서버. Pickup은 Material, Deliver는 Floor를 쓴다.
    public struct LaborRequest : NetworkMessage
    {
        public LaborAction Action;
        public int Material;
        public int Floor;
    }

    // 서버 → 요청한 클라이언트. Carrying은 -1(빈손) 또는 Company 값.
    public struct LaborReply : NetworkMessage
    {
        public string Message;
        public int Carrying;
        public int Pay;
        public int Earned;
    }
}
