using System;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Room;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.UI.Lobby
{
    public enum ConnectionState {Idle,Connecting,Joining,InRoom,Stopping,Failed}

    [RequireComponent(typeof(GameNetworkManager))]
    public sealed class ConnectionController : MonoBehaviour
    {
        [SerializeField,Min(1)] float connectionTimeout=10;
        GameNetworkManager room;
        double deadline;
        bool stopping;
        ConnectionState stopResult;
        string stopMessage;
        public ConnectionState State {get;private set;}=ConnectionState.Idle;
        public string Message {get;private set;}="";
        public string EndpointHint {get;private set;}="";
        public bool CanConnect=>State==ConnectionState.Idle||State==ConnectionState.Failed;
        public bool IsBusy=>State==ConnectionState.Connecting||State==ConnectionState.Joining||State==ConnectionState.Stopping;
        public event Action<ConnectionState,string> Changed;
        void Awake(){room=GetComponent<GameNetworkManager>();}
        void OnEnable()
        {
            if(!room)room=GetComponent<GameNetworkManager>();
            room.ClientConnected+=Connected;room.AdmissionApproved+=Approved;room.AdmissionRejected+=Rejected;
            room.ClientDisconnected+=Disconnected;room.ClientError+=Error;room.LocalPlayerReady+=PlayerReady;
        }
        void OnDisable()
        {
            if(!room)return;room.ClientConnected-=Connected;room.AdmissionApproved-=Approved;room.AdmissionRejected-=Rejected;
            room.ClientDisconnected-=Disconnected;room.ClientError-=Error;room.LocalPlayerReady-=PlayerReady;
        }
        void SetState(ConnectionState state,string message){State=state;Message=message;Changed?.Invoke(state,message);}
        bool Prepare(string nickname,string address,string portText,out ushort port)
        {
            port=0;if(!CanConnect||NetworkClient.active||NetworkServer.active)return false;
            if(!Nickname.TryNormalize(nickname,out var name)){SetState(ConnectionState.Failed,"닉네임을 1~12글자로 입력하세요.");return false;}
            if(!DirectConnectionSettings.TryPort(portText,out port)){SetState(ConnectionState.Failed,"포트는 1~65535 사이의 숫자로 입력하세요.");return false;}
            if(!DirectConnectionSettings.TryAddress(address,out _)){SetState(ConnectionState.Failed,"접속할 PC의 IPv4 주소를 입력하세요. 예: 192.168.0.10");return false;}
            if(!(room.transport is kcp2k.KcpTransport transport)){SetState(ConnectionState.Failed,"직접 접속용 KCP 전송 설정을 확인하세요.");return false;}
            transport.Port=port;room.maxConnections=RoomRoster.Capacity+4;room.LocalNickname=name;room.networkAddress=address.Trim();
            EndpointHint=$"{room.networkAddress}:{port}";
            DirectConnectionSettings.Save(name,room.networkAddress,port);
            deadline=Time.realtimeSinceStartupAsDouble+connectionTimeout;
            SetState(ConnectionState.Connecting,$"{room.networkAddress}:{port} 연결 중…");return true;
        }
        public bool Join(string nickname,string address,string port)
        {
            if(!Prepare(nickname,address,port,out _))return false;
            try{room.StartClient();return true;}catch(Exception){StopTo(ConnectionState.Failed,"접속을 시작하지 못했습니다. 주소와 포트를 확인하세요.");return false;}
        }
        void Connected(){if(State==ConnectionState.Connecting)SetState(ConnectionState.Joining,"서버 입장 승인 대기 중…");}
        void Approved(){if(State==ConnectionState.Connecting||State==ConnectionState.Joining)SetState(ConnectionState.Joining,"승인 완료 · 플레이어 생성 중…");}
        void PlayerReady(){if(State==ConnectionState.Joining)SetState(ConnectionState.InRoom,"대기실 입장 완료");}
        void Rejected(string reason){if(State==ConnectionState.Connecting||State==ConnectionState.Joining||State==ConnectionState.InRoom)StopTo(ConnectionState.Failed,reason);}
        void Error(string reason){if(State==ConnectionState.Connecting||State==ConnectionState.Joining||State==ConnectionState.InRoom)StopTo(ConnectionState.Failed,reason);}
        void Disconnected()
        {
            if(stopping||State==ConnectionState.Idle||State==ConnectionState.Failed)return;
            SetState(ConnectionState.Failed,State==ConnectionState.InRoom?"서버와 연결이 끊겼습니다. 다시 접속할 수 있습니다.":"연결하지 못했습니다. 서버의 주소·포트와 같은 네트워크인지 확인하세요.");
        }
        public void Cancel(){if(State==ConnectionState.Connecting||State==ConnectionState.Joining)StopTo(ConnectionState.Idle,"접속을 취소했습니다.");}
        public void Leave()
        {
            if(State==ConnectionState.Stopping)return;
            StopTo(ConnectionState.Idle,"대기실에서 나왔습니다.");
        }
        void StopTo(ConnectionState result,string message)
        {
            if(stopping)return;stopping=true;stopResult=result;stopMessage=message;SetState(ConnectionState.Stopping,"연결 정리 중…");
            if(NetworkClient.active)room.StopClient();
            FinishStop();
        }
        void FinishStop(){if(stopping&&!NetworkClient.active&&!NetworkServer.active){stopping=false;SetState(stopResult,stopMessage);}}
        void Update()
        {
            if((State==ConnectionState.Connecting||State==ConnectionState.Joining)&&Time.realtimeSinceStartupAsDouble>=deadline)StopTo(ConnectionState.Failed,"접속 시간이 초과되었습니다. 주소·포트와 호스트 실행 상태를 확인하세요.");
            FinishStop();
        }
    }
}
