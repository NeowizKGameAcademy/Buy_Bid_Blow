using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Sandbox.Shop;
using BuyBidBlow.Systems.Shop;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Network
{
    // 상점 Sandbox 서버 처리. ShopSite(씬)가 받은 요청을 넘겨준다.
    // 위치는 클라이언트 권한으로 동기화되므로 서버는 판매대와의 거리만 검증한다.
    public sealed partial class GameNetworkManager
    {
        // 서버 위치는 클라이언트보다 조금 늦으므로 거리 검증에 여유를 둔다.
        const float ShopServerSlack = 1.5f;

        bool ShopMatchRunning => engine != null && matchNet && matchNet.phase != MatchNet.Phase.Finished;

        [Server]
        internal void HandleShopRequest(NetworkConnectionToClient conn, ShopRequest request, ShopSettings settings, Vector3 counter, float shopReach)
        {
            if (!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player)) return;
            if (!ShopMatchRunning || !player.inMatch)
            {
                conn.Send(new ShopReply { Message = "경기 중에만 이용할 수 있습니다.", Bag = new int[0] });
                return;
            }

            engine.EnableShop(settings);
            var id = PlayerIdOf(conn);
            double now = NetworkTime.time;
            string message;
            bool good = false;
            switch (request.Action)
            {
                case ShopAction.Buy:
                {
                    if (Vector3.Distance(player.transform.position, counter) > shopReach + ShopServerSlack)
                    {
                        message = "상점 가까이에서 이용하세요.";
                        break;
                    }
                    int before = engine.Snapshot(id).Cash;
                    message = engine.BuyItem(id, request.ItemId);
                    good = engine.Snapshot(id).Cash != before;
                    if (good) SendPortfolio(conn, player);
                    break;
                }
                case ShopAction.Use:
                {
                    message = engine.UseItem(id, request.Slot, request.Emote, now, out var used);
                    good = used != null;
                    if (good) BroadcastShopEffect(new ShopEffectPlay { Player = player.netId, ItemId = used.Id, Emote = request.Emote, Seconds = used.Seconds });
                    break;
                }
                default:
                    message = "잘못된 요청입니다.";
                    break;
            }

            conn.Send(new ShopReply
            {
                Message = message,
                Good = good,
                Bag = engine.Shop.BagCopy(id),
                Cooldown = (float)engine.Shop.CooldownLeft(id, now)
            });
        }

        // 매 프레임: 경기가 진행 중이면 상점을 켠다.
        [Server]
        internal void ShopServerTick(ShopSettings settings)
        {
            if (!ShopMatchRunning || matchNet.phase != MatchNet.Phase.Playing) return;
            engine.EnableShop(settings);
        }

        [Server]
        void BroadcastShopEffect(ShopEffectPlay effect)
        {
            foreach (var conn in NetworkServer.connections.Values)
                if (conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var other) && other.inMatch)
                    conn.Send(effect);
        }
    }
}
