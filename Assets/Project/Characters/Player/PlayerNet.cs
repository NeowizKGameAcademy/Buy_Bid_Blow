using System;
using System.Collections.Generic;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    public sealed class PlayerNet : NetworkBehaviour
    {
        [SyncVar] public int seat;
        [SyncVar] public bool ready;
        [SyncVar] public int top;
        [SyncVar] public int bottom;
        [SyncVar] public int tie;
        [SyncVar] public string nickname;
        [SyncVar] public string playerId;
        // Set by the server when the match starts. In the level the owner plays through the local Player
        // and its position is synced (NetworkTransform, client to server); other clients see this avatar.
        [SyncVar(hook=nameof(OnInMatchChanged))] public bool inMatch;
        // 서버에서 플레이어 오브젝트를 씬 전환 후에도 유지
        // The player object lives from connection through lobby and match, across scene loads.
        public override void OnStartServer(){base.OnStartServer();DontDestroyOnLoad(gameObject);}
        // 클라이언트에서 오브젝트를 유지하고 목록에 등록한 뒤 아바타 표시 갱신
        public override void OnStartClient(){base.OnStartClient();DontDestroyOnLoad(gameObject);All.Add(this);RefreshAvatar();}
        // 플레이어 목록에서 제거
        public override void OnStopClient(){All.Remove(this);base.OnStopClient();}
        public static readonly List<PlayerNet> All = new List<PlayerNet>();
        // 매치 참여 상태가 바뀌면 아바타 표시 갱신
        void OnInMatchChanged(bool _,bool value)
        {
            RefreshAvatar();
            if (isLocalPlayer && NetworkManager.singleton is GameNetworkManager room) room.SetClientMatchState(value);
        }
        // 매치 중인 자신의 아바타면 자식 모델을 숨기고 아니면 표시
        // The owner already sees its own wolf through the local Player, so its avatar hides in the level.
        void RefreshAvatar(){bool visible=!inMatch||!isLocalPlayer;foreach(Transform child in transform)child.gameObject.SetActive(visible);}
        // 아바타 표시를 갱신하고 네트워크 매니저에 로컬 플레이어 준비를 알림
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();RefreshAvatar();
            if(NetworkManager.singleton is GameNetworkManager room){room.NotifyLocalPlayerReady();room.SetClientMatchState(inMatch);}
        }
        // The avatar model faces the root's -Z (the lobby lines them up facing the camera with no rotation),
        // so in the level it turns around to face where the local player faces.
        public static readonly Quaternion ModelYaw = Quaternion.Euler(0, 180, 0);
        public static readonly Color[] Colors = {new Color(.6f,.12f,.2f), new Color(.1f,.5f,.7f), new Color(.8f,.6f,.12f), new Color(.2f,.65f,.35f), new Color(.55f,.3f,.7f), Color.black};
        BBBWolfAppearance appearance;
        BBBWolfCharacter character;
        PlayerSeating localBody;
        Vector3 lastPosition;
        float walkSpeed;
        // Players wait in the lobby room behind the hall's LOBBY door (Content/Levels/LobbyRoom), lined up along +X.
        public static readonly Vector3 WaitingCenter = new Vector3(20.87f, 0, -2.4f);
        public static Quaternion WaitingRotation => Quaternion.identity;
        // The match starts on the hall's under-mezzanine carpet, keeping the six players apart.
        public static readonly Vector3 MatchStartCenter = new Vector3(0, .04f, 9.6f);
        // 로비 대기 자리 인덱스에 해당하는 위치 계산
        public static Vector3 SeatPosition(int index)=>WaitingCenter+Vector3.right*((index-2.5f)*2.1f);
        // 매치 시작 자리 인덱스에 해당하는 홀 위치 계산
        public static Vector3 MatchStartPosition(int index)=>MatchStartCenter+Vector3.right*((index-2.5f)*2.1f);
        // 외형·캐릭터 컴포넌트를 찾고 루트모션을 끄며 동작과 이동 속도 초기화
        void Awake()
        {
            appearance = GetComponentInChildren<BBBWolfAppearance>(true);
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            character=GetComponentInChildren<BBBWolfCharacter>(true);
            if(character) {character.StopAction();character.SetMovementSpeed(0);}
        }
        // 매치 중이면 따라가기, 아니면 로비 자리에 배치하고 외형 적용
        void LateUpdate()
        {
            if (!isClient) return;
            FlushReplies();
            if(inMatch){FollowInMatch();return;}
            transform.SetPositionAndRotation(SeatPosition(seat), WaitingRotation);
            transform.localScale=Vector3.one*1.5f;
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        // 로컬이면 로컬 플레이어 위치를 따라가고, 원격이면 이동 속도로 걷기 애니메이션과 외형 갱신
        void FollowInMatch()
        {
            transform.localScale=Vector3.one;
            if(isLocalPlayer)
            {
                if(!localBody)localBody=FindFirstObjectByType<PlayerSeating>();
                if(localBody)transform.SetPositionAndRotation(localBody.transform.position,Quaternion.Euler(0,localBody.transform.eulerAngles.y,0)*ModelYaw);
                return;
            }
            // Remote wolves walk at the speed their synced position moves.
            var step=transform.position-lastPosition;step.y=0;lastPosition=transform.position;
            walkSpeed=Mathf.Lerp(walkSpeed,Time.deltaTime>0?step.magnitude/Time.deltaTime:0,.2f);
            if(character)character.SetMovementSpeed(Mathf.Clamp01(walkSpeed/WalkSpeedForFullBlend));
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        const float WalkSpeedForFullBlend=4f;
        // 준비 상태 설정 (서버 명령)
        [Command] public void SetReady(bool value) { ready = value; }
        // 서버에 매치 시작 요청
        // Only honored for the room host; the server checks readiness and player count.
        [Command] public void CmdStartMatch() { if (NetworkManager.singleton is GameNetworkManager room) room.RequestStartMatch(this); }
        // 옷·넥타이 색상 값을 검증해 변경하고 준비 상태 해제
        [Command] public void Customize(int upper, int lower, int color)
        {
            if (upper < 0 || upper > 2 || lower < 0 || lower > 1 || color < 0 || color >= Colors.Length) return;
            if (top == upper && bottom == lower && tie == color) return;
            top = upper; bottom = lower; tie = color; ready = false;
        }

        // ---- Trading: private to the owner; the server's MatchEngine decides every request. ----
        // Last portfolio and downside bet the server sent this client; the trade UI may enable after they arrive.
        public static PortfolioSnapshot LocalPortfolio { get; private set; }
        public static Company? LocalBet { get; private set; }
        public static int LocalBetStake { get; private set; }
        // This client's submission to the running secret auction (0 = none); nobody else learns it.
        public static int LocalSecretBid { get; private set; }
        public static event Action<PortfolioSnapshot> LocalPortfolioChanged;
        // The server's answer to this client's last trade or bet request.
        public static event Action<RequestResult> LocalReply;
        private readonly List<RequestResult> pendingReplies = new List<RequestResult>();
        public static IReadOnlyList<Info> LocalInformation { get; private set; } = new Info[0];
        public static event Action LocalInformationChanged;

        // 서버에서 매매를 처리하고 결과를 요청한 클라이언트에 응답
        [Command] public void CmdTrade(Company company, int quantity, bool buy)
        {
            if (NetworkManager.singleton is GameNetworkManager room) Reply(room.Trade(connectionToClient, company, quantity, buy));
        }
        // 서버에서 하락 베팅을 토글하고 결과를 응답
        [Command] public void CmdToggleBet(Company company)
        {
            if (NetworkManager.singleton is GameNetworkManager room) Reply(room.ToggleBet(connectionToClient, company));
        }
        // 서버에서 입찰 또는 입찰 취소를 처리하고 결과를 응답
        // Seated bid: amount to the running auction, or cancel this player's secret submission.
        [Command] public void CmdBid(int amount, bool cancel)
        {
            if (NetworkManager.singleton is GameNetworkManager room) Reply(room.Bid(connectionToClient, amount, cancel));
        }
        // 서버 응답 메시지를 로컬 응답 이벤트로 전달
        [Server] public void Reply(RequestResult result)
        {
            result.StateRevision = GetComponent<PlayerAssetsNet>().Revision;
            TargetReply(result);
        }
        [TargetRpc] private void TargetReply(RequestResult result) { pendingReplies.Add(result); FlushReplies(); }
        private void FlushReplies()
        {
            if (!isLocalPlayer) return;
            uint revision = GetComponent<PlayerAssetsNet>().Revision;
            while (pendingReplies.Count > 0 && pendingReplies[0].StateRevision <= revision)
            {
                var result = pendingReplies[0]; pendingReplies.RemoveAt(0); LocalReply?.Invoke(result);
            }
        }

        // 포트폴리오 스냅샷을 배열로 바꿔 소유 클라이언트에 전송
        // bet: the Company index of the open downside bet, or -1.
        [Server] public void SendPortfolio(PortfolioSnapshot snapshot, int bet, int betStake, int secretBid, IReadOnlyList<Info> information)
        {
            GetComponent<PlayerAssetsNet>().Publish(snapshot, bet, betStake, secretBid, information);
        }
        // 받은 포트폴리오·베팅·비밀 입찰 정보를 로컬에 저장하고 변경 이벤트 발생
        internal static void ApplyLocalState(PortfolioSnapshot snapshot, Company? bet, int betStake, int secretBid, IReadOnlyList<Info> information)
        {
            LocalPortfolio = snapshot;
            LocalBet = bet;
            LocalBetStake = betStake;
            LocalSecretBid = secretBid;
            var copy = new Info[information.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = information[i];
            LocalInformation = copy;
            LocalPortfolioChanged?.Invoke(LocalPortfolio);
            LocalInformationChanged?.Invoke();
        }
        // ---- Hall seats: the server hands out each seat slot to one player at a time. ----
        // Slot first: Mirror applies SyncVars in declaration order, so the seat hook sees its slot.
        [SyncVar] public int hallSeatSlot;
        // Seat.Id of the chair this player sits on, "" while standing.
        [SyncVar(hook = nameof(OnHallSeatChanged))] public string hallSeat = "";
        public bool IsSeated => !string.IsNullOrEmpty(hallSeat);
        // The owner's seat changed: the local Player sits down or stands up to match.
        public static event Action<string, int> LocalSeatChanged;

        // 해당 좌석 슬롯을 차지한 플레이어가 있는지 확인
        public static bool IsSeatTaken(string seat, int slot) => All.Exists(p => p.hallSeat == seat && p.hallSeatSlot == slot);

        // 요청 값과 중복을 검사한 뒤 플레이어에게 좌석 슬롯 배정
        [Command] public void CmdSit(string seat, int slot)
        {
            if (NetworkManager.singleton is GameNetworkManager room) Reply(room.Sit(this, seat, slot));
        }
        [Server] public void AssignSeat(string seatId, int slot) { hallSeatSlot = slot; hallSeat = seatId; }
        // 서버에 일어서기 요청
        [Command] public void CmdStand() => StandUp();
        // 좌석 정보를 비워 일어선 상태로 만듦
        [Server] public void StandUp() { hallSeat = ""; hallSeatSlot = 0; }

        // 로컬이면 좌석 변경 이벤트 발생, 원격이면 캐릭터 착석 자세 갱신
        void OnHallSeatChanged(string _, string seat)
        {
            if (isLocalPlayer) LocalSeatChanged?.Invoke(seat, hallSeatSlot);
            else if (character) character.SetSeated(!string.IsNullOrEmpty(seat));
        }

        // 로컬 포트폴리오·베팅·입찰 정보 초기화
        public override void OnStopLocalPlayer() { pendingReplies.Clear(); ApplyLocalState(null, null, 0, 0, new Info[0]); base.OnStopLocalPlayer(); }

        [Command] public void CmdReturnToLobby()
        {
            if (NetworkManager.singleton is GameNetworkManager room) Reply(room.ReturnToLobby(this));
        }

        [Command] public void CmdEmote(int number)
        {
            if (inMatch && number >= 0 && number <= 4) RpcEmote(number);
        }
        [ClientRpc] private void RpcEmote(int number)
        {
            if (isLocalPlayer || !character) return;
            if (number == 0) { character.StopAction(); character.SetSeated(IsSeated); return; }
            var actions = new[] { BBBWolfCharacter.Action.Defeat, BBBWolfCharacter.Action.Flair,
                BBBWolfCharacter.Action.HandRaising, BBBWolfCharacter.Action.HipHopDancing };
            character.PlayAction(actions[number - 1]);
        }
        [Command] public void CmdJump() { if (inMatch && !IsSeated) RpcJump(); }
        [ClientRpc] private void RpcJump() { if (!isLocalPlayer && character) character.PlayAction(BBBWolfCharacter.Action.Jump); }
        [Server] public void ShowBid() => RpcBid();
        [ClientRpc] private void RpcBid() { if (character) character.PlayAction(BBBWolfCharacter.Action.Bid); }
    }
}
