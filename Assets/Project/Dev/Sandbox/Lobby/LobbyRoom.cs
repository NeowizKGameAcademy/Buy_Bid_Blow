using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.LobbySandbox
{
    public struct LobbyJoinRequest : NetworkMessage {public int protocol;public string nickname;}
    public struct LobbyJoinResponse : NetworkMessage {public bool accepted;public string reason;}
    public struct LobbyClosing : NetworkMessage {}
    public sealed class LobbyRoom : NetworkManager
    {
        public const int ProtocolVersion=1;
        readonly LobbyRoster roster=new LobbyRoster();
        public event Action ClientConnected,AdmissionApproved,ClientDisconnected,LocalPlayerReady;
        public event Action<string> AdmissionRejected,ClientError;
        // Captured before connecting; the owned player submits it once after spawning.
        public string LocalNickname {get;set;}=string.Empty;
        public bool CanStartMatch
        {
            get {
                if(!NetworkServer.active)return false;
                var players=NetworkServer.connections.Values.Where(c=>c.identity).Select(c=>c.identity.GetComponent<LobbyPlayer>()).Where(p=>p).ToArray();
                return players.Length>=3&&players.Length==roster.Count&&players.All(p=>p.ready);
            }
        }
        public override void OnStartServer()
        {
            base.OnStartServer();roster.Clear();NetworkServer.RegisterHandler<LobbyJoinRequest>(ReceiveJoin);
        }
        public override void OnStopServer(){roster.Clear();base.OnStopServer();}
        public override void OnStartClient()
        {
            base.OnStartClient();NetworkClient.RegisterHandler<LobbyJoinResponse>(ReceiveResponse);
            NetworkClient.RegisterHandler<LobbyClosing>(_=>AdmissionRejected?.Invoke("호스트가 방을 종료했습니다."));
        }
        public override void OnClientConnect()
        {
            // Defer Ready/AddPlayer until server capacity and nickname checks have passed.
            ClientConnected?.Invoke();NetworkClient.Send(new LobbyJoinRequest{protocol=ProtocolVersion,nickname=LocalNickname});
        }
        public override void OnClientDisconnect(){ClientDisconnected?.Invoke();base.OnClientDisconnect();}
        public override void OnClientError(TransportError error,string reason)
        {
            ClientError?.Invoke(error==TransportError.Timeout?"접속 시간이 초과되었습니다. 호스트 상태를 확인하세요.":"네트워크 연결 오류가 발생했습니다. 주소·포트와 호스트 상태를 확인하세요.");
        }
        public override void OnServerConnect(NetworkConnectionToClient conn){base.OnServerConnect(conn);StartCoroutine(AdmissionTimeout(conn));}
        IEnumerator AdmissionTimeout(NetworkConnectionToClient conn)
        {
            yield return new WaitForSecondsRealtime(10);
            if(NetworkServer.active&&NetworkServer.connections.TryGetValue(conn.connectionId,out var current)&&current==conn&&!conn.identity) {
                roster.Release(conn.connectionId);Reject(conn,"입장 처리 시간이 초과되었습니다.");
            }
        }
        void ReceiveJoin(NetworkConnectionToClient conn,LobbyJoinRequest message)
        {
            if(conn.identity||roster.Contains(conn.connectionId))return;
            if(message.protocol!=ProtocolVersion){Reject(conn,"게임 버전이 다릅니다. 같은 버전으로 실행하세요.");return;}
            if(!roster.TryReserve(conn.connectionId,message.nickname,out var error)){Reject(conn,error);return;}
            conn.Send(new LobbyJoinResponse{accepted=true,reason=""});
        }
        void ReceiveResponse(LobbyJoinResponse message)
        {
            if(!message.accepted){AdmissionRejected?.Invoke(message.reason);return;}
            if(NetworkClient.localPlayer||NetworkClient.ready)return;
            AdmissionApproved?.Invoke();NetworkClient.Ready();NetworkClient.AddPlayer();
        }
        void Reject(NetworkConnectionToClient conn,string reason)
        {
            conn.Send(new LobbyJoinResponse{accepted=false,reason=reason});StartCoroutine(DisconnectRejected(conn));
        }
        IEnumerator DisconnectRejected(NetworkConnectionToClient conn){yield return new WaitForSecondsRealtime(.25f);if(NetworkServer.active&&NetworkServer.connections.TryGetValue(conn.connectionId,out var current)&&current==conn)conn.Disconnect();}
        public override void OnServerDisconnect(NetworkConnectionToClient conn){roster.Release(conn.connectionId);base.OnServerDisconnect(conn);}
        public void NotifyLocalPlayerReady()=>LocalPlayerReady?.Invoke();
        public void BroadcastClosing(){if(NetworkServer.active)NetworkServer.SendToAll(new LobbyClosing());}
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if(!roster.TryGetName(conn.connectionId,out var name)){Reject(conn,"입장 승인이 필요합니다.");return;}
            var used = NetworkServer.connections.Values.Where(c => c.identity).Select(c => c.identity.GetComponent<LobbyPlayer>().seat).ToArray();
            int seat = Enumerable.Range(0,6).FirstOrDefault(i => !used.Contains(i));
            if (used.Length >= 6) { conn.Disconnect(); return; }
            var player = Instantiate(playerPrefab);
            var lobbyPlayer=player.GetComponent<LobbyPlayer>();lobbyPlayer.seat = seat;
            lobbyPlayer.nickname=name;
            NetworkServer.AddPlayerForConnection(conn,player);
        }
    }
}
