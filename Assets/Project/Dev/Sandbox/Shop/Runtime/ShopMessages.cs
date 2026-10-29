using Mirror;

namespace BuyBidBlow.Sandbox.Shop
{
    public enum ShopAction : byte { Buy, Use }

    // 클라이언트 → 서버. Buy는 ItemId, Use는 Slot을 쓴다. 이모티콘 Use는 Emote(Emotes 번호)도 쓴다.
    public struct ShopRequest : NetworkMessage
    {
        public ShopAction Action;
        public int ItemId;
        public int Slot;
        public int Emote;
    }

    // 서버 → 경기 참가자. 이 서버가 상점 기능을 처리한다는 알림. 받기 전에는 요청을 보내지 않는다
    // (상점 기능이 없는 서버는 모르는 메시지를 받으면 연결을 끊는다).
    public struct ShopAvailable : NetworkMessage { }

    // 서버 → 한 플레이어. 요청 결과. Bag은 내 가방 전체(아이템 Id, 0 = 빈 칸). Cooldown은 다시 쓸 수 있을 때까지 남은 초.
    public struct ShopReply : NetworkMessage
    {
        public string Message;
        public bool Good;
        public int[] Bag;
        public float Cooldown;
    }

    // 서버 → 경기 참가자 전체. Player(netId) 주변에 효과를 Seconds초 동안 보여 준다. Emote는 이모티콘 번호.
    public struct ShopEffectPlay : NetworkMessage
    {
        public uint Player;
        public int ItemId;
        public int Emote;
        public float Seconds;
    }
}
