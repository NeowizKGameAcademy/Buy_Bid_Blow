using BuyBidBlow.Systems.Labor;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow.Systems.Match
{
    // 노동 Sandbox 연결. 설정을 넘긴 경기에서만 Labor가 생기며, 원본 경기에는 영향이 없다.
    // 라운드 마감(Deadline)과 무관하게 경기가 끝나기 전까지 언제든 일할 수 있다.
    public sealed partial class MatchEngine
    {
        public LaborBoard Labor { get; private set; }

        public void EnableLabor(LaborSettings settings)
        {
            if (Labor == null) Labor = new LaborBoard(settings);
        }

        string LaborGate(PlayerId player) =>
            Labor == null ? "노동 기능이 꺼져 있습니다." :
            Complete ? "경기가 끝났습니다." :
            !assets.ContainsKey(player) ? "경기 참가자만 일할 수 있습니다." : null;

        public string PickupLabor(PlayerId player, Company material, double now) =>
            LaborGate(player) ?? Labor.Pickup(player, material, now);

        public string ReturnLabor(PlayerId player) =>
            Labor == null ? "노동 기능이 꺼져 있습니다." : Labor.Return(player);

        public string DeliverLabor(PlayerId player, int floor, double now, out int pay)
        {
            pay = 0;
            var gate = LaborGate(player);
            if (gate != null) return gate;
            var reply = Labor.Deliver(player, floor, now, out pay);
            if (pay > 0) { assets[player].Credit(pay); Changed?.Invoke(); }
            return reply;
        }
    }
}
