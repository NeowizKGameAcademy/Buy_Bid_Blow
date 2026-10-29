using System;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Common
{
    // Public match state, written by the server controller (GameNetworkManager) and read by clients.
    // Spawned when a match starts. Props and HUD subscribe to it instead of carrying network state;
    // private player data stays on PlayerNet.
    public sealed class MatchNet : NetworkBehaviour
    {
        public enum Phase { Preparing, Playing, Finished }

        public static MatchNet Current { get; private set; }

        // When things are due is MatchTimeline's job; events here say what has happened, once its public data has
        // arrived, so sound and screen move on the same signal.
        // First round start while preparing. Server time (NetworkTime.time).
        [SyncVar] public double firstRoundStartsAt;
        [SyncVar] public double roundClosesAt;
        [SyncVar] public int totalRounds;
        // Last closed round and the changes applied at its Closing Bell (0 = none yet).
        public readonly SyncList<PriceChange> lastChanges = new SyncList<PriceChange>();
        [SyncVar] public int lastClosedRound;
        [SyncVar(hook = nameof(OnPhaseChanged))] public Phase phase;
        // 0 while preparing; stays at the last round once finished.
        [SyncVar(hook = nameof(OnRoundChanged))] public int round;
        // Current price and remaining market stock per company, indexed by Company.
        public readonly SyncList<int> prices = new SyncList<int>();
        public readonly SyncList<int> stock = new SyncList<int>();
        public readonly SyncList<float> riseChances = new SyncList<float>();
        [SyncVar] public int minPrice;
        // Flat rows: Straw, Timber, Brick. Prices at -5..0 followed by each revealed closing price.
        // Replicated in full for clients that load the hall after a price update.
        public readonly SyncList<int> priceHistory = new SyncList<int>();

        // Mezzanine auction, public part only. The lot shows rounds and companies; directions go to the winner alone.
        public enum AuctionStage { None, Open, Secret }
        public struct PublicLot { public int firstRound, firstA, firstB, secondRound, secondA, secondB; }
        [SyncVar] public AuctionStage auctionStage;
        [SyncVar] public double auctionClosesAt;
        [SyncVar] public PublicLot auctionLot;
        // Open auction only (secret bids stay private): 0 = no bid yet.
        [SyncVar] public int highestBid;
        [SyncVar] public string highestBidder;
        // Next round with an auction, 0 when none are left.
        [SyncVar] public int nextAuctionRound;

        // One round boundary: a round's Closing Bell and the next round's start happen together.
        // Closed is 0 when the first round opens, Opened is 0 when the match ends; Changes are those applied at the bell.
        public struct RoundTurn { public int Closed, Opened; public PriceChange[] Changes; }

        // Raised on clients for changes seen while connected, never for the state a client joins into.
        // Static so listeners subscribe once whichever match is running.
        public static event Action<RoundTurn> RoundTurned;
        // Previous and current stage. Going straight from Open to Secret is one change.
        public static event Action<AuctionStage, AuctionStage> AuctionStageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; RoundTurned = null; AuctionStageChanged = null; }

        // What this client has already announced.
        int seenClosed, seenRound;
        AuctionStage seenStage;

        public double SecondsLeft => Math.Max(0, (phase == Phase.Preparing ? firstRoundStartsAt : roundClosesAt) - NetworkTime.time);
        public int GetPrice(Company company) => prices[(int)company];
        public int GetStock(Company company) => stock[(int)company];

        public override void OnStartServer() { base.OnStartServer(); DontDestroyOnLoad(gameObject); }

        public override void OnStartClient()
        {
            base.OnStartClient();
            DontDestroyOnLoad(gameObject);
            Current = this;
            seenClosed = lastClosedRound; seenRound = round; seenStage = auctionStage;
        }

        public override void OnStopClient()
        {
            if (Current == this) Current = null;
            base.OnStopClient();
        }

        // Compared here rather than in SyncVar hooks: a remote client applies fields in declaration order but the host
        // in the server's assignment order, so a hook could see half a turn. By Update the whole server update is in.
        void Update()
        {
            if (!isClient) return;
            if (auctionStage != seenStage)
            {
                var previous = seenStage;
                seenStage = auctionStage;
                AuctionStageChanged?.Invoke(previous, auctionStage);
            }
            if (lastClosedRound == seenClosed && round == seenRound) return;
            var turn = new RoundTurn
            {
                Closed = lastClosedRound != seenClosed ? lastClosedRound : 0,
                Opened = phase == Phase.Playing && round != seenRound ? round : 0,
                Changes = new PriceChange[lastClosedRound != seenClosed ? lastChanges.Count : 0]
            };
            if (turn.Closed > 0) lastChanges.CopyTo(turn.Changes, 0);
            seenClosed = lastClosedRound; seenRound = round;
            Debug.Log($"[Match] Round turned {turn.Closed} -> {turn.Opened}: {string.Join(", ", Array.ConvertAll(turn.Changes, c => $"{c.Company} {c.Amount:+0;-0;0}"))}");
            RoundTurned?.Invoke(turn);
        }

        void OnPhaseChanged(Phase _, Phase value) => Debug.Log($"[Match] Phase {value}");

        void OnRoundChanged(int _, int value)
        {
            if (value > 0) Debug.Log($"[Match] Round {value} started, {SecondsLeft:F0}s left");
        }
    }
}
