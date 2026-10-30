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
        // GameNetworkManager 참조 캐시
        void Awake(){room=GetComponent<GameNetworkManager>();}
        // 네트워크 매니저의 접속·승인·거절·끊김·오류·플레이어 준비 이벤트 구독
        void OnEnable()
        {
            if(!room)room=GetComponent<GameNetworkManager>();
            room.ClientConnected+=Connected;room.AdmissionApproved+=Approved;room.AdmissionRejected+=Rejected;
            room.ClientDisconnected+=Disconnected;room.ClientError+=Error;room.LocalPlayerReady+=PlayerReady;
        }
        // 네트워크 매니저 이벤트 구독 해제
        void OnDisable()
        {
            if(!room)return;room.ClientConnected-=Connected;room.AdmissionApproved-=Approved;room.AdmissionRejected-=Rejected;
            room.ClientDisconnected-=Disconnected;room.ClientError-=Error;room.LocalPlayerReady-=PlayerReady;
        }
        // 상태와 메시지를 갱신하고 변경 이벤트 발생
        void SetState(ConnectionState state,string message){State=state;Message=message;Changed?.Invoke(state,message);}
        // 닉네임·포트·주소를 검증하고 전송/매니저 설정 및 저장 후 접속 중 상태로 전환
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
        // 입력값으로 접속을 준비하고 클라이언트 시작
        public bool Join(string nickname,string address,string port)
        {
            if(!Prepare(nickname,address,port,out _))return false;
            try{room.StartClient();return true;}catch(Exception){StopTo(ConnectionState.Failed,"접속을 시작하지 못했습니다. 주소와 포트를 확인하세요.");return false;}
        }
        // 이 PC에서 방을 열고 같은 프로그램으로 입장 (Mirror host). 다른 플레이어는 이 PC의 주소로 접속한다.
        public bool Host(string nickname,string port)
        {
            if(!Prepare(nickname,"127.0.0.1",port,out _))return false;
            try{room.StartHost();return true;}catch(Exception){StopTo(ConnectionState.Failed,"방을 열지 못했습니다. 포트가 이미 사용 중인지 확인하세요.");return false;}
        }
        // 서버 연결 시 입장 승인 대기 상태로 전환
        void Connected(){if(State==ConnectionState.Connecting)SetState(ConnectionState.Joining,"서버 입장 승인 대기 중…");}
        // 입장 승인 시 플레이어 생성 중 상태로 전환
        void Approved(){if(State==ConnectionState.Connecting||State==ConnectionState.Joining)SetState(ConnectionState.Joining,"승인 완료 · 플레이어 생성 중…");}
        // 로컬 플레이어 준비 시 대기실 입장 완료 상태로 전환
        void PlayerReady(){if(State==ConnectionState.Joining)SetState(ConnectionState.InRoom,"대기실 입장 완료");}
        // 입장 거절 시 연결을 정리하고 실패 상태로 전환
        void Rejected(string reason){if(State==ConnectionState.Connecting||State==ConnectionState.Joining||State==ConnectionState.InRoom)StopTo(ConnectionState.Failed,reason);}
        // 클라이언트 오류 시 연결을 정리하고 실패 상태로 전환
        void Error(string reason){if(State==ConnectionState.Connecting||State==ConnectionState.Joining||State==ConnectionState.InRoom)StopTo(ConnectionState.Failed,reason);}
        // 예기치 않은 연결 끊김 시 상황에 맞는 메시지로 실패 상태 전환
        void Disconnected()
        {
            if(stopping||State==ConnectionState.Idle||State==ConnectionState.Failed)return;
            SetState(ConnectionState.Failed,State==ConnectionState.InRoom?"서버와 연결이 끊겼습니다. 다시 접속할 수 있습니다.":"연결하지 못했습니다. 서버의 주소·포트와 같은 네트워크인지 확인하세요.");
        }
        // 접속 중이면 연결을 정리하고 취소 처리
        public void Cancel(){if(State==ConnectionState.Connecting||State==ConnectionState.Joining)StopTo(ConnectionState.Idle,"접속을 취소했습니다.");}
        // 정지 중이 아니면 연결을 정리하고 대기실 나가기 처리
        public void Leave()
        {
            if(State==ConnectionState.Stopping)return;
            StopTo(ConnectionState.Idle,"대기실에서 나왔습니다.");
        }
        // 정지 중 상태로 전환하고 클라이언트를 중지한 뒤 정리 완료 시도
        void StopTo(ConnectionState result,string message)
        {
            if(stopping)return;stopping=true;stopResult=result;stopMessage=message;SetState(ConnectionState.Stopping,"연결 정리 중…");
            if(NetworkClient.active)room.StopClient();
            FinishStop();
        }
        // 클라이언트/서버가 모두 꺼졌으면 정지를 마치고 결과 상태로 전환
        void FinishStop(){if(stopping&&!NetworkClient.active&&!NetworkServer.active){stopping=false;SetState(stopResult,stopMessage);}}
        // 접속 시간 초과를 검사하고 연결 정리 완료 처리
        void Update()
        {
            if((State==ConnectionState.Connecting||State==ConnectionState.Joining)&&Time.realtimeSinceStartupAsDouble>=deadline)StopTo(ConnectionState.Failed,"접속 시간이 초과되었습니다. 주소·포트와 호스트 실행 상태를 확인하세요.");
            FinishStop();
        }
    }
}
