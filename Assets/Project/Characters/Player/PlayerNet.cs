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
        // Set by the server when the match starts. In the level the owner plays through the local Player
        // and its position is synced (NetworkTransform, client to server); other clients see this avatar.
        [SyncVar(hook=nameof(OnInMatchChanged))] public bool inMatch;
        // The player object lives from connection through lobby and match, across scene loads.
        public override void OnStartServer(){base.OnStartServer();DontDestroyOnLoad(gameObject);}
        public override void OnStartClient(){base.OnStartClient();DontDestroyOnLoad(gameObject);All.Add(this);RefreshAvatar();}
        public override void OnStopClient(){All.Remove(this);base.OnStopClient();}
        public static readonly List<PlayerNet> All = new List<PlayerNet>();
        void OnInMatchChanged(bool _,bool value)=>RefreshAvatar();
        // The owner already sees its own wolf through the local Player, so its avatar hides in the level.
        void RefreshAvatar(){bool visible=!inMatch||!isLocalPlayer;foreach(Transform child in transform)child.gameObject.SetActive(visible);}
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();RefreshAvatar();
            if(NetworkManager.singleton is GameNetworkManager room)room.NotifyLocalPlayerReady();
        }
        // The avatar model faces the root's -Z (the lobby lines them up facing the camera with no rotation),
        // so in the level it turns around to face where the local player faces.
        public static readonly Quaternion ModelYaw = Quaternion.Euler(0, 180, 0);
        public static readonly Color[] Colors = {new Color(.6f,.12f,.2f), new Color(.1f,.5f,.7f), new Color(.8f,.6f,.12f), new Color(.2f,.65f,.35f), new Color(.55f,.3f,.7f), Color.black};
        BBBWolfAppearance appearance;
        BBBWolfCharacter character;
        PlayerMove localBody;
        Vector3 lastPosition;
        float walkSpeed;
        // The furnished hall uses +Z for the under-mezzanine matching carpet.
        public static readonly Vector3 WaitingCenter = new Vector3(0, .04f, 9.6f);
        public static Quaternion WaitingRotation => Quaternion.identity;
        public static Vector3 SeatPosition(int index)=>WaitingCenter+Vector3.right*((index-2.5f)*2.1f);
        void Awake()
        {
            appearance = GetComponentInChildren<BBBWolfAppearance>(true);
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            character=GetComponentInChildren<BBBWolfCharacter>(true);
            if(character) {character.StopAction();character.SetMovementSpeed(0);}
        }
        void LateUpdate()
        {
            if(inMatch){FollowInMatch();return;}
            transform.SetPositionAndRotation(SeatPosition(seat), WaitingRotation);
            transform.localScale=Vector3.one*1.5f;
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        void FollowInMatch()
        {
            transform.localScale=Vector3.one;
            if(isLocalPlayer)
            {
                if(!localBody)localBody=FindFirstObjectByType<PlayerMove>();
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
        [Command] public void SetReady(bool value) { ready = value; }
        // Only honored for the room host; the server checks readiness and player count.
        [Command] public void CmdStartMatch() { if (NetworkManager.singleton is GameNetworkManager room) room.RequestStartMatch(this); }
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
        static readonly List<Info> localInformation = new List<Info>();
        public static IReadOnlyList<Info> LocalInformation => localInformation;
        public static event Action LocalInformationChanged;
        [TargetRpc] public void TargetAwardInformation(Info first, Info second)
        {
            localInformation.Add(first); localInformation.Add(second);
            LocalInformationChanged?.Invoke();
        }
        [TargetRpc] public void TargetResetInformation()
        {
            localInformation.Clear(); LocalInformationChanged?.Invoke();
        }
        // The server's answer to this client's last trade or bet request.
        public static event Action<string> LocalReply;

        [Command] public void CmdTrade(Company company, int quantity, bool buy)
        {
            if (NetworkManager.singleton is GameNetworkManager room) TargetReply(room.Trade(connectionToClient, company, quantity, buy));
        }
        [Command] public void CmdToggleBet(Company company)
        {
            if (NetworkManager.singleton is GameNetworkManager room) TargetReply(room.ToggleBet(connectionToClient, company));
        }
        // Seated bid: amount to the running auction, or cancel this player's secret submission.
        [Command] public void CmdBid(int amount, bool cancel)
        {
            if (NetworkManager.singleton is GameNetworkManager room) TargetReply(room.Bid(connectionToClient, amount, cancel));
        }
        [TargetRpc] void TargetReply(string message) => LocalReply?.Invoke(message);

        // bet: the Company index of the open downside bet, or -1.
        [Server] public void SendPortfolio(PortfolioSnapshot snapshot, int bet, int betStake, int secretBid)
        {
            var companies = (Company[])Enum.GetValues(typeof(Company));
            var shares = new int[companies.Length];
            for (int i = 0; i < companies.Length; i++) shares[i] = snapshot.GetShares(companies[i]);
            TargetPortfolio(snapshot.Cash, snapshot.Reserved, shares, bet, betStake, secretBid);
        }
        [TargetRpc] void TargetPortfolio(int cash, int reserved, int[] shares, int bet, int betStake, int secretBid)
        {
            var byCompany = new Dictionary<Company, int>();
            for (int i = 0; i < shares.Length; i++) byCompany[(Company)i] = shares[i];
            LocalPortfolio = new PortfolioSnapshot(cash, reserved, byCompany);
            LocalBet = bet >= 0 ? (Company)bet : (Company?)null;
            LocalBetStake = betStake;
            LocalSecretBid = secretBid;
            LocalPortfolioChanged?.Invoke(LocalPortfolio);
        }
        // ---- Hall seats: the server hands out each seat slot to one player at a time. ----
        // Slot first: Mirror applies SyncVars in declaration order, so the seat hook sees its slot.
        [SyncVar] public int hallSeatSlot;
        // Seat.Id of the chair this player sits on, "" while standing.
        [SyncVar(hook = nameof(OnHallSeatChanged))] public string hallSeat = "";
        public bool IsSeated => !string.IsNullOrEmpty(hallSeat);
        // The owner's seat changed: the local Player sits down or stands up to match.
        public static event Action<string, int> LocalSeatChanged;

        public static bool IsSeatTaken(string seat, int slot) => All.Exists(p => p.hallSeat == seat && p.hallSeatSlot == slot);

        [Command] public void CmdSit(string seat, int slot)
        {
            // The server has no hall loaded, so it only keeps seats unique; reaching the seat is checked locally.
            if (!inMatch || string.IsNullOrEmpty(seat) || seat.Length > 64 || slot < 0 || slot > 7) return;
            foreach (var conn in NetworkServer.connections.Values)
                if (conn.identity && conn.identity.TryGetComponent<PlayerNet>(out var other) && other != this && other.hallSeat == seat && other.hallSeatSlot == slot) return;
            hallSeatSlot = slot; hallSeat = seat;
        }
        [Command] public void CmdStand() => StandUp();
        [Server] public void StandUp() { hallSeat = ""; hallSeatSlot = 0; }

        void OnHallSeatChanged(string _, string seat)
        {
            if (isLocalPlayer) LocalSeatChanged?.Invoke(seat, hallSeatSlot);
            else if (character) character.SetSeated(!string.IsNullOrEmpty(seat));
        }

        public override void OnStopLocalPlayer() { LocalPortfolio = null; LocalBet = null; LocalBetStake = 0; LocalSecretBid = 0; localInformation.Clear(); LocalInformationChanged?.Invoke(); LocalPortfolioChanged?.Invoke(null); base.OnStopLocalPlayer(); }
    }
}
