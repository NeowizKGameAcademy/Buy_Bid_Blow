using UnityEngine;
using BuyBidBlow.Systems.Types;

namespace BuyBidBlow
{
    // 임시 로컬 거래: 보유 수량과 NPC 재고는 실행 중에만 유지됩니다.
    public class LocalTradeController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MatchManager matchManager;

        // [SerializeField] private int stock = 100;
        [SerializeField] private int ownedQuantity;

        private Company selectedCompany;
        private bool hasSelectedCompany;

        public int OwnedQuantity => ownedQuantity;
        //public int Stock => stock;

        private void OnEnable()
        {
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
            tradePanel.Open((int)company, GetDisplayName(company), matchManager.GetPrice(company));
            ShowResult("거래할 수량을 선택하세요.");
        }

        private bool Validate(int requestedItemId, int quantity, out int total)
        {
            total = 0;
            if (!tradePanel.IsOpen || !hasSelectedCompany || requestedItemId != (int)selectedCompany || quantity <= 0 || !ResolveMatchManager())
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

        private void Buy(int requestedItemId, int quantity)
        {
            if (!Validate(requestedItemId, quantity, out int total)) return;
            //if (quantity > stock)
            //{
            //    ShowResult("NPC 재고가 부족합니다.");
            //    return;
            //}
            if ((long)ownedQuantity + quantity > int.MaxValue)
            {
                ShowResult("보유 수량 한도를 초과합니다.");
                return;
            }
            if (!wallet.TrySpendMoney(total))
            {
                ShowResult("돈이 부족합니다.");
                return;
            }

            ownedQuantity += quantity;
            //stock -= quantity;
            ShowResult($"구매 완료: {quantity:N0}개 / $ {total:N0}");
        }

        private void Sell(int requestedItemId, int quantity)
        {
            if (!Validate(requestedItemId, quantity, out int total)) return;
            if (quantity > ownedQuantity)
            {
                ShowResult("보유 수량이 부족합니다.");
                return;
            }
            //if ((long)wallet.Money + total > int.MaxValue || (long)stock + quantity > int.MaxValue)
            //{
            //    ShowResult("거래 한도를 초과합니다.");
            //    return;
            //}

            ownedQuantity -= quantity;
            //stock += quantity;
            wallet.AddMoney(total);
            ShowResult($"판매 완료: {quantity:N0}개 / $ {total:N0}");
        }

        private void ShowResult(string message)
        {
            tradePanel.SetLog($"{message}\n보유: {ownedQuantity:N0}개");
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
