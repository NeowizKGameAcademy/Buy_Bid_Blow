using System;
using System.Collections.Generic;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.Characters
{
    public sealed class PlayerNet : NetworkBehaviour
    {
        [SyncVar] public int seat;
        [SyncVar] public bool ready;
        [SyncVar(hook = nameof(OnAppearanceChanged))] public int top;
        [SyncVar(hook = nameof(OnAppearanceChanged))] public int bottom;
        [SyncVar(hook = nameof(OnAppearanceChanged))] public int tie;
        [SyncVar] public string nickname;
        // Set by the server when the match starts. In the level the owner plays through the local Player
        // and its position is synced (NetworkTransform, client to server); other clients see this avatar.
        [SyncVar(hook=nameof(OnInMatchChanged))] public bool inMatch;
        [SyncVar(hook = nameof(OnWalkingChanged))] bool syncedWalking;
        [SyncVar(hook = nameof(OnGroundedChanged))] bool syncedGrounded;
        [SyncVar] float syncedLookPitch;
        [SyncVar] float syncedLookYaw;
        // The player object lives from connection through lobby and match, across scene loads.
        public override void OnStartServer(){base.OnStartServer();DontDestroyOnLoad(gameObject);SetGameplayActive(false);}
        public override void OnStartClient(){base.OnStartClient();DontDestroyOnLoad(gameObject);All.Add(this);RefreshAvatar();}
        public override void OnStopClient(){All.Remove(this);base.OnStopClient();}
        public static readonly List<PlayerNet> All = new List<PlayerNet>();
        void OnInMatchChanged(bool _,bool value)=>RefreshAvatar();
        void OnWalkingChanged(bool _, bool value)
        {
            if (!isLocalPlayer) SetWalkingAnimationLocal(value);
        }
        void OnGroundedChanged(bool _, bool value)
        {
            if (isLocalPlayer) return;
            var animator = GetComponentInChildren<Animator>(true);
            if (animator) animator.SetBool("isGroud", value);
        }
        void OnAppearanceChanged(int _, int __) => ApplyAppearance();
        void RefreshAvatar()
        {
            if (isLocalPlayer)
                SetGameplayActive(inMatch);
            else
                SetGameplayActive(false);
        }
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();RefreshAvatar();BuyBidBlow.UI.Menu.GameMenuController.Ensure();
            if(NetworkManager.singleton is GameNetworkManager room)room.NotifyLocalPlayerReady();
        }
        // The avatar model faces the root's -Z (the lobby lines them up facing the camera with no rotation),
        // so in the level it turns around to face where the local player faces.
        public static readonly Quaternion ModelYaw = Quaternion.Euler(0, 180, 0);
        public static readonly Color[] Colors = {new Color(.6f,.12f,.2f), new Color(.1f,.5f,.7f), new Color(.8f,.6f,.12f), new Color(.2f,.65f,.35f), new Color(.55f,.3f,.7f), Color.black};
        BBBWolfAppearance appearance;
        BBBWolfCharacter character;
        PlayerMove movementSource;
        float nextLookSend;
        bool sentLook;
        float lastLookPitch;
        float lastLookYaw;
        const int LocalAvatarLayer = 30; // HideFromMain
        // The furnished hall uses +Z for the under-mezzanine matching carpet.
        public static readonly Vector3 WaitingCenter = new Vector3(0, .04f, 9.6f);
        // Player.prefab's wolf mesh faces +Z, unlike the legacy PlayerNet prefab.
        public static Quaternion WaitingRotation => Quaternion.Euler(0, 180, 0);
        public static Vector3 SeatPosition(int index)=>WaitingCenter+Vector3.right*((index-2.5f)*2.1f);
        void Awake()
        {
            TryGetComponent(out movementSource);
            appearance = GetComponentInChildren<BBBWolfAppearance>(true);
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            character=GetComponentInChildren<BBBWolfCharacter>(true);
            if(character) character.StopAction();
        }
        void Update()
        {
            if (!isClient || !inMatch) return;
            if (isLocalPlayer)
            {
                if (!movementSource) return;
                float pitch = movementSource.LookPitch;
                float yaw = movementSource.LookYaw;
                if (Time.unscaledTime < nextLookSend) return;
                nextLookSend = Time.unscaledTime + .05f;
                if (sentLook && Mathf.Abs(pitch-lastLookPitch)<.1f && Mathf.Abs(yaw-lastLookYaw)<.1f) return;
                sentLook = true;
                lastLookPitch = pitch;
                lastLookYaw = yaw;
                CmdSyncLook(pitch, yaw);
            }
            else
            {
                ApplyRemoteLocomotion(syncedWalking, syncedGrounded);
            }
        }
        [Command(channel = Channels.Unreliable)] void CmdSyncLook(float pitch, float yaw)
        {
            syncedLookPitch = Mathf.Clamp(pitch,-60,40);
            syncedLookYaw = Mathf.Clamp(yaw,-90,90);
        }
        void ApplyRemoteLocomotion(bool walking, bool grounded)
        {
            var animator = GetComponentInChildren<Animator>(true);
            if (!animator) return;
            animator.SetBool("isWalking", walking && !IsSeated);
            animator.SetBool("isGroud", grounded);
        }
        void LateUpdate()
        {
            if(inMatch)
            {
                FollowInMatch();
                if (!isLocalPlayer && movementSource)
                    movementSource.ApplyNetworkLook(syncedLookPitch,syncedLookYaw);
                return;
            }
            transform.SetPositionAndRotation(SeatPosition(seat), WaitingRotation);
            transform.localScale=Vector3.one*1.5f;
            ApplyAppearance();
        }
        void FollowInMatch()
        {
            transform.localScale=Vector3.one;
            if(isLocalPlayer)
                return;
            ApplyAppearance();
        }
        [Command] void CmdSetWalking(bool walking) => syncedWalking = walking;
        public void SetWalkingAnimation(bool walking)
        {
            if (!isLocalPlayer) return;
            CmdSetWalking(walking);
        }
        public void SetGroundedAnimation(bool grounded)
        {
            if (isLocalPlayer) CmdSetGrounded(grounded);
        }
        [Command] void CmdSetGrounded(bool grounded) => syncedGrounded = grounded;
        public void CancelEmotion(bool walking)
        {
            if (isLocalPlayer) CmdCancelEmotion(walking);
        }
        [Command] void CmdCancelEmotion(bool walking)
        {
            syncedWalking = walking;
            RpcCancelEmotion(walking);
        }
        [ClientRpc] void RpcCancelEmotion(bool walking)
        {
            if (TryGetComponent<BuyBidBlow.Audio.PlayerActionAudio>(out var audio)) audio.StopEmotion();
            if (isLocalPlayer) return;
            var animator = GetComponentInChildren<Animator>(true);
            if (!animator) return;
            animator.ResetTrigger("Defeat");
            animator.ResetTrigger("Flair");
            animator.ResetTrigger("Hadraise");
            animator.ResetTrigger("HipHopDance");
            animator.ResetTrigger("GolfDrive");
            animator.ResetTrigger("JumpingJacks");
            animator.ResetTrigger("LayingPose");
            animator.ResetTrigger("HurricaneKick");
            animator.Play(walking ? "Player_Walking" : "Player_Idle", 0, 0);
        }
        void SetWalkingAnimationLocal(bool walking)
        {
            var animator = GetComponentInChildren<Animator>(true);
            if (animator) animator.SetBool("isWalking", walking);
        }
        public void PlayAnimationTrigger(string trigger)
        {
            if (!isLocalPlayer) return;
            CmdPlayAnimationTrigger(trigger);
        }
        public void PlayAnimationTrigger(int trigger)
        {
            if (!isLocalPlayer) return;
            CmdPlayAnimationTriggerHash(trigger);
        }
        [Command] void CmdPlayAnimationTrigger(string trigger)
        {
            if (trigger != "Jump" && trigger != "Defeat" && trigger != "Flair" && trigger != "Hadraise" && trigger != "HipHopDance"
                && trigger != "GolfDrive" && trigger != "JumpingJacks" && trigger != "LayingPose" && trigger != "HurricaneKick") return;
            RpcPlayAnimationTrigger(Animator.StringToHash(trigger));
        }
        [Command] void CmdPlayAnimationTriggerHash(int trigger)
        {
            if (trigger != Animator.StringToHash("Jump")) return;
            RpcPlayAnimationTrigger(trigger);
        }
        static readonly string[] Emotions = { "Defeat", "Flair", "Hadraise", "HipHopDance", "GolfDrive", "JumpingJacks", "LayingPose", "HurricaneKick" };
        [ClientRpc] void RpcPlayAnimationTrigger(int trigger)
        {
            // The accepted jump or emotion reaches the owner and observers once through the existing RPC.
            if (TryGetComponent<BuyBidBlow.Audio.PlayerActionAudio>(out var audio))
            {
                if (trigger == Animator.StringToHash("Jump")) audio.PlayJump();
                foreach (var emotion in Emotions)
                    if (trigger == Animator.StringToHash(emotion)) audio.PlayEmotion(emotion);
            }
            if (isLocalPlayer) return;
            var animator = GetComponentInChildren<Animator>(true);
            if (animator) animator.SetTrigger(trigger);
        }
        void ApplyAppearance()
        {
            if (!appearance) return;
            appearance.SetOutfit(top, bottom);
            appearance.SetTieColor(Colors[Mathf.Clamp(tie, 0, Colors.Length - 1)]);
        }

        void RefreshAvatarLayers(bool gameplayActive)
        {
            // Player.prefab uses Wolf_Model directly and need not contain
            // BBBWolfCharacter. Include inactive outfits so customization keeps
            // the same visibility when it enables another mesh.
            int layer = isLocalPlayer && gameplayActive ? LocalAvatarLayer : 0;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
                    continue;
                if (renderer.GetComponentInParent<Canvas>(true))
                    continue;
                renderer.gameObject.layer = layer;
            }
        }

        // Player.prefab is both the Mirror player object and the local gameplay
        // rig. Only its owner may have a camera, HUD or input enabled.
        public void SetGameplayActive(bool active)
        {
            if (!isLocalPlayer) active = false;
            if (TryGetComponent(out PlayerCamera playerCamera)) playerCamera.SetEmotionView(false);
            RefreshAvatarLayers(active);
            // Cameras must be disabled on the GameObject as well as on their
            // component. This prevents a remote player's culling mask from
            // contributing to the local Game view for even one frame.
            foreach (var camera in GetComponentsInChildren<Camera>(true))
            {
                if (active)
                    camera.cullingMask = (camera.cullingMask | 1) & ~(1 << LocalAvatarLayer);
                camera.enabled = active;
                camera.gameObject.SetActive(active);
            }
            foreach (var listener in GetComponentsInChildren<AudioListener>(true))
                listener.enabled = active;
            if (TryGetComponent(out PlayerController controller)) controller.enabled = active;
            if (active && TryGetComponent(out PlayerMove localMovement))
                SetGroundedAnimation(localMovement.IsGrounded);
            if (TryGetComponent(out PlayerMove movement))
            {
                movement.enabled = active;
                if (!active) movement.SetMoveInput(Vector2.zero);
            }
            if (TryGetComponent(out PlayerInteraction interaction)) interaction.enabled = active;
            if (TryGetComponent(out BuyBidBlow.Common.LocalTradeController trading)) trading.enabled = active;
            if (TryGetComponent(out SeatController seats)) seats.enabled = active;
            if (TryGetComponent(out UnityEngine.InputSystem.PlayerInput input)) input.enabled = active;
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(active);
        }
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
            ClearLocalResults();
            localInformation.Clear(); LocalInformationChanged?.Invoke();
        }
        // The server's answer to this client's last trade or bet request.
        public static event Action<string> LocalReply;
        public static event Action<BuyBidBlow.Common.MatchNotice> LocalNotice;
        public static event Action<BuyBidBlow.Common.AuctionOutcome> LocalAuctionOutcome;
        public static event Action LocalResultsChanged;
        public static BuyBidBlow.Common.FinalStanding[] LocalResults { get; private set; }
        public static int LocalResultRounds { get; private set; }
        [TargetRpc] public void TargetNotice(BuyBidBlow.Common.MatchNotice notice) => LocalNotice?.Invoke(notice);
        [TargetRpc] public void TargetAuctionOutcome(BuyBidBlow.Common.AuctionOutcome outcome) => LocalAuctionOutcome?.Invoke(outcome);
        [TargetRpc] public void TargetResults(BuyBidBlow.Common.FinalStanding[] rows, int rounds)
        { LocalResults = rows; LocalResultRounds = rounds; LocalResultsChanged?.Invoke(); }
        public void ClearLocalResults() { LocalResults = null; LocalResultRounds = 0; }
        [Command] public void CmdReturnToLobby()
        { if(NetworkManager.singleton is GameNetworkManager room) room.RequestReturnToLobby(this); }

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
            var animator = GetComponentInChildren<Animator>(true);
            if (!animator) return;
            animator.SetBool("isWalking", false);
            animator.Play(string.IsNullOrEmpty(seat) ? "Player_Idle" : "Seated", 0, 0);
        }

        public override void OnStopLocalPlayer() { ClearLocalResults(); LocalPortfolio = null; LocalBet = null; LocalBetStake = 0; LocalSecretBid = 0; localInformation.Clear(); LocalInformationChanged?.Invoke(); LocalPortfolioChanged?.Invoke(null); base.OnStopLocalPlayer(); }
    }
}
