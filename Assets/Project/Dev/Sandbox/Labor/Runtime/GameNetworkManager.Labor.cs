using System;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Sandbox.Labor;
using BuyBidBlow.Systems.Labor;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Network
{
    // 노동 Sandbox 서버 처리. LaborSite(씬)가 받은 요청을 넘겨준다.
    // 위치는 클라이언트 권한으로 동기화되므로 서버는 거리와 최소 운반 시간으로 검증한다.
    public sealed partial class GameNetworkManager
    {
        [Server]
        internal void HandleLaborRequest(NetworkConnectionToClient conn, LaborRequest request, LaborSettings settings)
        {
            if (!conn.identity || !conn.identity.TryGetComponent<PlayerNet>(out var player)) return;
            if (engine == null || !matchNet || matchNet.phase == MatchNet.Phase.Finished || !player.inMatch)
            {
                conn.Send(new LaborReply { Message = "경기 중에만 일할 수 있습니다.", Carrying = -1 });
                return;
            }

            engine.EnableLabor(settings);
            var id = PlayerIdOf(conn);
            var position = player.transform.position;
            var reply = new LaborReply();
            switch (request.Action)
            {
                case LaborAction.Pickup:
                {
                    var material = (Company)request.Material;
                    if (!Enum.IsDefined(typeof(Company), material)) reply.Message = "잘못된 자재입니다.";
                    else if (Vector3.Distance(position, LaborLayout.PilePosition(material)) > LaborLayout.ServerReach)
                        reply.Message = "자재 더미 가까이에서 드세요.";
                    else reply.Message = engine.PickupLabor(id, material, NetworkTime.time);
                    break;
                }
                case LaborAction.Return:
                    reply.Message = engine.ReturnLabor(id);
                    break;
                case LaborAction.Deliver:
                {
                    if (!settings.IsFloor(request.Floor) || request.Floor > LaborLayout.TopFloor) reply.Message = "잘못된 층입니다.";
                    else if (Vector3.Distance(position, LaborLayout.DropPosition(request.Floor)) > LaborLayout.ServerReach)
                        reply.Message = "하역장 가까이에서 내려놓으세요.";
                    else
                    {
                        reply.Message = engine.DeliverLabor(id, request.Floor, NetworkTime.time, out int pay);
                        reply.Pay = pay;
                    }
                    break;
                }
                default:
                    reply.Message = "잘못된 요청입니다.";
                    break;
            }

            reply.Carrying = engine.Labor.TryGetLoad(id, out var load) ? (int)load : -1;
            reply.Earned = engine.Labor.Earned(id);
            conn.Send(reply);
            if (reply.Pay > 0) SendPortfolio(conn, player);
        }
    }
}
