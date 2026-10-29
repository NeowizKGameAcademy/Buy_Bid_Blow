using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Sandbox.Shop;
using BuyBidBlow.Systems.Shop;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Network
{
    // 상점 Sandbox 서버 처리. ShopSite(씬)가 받은 요청을 넘겨준다.
    // 위치는 클라이언트 권한으로 동기화되므로 서버는 상점·상대와의 거리만 검증한다.
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
            var position = player.transform.position;
            string message;
            bool good = false;
            switch (request.Action)
            {
                case ShopAction.Buy:
                case ShopAction.Sell:
                {
                    if (Vector3.Distance(position, counter) > shopReach + ShopServerSlack)
                    {
                        message = "상점 가까이에서 이용하세요.";
                        break;
                    }
                    int before = engine.Snapshot(id).Cash;
                    message = request.Action == ShopAction.Buy ? engine.BuyItem(id, request.ItemId) : engine.SellItem(id, request.Slot);
                    good = engine.Snapshot(id).Cash != before;
                    break;
                }
                case ShopAction.Use:
                {
                    var item = engine.Shop.ItemAt(id, request.Slot);
                    if (item == null) { message = "빈 칸입니다."; break; }
                    switch (item.Effect)
                    {
                        case ShopEffect.Leash:
                            if (!TryShopTarget(request.Target, out var targetId, out var target)) { message = "대상을 찾을 수 없습니다."; break; }
                            if (Vector3.Distance(position, target.transform.position) > settings.LeashReach + ShopServerSlack) { message = "더 가까이 가서 채우세요."; break; }
                            message = engine.UseLeash(id, request.Slot, targetId, NetworkTime.time);
                            good = engine.Shop.IsLeashed(targetId) && engine.Shop.ItemAt(id, request.Slot) == null;
                            if (good) SendShopNotice(target, $"{player.nickname}님이 목줄을 채웠습니다! 가위로 끊을 수 있습니다.", false);
                            break;
                        case ShopEffect.Scissors:
                            bool wasLeashed = engine.Shop.IsLeashed(id);
                            message = engine.UseScissors(id, request.Slot);
                            good = wasLeashed && !engine.Shop.IsLeashed(id);
                            break;
                        default:
                            message = $"{item.Name}은(는) 가지고 있기만 하면 효과가 있습니다.";
                            break;
                    }
                    break;
                }
                default:
                    message = "잘못된 요청입니다.";
                    break;
            }

            conn.Send(new ShopReply { Message = message, Good = good, Bag = engine.Shop.BagCopy(id) });
            if (good) SendPortfolio(conn, player);
        }

        // 매 프레임: 상점 켜기, 집문서 수입, 목줄 만료·이탈 정리.
        [Server]
        internal void ShopServerTick(ShopSettings settings)
        {
            if (!ShopMatchRunning || matchNet.phase != MatchNet.Phase.Playing) return;
            engine.EnableShop(settings);

            foreach (var entry in engine.PayDeedIncome())
            {
                var owner = FindParticipant(entry.Key);
                if (!owner || owner.connectionToClient == null) continue;
                owner.connectionToClient.Send(new ShopReply { Message = $"집문서 수입 +${entry.Value}", Good = true, Bag = engine.Shop.BagCopy(entry.Key) });
                SendPortfolio(owner.connectionToClient, owner);
            }

            engine.Shop.ExpireLeashes(NetworkTime.time, p => FindParticipant(p) != null);
        }

        // 지금 목줄 관계 전체. 경기가 없으면 빈 상태.
        [Server]
        internal ShopLeashState ShopLeashSnapshot()
        {
            var targets = new List<uint>();
            var holders = new List<uint>();
            if (ShopMatchRunning && engine.Shop != null)
            {
                foreach (var entry in engine.Shop.Leashes)
                {
                    var target = FindParticipant(entry.Key);
                    var holder = FindParticipant(entry.Value.Holder);
                    if (!target || !holder) continue;
                    targets.Add(target.netId);
                    holders.Add(holder.netId);
                }
            }
            return new ShopLeashState { Targets = targets.ToArray(), Holders = holders.ToArray() };
        }

        bool TryShopTarget(uint netId, out PlayerId id, out PlayerNet target)
        {
            id = default;
            target = null;
            if (!NetworkServer.spawned.TryGetValue(netId, out var identity) || identity.connectionToClient == null) return false;
            if (!identity.TryGetComponent(out target) || !target.inMatch) return false;
            id = PlayerIdOf(identity.connectionToClient);
            return true;
        }

        static void SendShopNotice(PlayerNet player, string message, bool good)
        {
            if (player && player.connectionToClient != null)
                player.connectionToClient.Send(new ShopReply { Message = message, Good = good, Bag = null });
        }
    }
}
