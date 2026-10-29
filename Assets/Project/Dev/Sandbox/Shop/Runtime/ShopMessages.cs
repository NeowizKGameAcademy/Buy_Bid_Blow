using Mirror;

namespace BuyBidBlow.Sandbox.Shop
{
    public enum ShopAction : byte { Buy, Sell, Use }

    // 클라이언트 → 서버. Buy는 ItemId, Sell·Use는 Slot을 쓴다. 목줄 Use는 Target(상대 netId)도 쓴다.
    public struct ShopRequest : NetworkMessage
    {
        public ShopAction Action;
        public int ItemId;
        public int Slot;
        public uint Target;
    }

    // 서버 → 경기 참가자. 이 서버가 상점 기능을 처리한다는 알림. 받기 전에는 요청을 보내지 않는다
    // (상점 기능이 없는 서버는 모르는 메시지를 받으면 연결을 끊는다).
    public struct ShopAvailable : NetworkMessage { }

    // 서버 → 한 플레이어. 요청 결과, 집문서 수입 알림 등. Bag은 내 가방 전체(아이템 Id, 0 = 빈 칸).
    public struct ShopReply : NetworkMessage
    {
        public string Message;
        public bool Good;
        public int[] Bag;
    }

    // 서버 → 경기 참가자 전체. 현재 목줄 관계 전체. Targets[i]가 Holders[i]에게 묶여 있다(netId).
    public struct ShopLeashState : NetworkMessage
    {
        public uint[] Targets;
        public uint[] Holders;
    }
}
