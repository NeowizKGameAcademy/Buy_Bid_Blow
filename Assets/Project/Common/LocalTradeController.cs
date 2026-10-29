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
        private TradingPanelData displayedData;
        private bool hasDisplayedData;

        private static bool Networked => NetworkClient.active && MatchNet.Current != null;

        // 참조를 확인하고 패널·서버 이벤트를 구독한 뒤 초기 포트폴리오를 반영
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
            MatchNet.RoundTurned += OnRoundTurned;
            // The starting portfolio may have arrived while this was still disabled.
            if (PlayerNet.LocalPortfolio != null)
                assets.ApplyServerState(PlayerNet.LocalPortfolio);

            // The match may start after this component (scene load order); it binds again on first use.
            if (!Networked)
                ResolveMatchManager(false);
        }

        // 이벤트 구독을 해제하고 매치 연결 상태를 초기화
        private void OnDisable()
        {
            tradePanel.BuyRequested -= Buy;
            tradePanel.SellRequested -= Sell;
            tradePanel.BetRequested -= Bet;
            PlayerNet.LocalReply -= OnServerReply;
            PlayerNet.LocalPortfolioChanged -= OnServerPortfolio;
            MatchNet.RoundTurned -= OnRoundTurned;

            if (matchManager != null)
                matchManager.RoundAdvanced -= OnRoundAdvanced;
            matchManager = null;
            joined = false;
            BindMatch(null);
        }

        // 거래 가능 상태면 선택 회사를 기억하고 거래 패널을 염
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
            hasDisplayedData=false;
        }

        // 회사의 보유량·가격·재고·베팅 상태로 패널 데이터를 구성
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
                var snapshot = PlayerNet.LocalPortfolio;
                data.owned = snapshot?.GetShares(company) ?? 0;
                data.available = snapshot?.Available ?? 0;
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

        // 거래 요청과 연결 상태가 유효한지 검사하고 실패 시 메시지 표시
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

        // 매수 요청을 전달
        private void Buy(Company requestedCompany, int quantity) => Request(requestedCompany, quantity, true);

        // 매도 요청을 전달
        private void Sell(Company requestedCompany, int quantity) => Request(requestedCompany, quantity, false);

        // 서버에 거래 명령을 보내거나 샌드박스 TradeService로 바로 거래
        private void Request(Company requestedCompany, int quantity, bool buy)
        {
            if (!Validate(requestedCompany, quantity)) return;

            if (Networked)
            {
                ShowResult("거래 요청 중...");
                NetworkClient.localPlayer.GetComponent<PlayerNet>().CmdTrade(requestedCompany, quantity, buy);
                return;
            }

            var service = matchManager.TradeService;
            ApplyResult(buy
                ? service.TryBuy(CurrentPlayerId, requestedCompany, quantity)
                : service.TrySell(CurrentPlayerId, requestedCompany, quantity));
        }

        // 서버에 하락 베팅 토글 명령을 전송
        // Joining or cancelling a downside bet on the selected company (server match only).
        private void Bet(Company requestedCompany)
        {
            if (!Networked || !Validate(requestedCompany, 1)) return;
            ShowResult("베팅 요청 중...");
            NetworkClient.localPlayer.GetComponent<PlayerNet>().CmdToggleBet(requestedCompany);
        }

        // 패널이 열려 있으면 서버 응답 메시지를 표시
        // The server sends the new portfolio before its reply, so the panel already shows the result.
        private void OnServerReply(RequestResult result)
        {
            if (hasSelectedCompany && tradePanel.IsOpen && result.Company == selectedCompany &&
                (result.Kind == RequestKind.Buy || result.Kind == RequestKind.Sell || result.Kind == RequestKind.Bet))
                ShowResult(result.Message);
        }

        // 서버 포트폴리오를 자산에 반영하고 열린 패널을 갱신
        private void OnServerPortfolio(PortfolioSnapshot snapshot)
        {
            if (snapshot == null) { tradePanel.Close(); BindMatch(null); return; }
            RefreshOpenPanel();
        }

        // Public lists, phase and the private portfolio arrive separately; reconcile the open panel each frame.
        // RefreshOpenPanel repaints only when what it shows changed.
        private void Update()
        {
            if (!Networked || !tradePanel.IsOpen) return;
            if (match != MatchNet.Current) BindMatch(MatchNet.Current);
            RefreshOpenPanel();
        }

        // 성공한 거래 결과를 자산에 반영하고 패널과 결과 메시지를 갱신
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

        // 거래 패널 로그에 메시지를 표시
        private void ShowResult(string message) => tradePanel.SetLog(message);

        // 샌드박스 매치를 찾아 구독하고 처음 한 번 플레이어를 참가시킴
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

        private void BindMatch(MatchNet value) => match = value;

        // Prices change at each Closing Bell; refresh the open panel once the new prices arrive.
        private void OnRoundTurned(MatchNet.RoundTurn _) => RefreshOpenPanel();

        // 샌드박스 라운드가 넘어가면 열린 패널을 갱신
        private void OnRoundAdvanced(int _, IReadOnlyList<PriceChange> __) => RefreshOpenPanel();

        // 패널이 열려 있으면 선택 회사 데이터로 다시 표시
        private void RefreshOpenPanel()
        {
            if (hasSelectedCompany && tradePanel.IsOpen)
            {
                var current=PanelData(selectedCompany);
                if(hasDisplayedData && current.Equals(displayedData))return;
                displayedData=current;hasDisplayedData=true;
                tradePanel.Present(current);
            }
        }
    }
}
