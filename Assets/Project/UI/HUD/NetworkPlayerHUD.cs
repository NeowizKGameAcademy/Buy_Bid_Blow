using System;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Round;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.HUD
{
    // Presentation only. Public match state and the owner's private snapshot have separate sources.
    public sealed class NetworkPlayerHUD : MonoBehaviour
    {
        public Text roundLabel, timeLabel, phaseLabel, cashLabel, reservedLabel, totalLabel;
        public Text[] holdings;
        public RoundStartView roundStart;
        MatchNet bound;
        int announcedRound;
        bool roundPending;

        void OnEnable()
        {
            PlayerNet.LocalPortfolioChanged += OnPortfolioChanged;
            if (roundStart) roundStart.Hide();
            Refresh();
        }

        void OnDisable()
        {
            PlayerNet.LocalPortfolioChanged -= OnPortfolioChanged;
            Bind(null);
            if (roundStart) roundStart.Hide();
        }

        void OnPortfolioChanged(BuyBidBlow.Systems.Portfolio.PortfolioSnapshot _) => Refresh();
        void Update() => Refresh();

        void Bind(MatchNet match)
        {
            if (ReferenceEquals(bound, match)) return;
            if (bound) bound.RoundStarted -= OnRoundStarted;
            bound = match;
            announcedRound = 0;
            roundPending = match != null;
            if (bound) bound.RoundStarted += OnRoundStarted;
        }

        // Defer presentation until Update: the remaining SyncVars (including auction state)
        // may still be deserializing while the round hook runs.
        void OnRoundStarted(int _) { roundPending = true; }

        public void Refresh()
        {
            Bind(MatchNet.Current);
            var portfolio = PlayerNet.LocalPortfolio;
            cashLabel.text = portfolio == null ? "—" : $"${portfolio.Cash:N0}";
            reservedLabel.text = portfolio == null ? "자산 수신 대기" : $"입찰 묶임 ${portfolio.Reserved:N0} · 거래 가능 ${portfolio.Available:N0}";
            for (int i = 0; i < holdings.Length; i++)
                holdings[i].text = portfolio == null ? "—" : portfolio.GetShares((Company)i).ToString();
            long total = portfolio?.Cash ?? 0;
            bool pricesReady = bound && bound.prices.Count >= holdings.Length;
            if (portfolio != null && pricesReady)
                for (int i = 0; i < holdings.Length; i++) total += (long)portfolio.GetShares((Company)i) * bound.GetPrice((Company)i);
            totalLabel.text = portfolio != null && pricesReady ? $"${total:N0}" : "—";
            if (!bound)
            {
                roundLabel.text = "경기 연결 대기"; timeLabel.text = "--:--"; phaseLabel.text = "대기 중";
                if (roundStart) roundStart.Hide();
                return;
            }
            roundLabel.text = $"{bound.round} / {bound.totalRounds} ROUND";
            int seconds = bound.phase == MatchNet.Phase.Finished ? 0 : (int)Math.Ceiling(bound.SecondsLeft);
            timeLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
            phaseLabel.text = bound.phase == MatchNet.Phase.Preparing ? "경기 준비" : bound.phase == MatchNet.Phase.Finished ? "경기 종료" : bound.auctionStage == MatchNet.AuctionStage.Open ? "공개 경매" : bound.auctionStage == MatchNet.AuctionStage.Secret ? "비공개 경매" : "주식 거래";
            if (bound.phase == MatchNet.Phase.Playing && bound.round > 0 && (roundPending || announcedRound != bound.round))
            {
                roundPending = false;
                announcedRound = bound.round;
                if (roundStart) roundStart.Show(bound.round, bound.totalRounds, bound.auctionStage != MatchNet.AuctionStage.None);
            }
            if (bound.phase == MatchNet.Phase.Finished && roundStart) roundStart.Hide();
        }
    }
}
