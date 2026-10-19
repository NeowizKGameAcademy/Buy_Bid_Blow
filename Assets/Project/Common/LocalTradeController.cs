using BuyBidBlow.Characters;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.Market;
using UnityEngine;
using UnityEngine.Serialization;
using BuyBidBlow.Systems.Market;

namespace BuyBidBlow.Common
{
    // Offline trading against the Dev sandbox MatchManager (Move & Trade scene, editor only).
    // Real matches trade through PlayerNet commands on the server instead.
    public class LocalTradeController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TradePanelController tradePanel;
        [FormerlySerializedAs("wallet")]
        [SerializeField] private PlayerAssetStatus assets;
        [SerializeField] private PlayerInfo playerInfo;
        [SerializeField] private string localPlayerId = "local-player";
        private ILocalMatch matchManager;

        private Company selectedCompany;
        private bool hasSelectedCompany;
        private bool joined;

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

            // The match may start after this component (scene load order); it binds again on first use.
            ResolveMatchManager(false);
        }

        private void OnDisable()
        {
            tradePanel.BuyRequested -= Buy;
            tradePanel.SellRequested -= Sell;

            if (matchManager != null)
                matchManager.RoundAdvanced -= OnRoundAdvanced;
            matchManager = null;
            joined = false;
        }

        public void OpenPanel(Company company)
        {
            if (!ResolveMatchManager())
                return;

            selectedCompany = company;
            hasSelectedCompany = true;

            tradePanel.Open(company, GetDisplayName(company), matchManager.GetPrice(company));

            ShowResult("거래할 수량을 선택하세요.");
        }

        private bool Validate(Company requestedCompany, int quantity)
        {
            if (!tradePanel.IsOpen || !hasSelectedCompany || requestedCompany != selectedCompany || quantity <= 0 || !ResolveMatchManager())
            {
                ShowResult("잘못된 거래 요청입니다.");
                return false;
            }

            if (matchManager.TradeService == null)
            {
                ShowResult("거래 서버가 준비되지 않았습니다.");
                return false;
            }
            return true;
        }

        private void Buy(Company requestedCompany, int quantity)
        {
            if (!Validate(requestedCompany, quantity)) return;
            ApplyResult(matchManager.TradeService.TryBuy(CurrentPlayerId, requestedCompany, quantity));
        }

        private void Sell(Company requestedCompany, int quantity)
        {
            if (!Validate(requestedCompany, quantity)) return;
            ApplyResult(matchManager.TradeService.TrySell(CurrentPlayerId, requestedCompany, quantity));
        }

        private void ApplyResult(TradeResult result)
        {
            assets.ApplyServerState(result);
            ShowResult(result.Message);
        }

        private PlayerId CurrentPlayerId => playerInfo != null
            ? playerInfo.ServerId
            : new PlayerId(localPlayerId);

        private void ShowResult(string message)
        {
            tradePanel.SetLog($"{message}\n보유: {assets.GetShares(selectedCompany):N0}개");
        }

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

        private void OnRoundAdvanced(int _, System.Collections.Generic.IReadOnlyList<PriceChange> __)
        {
            if (hasSelectedCompany && tradePanel.IsOpen)
                tradePanel.SetItemDetails(GetDisplayName(selectedCompany), matchManager.GetPrice(selectedCompany));
        }

        private static string GetDisplayName(Company company)
        {
            return company.ToString().ToUpperInvariant();
        }
    }
}
