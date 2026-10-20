using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Market;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace BuyBidBlow.Common
{
    // The local player's trading. In a match, requests go to the server's MatchEngine through
    // PlayerNet and only its answer changes the assets shown here. Without a network match it trades
    // against the Dev sandbox MatchManager (Move & Trade scene, editor only).
    public class LocalTradeController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TradePanelController tradePanel;
        [FormerlySerializedAs("wallet")]
        [SerializeField] private PlayerAssetStatus assets;
        [SerializeField] private PlayerInfo playerInfo;
        [SerializeField] private string localPlayerId = "local-player";
        private ILocalMatch matchManager;
        private MatchNet match;

        private Company selectedCompany;
        private bool hasSelectedCompany;
        private bool joined;

        private static bool Networked => NetworkClient.active && MatchNet.Current != null;

        private void OnEnable()
        {
            if (tradePanel == null || assets == null)
            {
                Debug.LogError("LocalTradeController needs TradePanelController and PlayerAssetStatus references.", this);
                enabled = false;
                return;
            }

            tradePanel.BuyRequested += Buy;
            tradePanel.SellRequested += Sell;
            tradePanel.BetRequested += Bet;
            PlayerNet.LocalReply += OnServerReply;
            PlayerNet.LocalPortfolioChanged += OnServerPortfolio;
            // The starting portfolio may have arrived while this was still disabled.
            if (PlayerNet.LocalPortfolio != null)
                assets.ApplyServerState(PlayerNet.LocalPortfolio);

            // The match may start after this component (scene load order); it binds again on first use.
            if (!Networked)
                ResolveMatchManager(false);
        }

        private void OnDisable()
        {
            tradePanel.BuyRequested -= Buy;
            tradePanel.SellRequested -= Sell;
            tradePanel.BetRequested -= Bet;
            PlayerNet.LocalReply -= OnServerReply;
            PlayerNet.LocalPortfolioChanged -= OnServerPortfolio;

            if (matchManager != null)
                matchManager.RoundAdvanced -= OnRoundAdvanced;
            matchManager = null;
            joined = false;
            BindMatch(null);
        }

        public void OpenPanel(Company company)
        {
            if (Networked)
            {
                BindMatch(MatchNet.Current);
                if (match.phase != MatchNet.Phase.Playing)
                    return;
            }
            else if (!ResolveMatchManager())
                return;

            selectedCompany = company;
            hasSelectedCompany = true;

            tradePanel.Open(company, PanelData(company));
        }

        // What the panel shows for one company, from the server-confirmed assets and the published price.
        private TradingPanelData PanelData(Company company)
        {
            var data = new TradingPanelData
            {
                company = company.ToString(),
                owned = assets.GetShares(company),
                available = assets.Available,
            };
            if (Networked)
            {
                data.price = match.GetPrice(company);
                data.stock = match.GetStock(company);
                foreach (var priceChange in match.lastChanges)
                    if (priceChange.Company == company) data.change += priceChange.Amount;
                data.closed = match.phase != MatchNet.Phase.Playing;
                data.hasBet = PlayerNet.LocalBet == company;
                data.otherBet = PlayerNet.LocalBet.HasValue && !data.hasBet;
                data.betStake = PlayerNet.LocalBetStake;
            }
            else
            {
                // The sandbox MatchManager has no supply limit or downside bets.
                data.price = matchManager.GetPrice(company);
                data.stock = -1;
                data.betUnavailable = true;
            }
            return data;
        }

        private bool Validate(Company requestedCompany, int quantity)
        {
            if (!tradePanel.IsOpen || !hasSelectedCompany || requestedCompany != selectedCompany || quantity <= 0)
            {
                ShowResult("잘못된 거래 요청입니다.");
                return false;
            }

            if (Networked)
            {
                if (NetworkClient.localPlayer == null)
                {
                    ShowResult("서버에 연결된 플레이어가 없습니다.");
                    return false;
                }
                return true;
            }

            if (!ResolveMatchManager() || matchManager.TradeService == null)
            {
                ShowResult("거래 서버가 준비되지 않았습니다.");
                return false;
            }
            return true;
        }

        private void Buy(Company requestedCompany, int quantity) => Request(requestedCompany, quantity, true);

        private void Sell(Company requestedCompany, int quantity) => Request(requestedCompany, quantity, false);

        private void Request(Company requestedCompany, int quantity, bool buy)
        {
            if (!Validate(requestedCompany, quantity)) return;

            if (Networked)
            {
                NetworkClient.localPlayer.GetComponent<PlayerNet>().CmdTrade(requestedCompany, quantity, buy);
                ShowResult("거래 요청 중...");
                return;
            }

            var service = matchManager.TradeService;
            ApplyResult(buy
                ? service.TryBuy(CurrentPlayerId, requestedCompany, quantity)
                : service.TrySell(CurrentPlayerId, requestedCompany, quantity));
        }

        // Joining or cancelling a downside bet on the selected company (server match only).
        private void Bet(Company requestedCompany)
        {
            if (!Networked || !Validate(requestedCompany, 1)) return;
            NetworkClient.localPlayer.GetComponent<PlayerNet>().CmdToggleBet(requestedCompany);
            ShowResult("베팅 요청 중...");
        }

        // The server sends the new portfolio before its reply, so the panel already shows the result.
        private void OnServerReply(string message)
        {
            if (hasSelectedCompany && tradePanel.IsOpen)
                ShowResult(message);
        }

        private void OnServerPortfolio(PortfolioSnapshot snapshot)
        {
            assets.ApplyServerState(snapshot);
            RefreshOpenPanel();
        }

        private void ApplyResult(TradeResult result)
        {
            // Rejections may carry no asset state, so only an accepted trade changes what is shown.
            if (result.Succeeded)
                assets.ApplyServerState(result);
            if (hasSelectedCompany && tradePanel.IsOpen)
            {
                tradePanel.Present(PanelData(selectedCompany));
                ShowResult(result.Message);
            }
        }

        private PlayerId CurrentPlayerId => playerInfo != null
            ? playerInfo.ServerId
            : new PlayerId(localPlayerId);

        private void ShowResult(string message) => tradePanel.SetLog(message);

        private bool ResolveMatchManager(bool warn = true)
        {
            if (matchManager == null)
            {
                matchManager = LocalMatch.Current;
                if (matchManager != null)
                    matchManager.RoundAdvanced += OnRoundAdvanced;
            }

            if (matchManager != null && matchManager.TradeService != null)
            {
                if (!joined)
                {
                    matchManager.AddPlayer(CurrentPlayerId);
                    assets.ApplyServerState(matchManager.TradeService.GetSnapshot(CurrentPlayerId));
                    joined = true;
                }
                return true;
            }

            if (warn) Debug.LogWarning("LocalTradeController needs the sandbox MatchManager in the scene.", this);
            return false;
        }

        private void BindMatch(MatchNet value)
        {
            if (match == value) return;
            if (match != null) match.ClosingBell -= OnClosingBell;
            match = value;
            if (match != null) match.ClosingBell += OnClosingBell;
        }

        // Prices change at each Closing Bell; refresh the open panel to the new round's price.
        private void OnClosingBell(int _, PriceChange[] __) => RefreshOpenPanel();

        private void OnRoundAdvanced(int _, IReadOnlyList<PriceChange> __) => RefreshOpenPanel();

        private void RefreshOpenPanel()
        {
            if (hasSelectedCompany && tradePanel.IsOpen)
                tradePanel.Present(PanelData(selectedCompany));
        }
    }
}
