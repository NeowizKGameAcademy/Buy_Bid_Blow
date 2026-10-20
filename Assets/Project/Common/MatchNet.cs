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

        // Mirror applies SyncLists first, then SyncVars in declaration order, all from the same update.
        // So hooks see the prices and changes they announce, and the Closing Bell fires before the next round starts.
        // First round start while preparing. Server time (NetworkTime.time).
        [SyncVar] public double firstRoundStartsAt;
        [SyncVar] public double roundClosesAt;
        [SyncVar] public int totalRounds;
        // Last closed round and the changes applied at its Closing Bell (0 = none yet).
        public readonly SyncList<PriceChange> lastChanges = new SyncList<PriceChange>();
        [SyncVar(hook = nameof(OnClosingBell))] public int lastClosedRound;
        [SyncVar(hook = nameof(OnPhaseChanged))] public Phase phase;
        // 0 while preparing; stays at the last round once finished.
        [SyncVar(hook = nameof(OnRoundChanged))] public int round;
        // Current price and remaining market stock per company, indexed by Company.
        public readonly SyncList<int> prices = new SyncList<int>();
        public readonly SyncList<int> stock = new SyncList<int>();
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

        public event Action<Phase> PhaseChanged;
        public event Action<int> RoundStarted;
        // Round that just closed and the changes applied at its Closing Bell.
        public event Action<int, PriceChange[]> ClosingBell;

        public double SecondsLeft => Math.Max(0, (phase == Phase.Preparing ? firstRoundStartsAt : roundClosesAt) - NetworkTime.time);
        public int GetPrice(Company company) => prices[(int)company];
        public int GetStock(Company company) => stock[(int)company];

        public override void OnStartServer() { base.OnStartServer(); DontDestroyOnLoad(gameObject); }

        public override void OnStartClient()
        {
            base.OnStartClient();
            DontDestroyOnLoad(gameObject);
            Current = this;
        }

        public override void OnStopClient()
        {
            if (Current == this) Current = null;
            base.OnStopClient();
        }

        void OnClosingBell(int _, int closedRound)
        {
            if (closedRound <= 0) return;
            var changes = new PriceChange[lastChanges.Count];
            lastChanges.CopyTo(changes, 0);
            Debug.Log($"[Match] Closing Bell R{closedRound}: {string.Join(", ", Array.ConvertAll(changes, c => $"{c.Company} {c.Amount:+0;-0;0}"))}");
            ClosingBell?.Invoke(closedRound, changes);
        }

        void OnPhaseChanged(Phase _, Phase value)
        {
            Debug.Log($"[Match] Phase {value}");
            PhaseChanged?.Invoke(value);
        }

        void OnRoundChanged(int _, int value)
        {
            if (value <= 0) return;
            Debug.Log($"[Match] Round {value} started, {SecondsLeft:F0}s left");
            RoundStarted?.Invoke(value);
        }
    }
}
