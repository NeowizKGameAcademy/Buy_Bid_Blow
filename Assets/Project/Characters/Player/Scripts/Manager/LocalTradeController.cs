using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    public class LocalTradeController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerPortfolio portfolio;
        [SerializeField] private MatchManager matchManager;

        private Company selectedCompany;
        private bool hasSelectedCompany;

        private void OnEnable()
        {
            if (portfolio == null)
                portfolio = GetComponent<PlayerPortfolio>();

            if (portfolio == null)
            {
                Debug.LogError("LocalTradeController needs a PlayerPortfolio.", this);
                enabled = false;
                return;
            }

            tradePanel.BuyRequested += Buy;
            tradePanel.SellRequested += Sell;

            ResolveMatchManager();

            if (matchManager != null)
                matchManager.RoundAdvanced += OnRoundAdvanced;
        }

        private void OnDisable()
        {
            tradePanel.BuyRequested -= Buy;
            tradePanel.SellRequested -= Sell;

            if (matchManager != null)
                matchManager.RoundAdvanced -= OnRoundAdvanced;
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

        private bool Validate(Company requestedCompany, int quantity, out int total)
        {
            total = 0;

            if (!tradePanel.IsOpen || !hasSelectedCompany || requestedCompany != selectedCompany || quantity <= 0 || !ResolveMatchManager())
            {
                ShowResult("잘못된 거래 요청입니다.");
                return false;
            }

            int unitPrice = matchManager.GetPrice(selectedCompany);
            long price = (long)unitPrice * quantity;
            if (price > int.MaxValue)
            {
                ShowResult("거래 금액이 너무 큽니다.");
                return false;
            }

            total = (int)price;
            return true;
        }

        private void Buy(Company requestedCompany, int quantity)
        {
            if (!Validate(requestedCompany, quantity, out int total)) return;

            if ((long)portfolio.GetOwnedQuantity(requestedCompany) + quantity > int.MaxValue)
            {
                ShowResult("보유 수량 한도를 초과합니다.");
                return;
            }

            if (!wallet.TrySpendMoney(total))
            {
                ShowResult("돈이 부족합니다.");
                return;
            }

            portfolio.AddStock(requestedCompany, quantity);

            ShowResult($"구매 완료: {quantity:N0}개 / $ {total:N0}");
        }

        private void Sell(Company requestedCompany, int quantity)
        {
            if (!Validate(requestedCompany, quantity, out int total)) return;

            if (!portfolio.TryRemoveStock(requestedCompany, quantity))
            {
                ShowResult("보유 수량이 부족합니다.");
                return;
            }

            wallet.AddMoney(total);
            ShowResult($"판매 완료: {quantity:N0}개 / $ {total:N0}");
        }

        private void ShowResult(string message)
        {
            tradePanel.SetLog($"{message}\n보유: {portfolio.GetOwnedQuantity(selectedCompany):N0}개");
        }

        private bool ResolveMatchManager()
        {
            if (matchManager == null)
                matchManager = MatchManager.instance;

            if (matchManager != null)
                return true;

            Debug.LogError("LocalTradeController needs a MatchManager.", this);
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
