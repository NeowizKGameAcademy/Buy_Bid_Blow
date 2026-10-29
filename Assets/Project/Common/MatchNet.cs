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
        [SyncVar] public int minPrice;
        // Last closed round and the changes applied at its Closing Bell (0 = none yet).
        public readonly SyncList<PriceChange> lastChanges = new SyncList<PriceChange>();
        [SyncVar(hook = nameof(OnClosingBell))] public int lastClosedRound;
        [SyncVar(hook = nameof(OnPhaseChanged))] public Phase phase;
        // 0 while preparing; stays at the last round once finished.
        [SyncVar(hook = nameof(OnRoundChanged))] public int round;
        // Current price and remaining market stock per company, indexed by Company.
        public readonly SyncList<int> prices = new SyncList<int>();
        public readonly SyncList<int> stock = new SyncList<int>();
        // Flat rows: Straw, Timber, Brick. Initial price followed by each revealed closing price.
        // Replicated in full for clients that load the hall after a price update.
        public readonly SyncList<int> priceHistory = new SyncList<int>();
        public readonly SyncList<MatchStanding> results = new SyncList<MatchStanding>();

        // Mezzanine auction, public part only. The lot shows rounds and companies; directions go to the winner alone.
        public enum AuctionStage { None, Open, Secret }
        public struct PublicLot { public int firstRound, firstA, firstB, secondRound, secondA, secondB; }
        [SyncVar] public AuctionStage auctionStage;
        [SyncVar] public double auctionClosesAt;
        [SyncVar] public PublicLot auctionLot;
        // Open auction only (secret bids stay private): 0 = no bid yet.
        [SyncVar] public int highestBid;
        [SyncVar] public string highestBidder;
        [SyncVar] public string highestBidderId;
        [SyncVar] public string auctionResult;
        [SyncVar] public uint auctionResultVersion;
        public uint MarketVersion { get; private set; }
        // Next round with an auction, 0 when none are left.
        [SyncVar] public int nextAuctionRound;

        public event Action<Phase> PhaseChanged;
        public event Action<int> RoundStarted;
        // Round that just closed and the changes applied at its Closing Bell.
        public event Action<int, PriceChange[]> ClosingBell;

        public double SecondsLeft => Math.Max(0, (phase == Phase.Preparing ? firstRoundStartsAt : roundClosesAt) - NetworkTime.time);
        // 회사의 현재 가격을 반환
        public int GetPrice(Company company) => prices[(int)company];
        // 회사의 남은 시장 재고를 반환
        public int GetStock(Company company) => stock[(int)company];

        // 서버에서 씬 전환 시 파괴되지 않도록 설정
        public override void OnStartServer() { base.OnStartServer(); DontDestroyOnLoad(gameObject); }

        // 씬 전환 시 유지되도록 하고 현재 매치로 등록
        public override void OnStartClient()
        {
            base.OnStartClient();
            DontDestroyOnLoad(gameObject);
            Current = this;
            prices.OnChange += OnMarketChanged;
            stock.OnChange += OnMarketChanged;
            MarketVersion++;
        }

        // 현재 매치 등록을 해제
        public override void OnStopClient()
        {
            prices.OnChange -= OnMarketChanged;
            stock.OnChange -= OnMarketChanged;
            if (Current == this) Current = null;
            base.OnStopClient();
        }
        private void OnMarketChanged(SyncList<int>.Operation operation, int index, int value) => MarketVersion++;

        // 마감된 라운드의 가격 변동을 로그로 남기고 ClosingBell 이벤트 발생
        void OnClosingBell(int _, int closedRound)
        {
            if (closedRound <= 0) return;
            var changes = new PriceChange[lastChanges.Count];
            lastChanges.CopyTo(changes, 0);
            Debug.Log($"[Match] Closing Bell R{closedRound}: {string.Join(", ", Array.ConvertAll(changes, c => $"{c.Company} {c.Amount:+0;-0;0}"))}");
            ClosingBell?.Invoke(closedRound, changes);
        }

        // 단계 변경을 로그로 남기고 PhaseChanged 이벤트 발생
        void OnPhaseChanged(Phase _, Phase value)
        {
            Debug.Log($"[Match] Phase {value}");
            PhaseChanged?.Invoke(value);
        }

        // 새 라운드 시작을 로그로 남기고 RoundStarted 이벤트 발생
        void OnRoundChanged(int _, int value)
        {
            if (value <= 0) return;
            Debug.Log($"[Match] Round {value} started, {SecondsLeft:F0}s left");
            RoundStarted?.Invoke(value);
        }
    }
}
