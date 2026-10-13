using UnityEngine;

namespace BuyBidBlow
{
    // 임시 로컬 거래: 보유 수량과 NPC 재고는 실행 중에만 유지됩니다.
    public class LocalTradeController : MonoBehaviour
    {
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private PlayerWallet wallet;

        [Header("Test Item")]
        [SerializeField] private int itemId = 1;
        [SerializeField] private string itemName = "BRICK";
        [SerializeField, Min(1)] private int unitPrice = 20;
        [SerializeField, Min(0)] private int stock = 100;
        [SerializeField, Min(0)] private int ownedQuantity;

        public int OwnedQuantity => ownedQuantity;
        public int Stock => stock;

        private void OnEnable()
        {
            tradePanel.BuyRequested += Buy;
            tradePanel.SellRequested += Sell;
        }

        private void OnDisable()
        {
            tradePanel.BuyRequested -= Buy;
            tradePanel.SellRequested -= Sell;
        }

        public void OpenPanel()
        {
            tradePanel.Open(itemId, itemName, unitPrice);
            ShowResult("거래할 수량을 선택하세요.");
        }

        private bool Validate(int requestedItemId, int quantity, out int total)
        {
            total = 0;
            if (!tradePanel.IsOpen || requestedItemId != itemId || quantity <= 0 || unitPrice <= 0)
            {
                ShowResult("잘못된 거래 요청입니다.");
                return false;
            }

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
            if (quantity > stock)
            {
                ShowResult("NPC 재고가 부족합니다.");
                return;
            }
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
            stock -= quantity;
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
            if ((long)wallet.Money + total > int.MaxValue || (long)stock + quantity > int.MaxValue)
            {
                ShowResult("거래 한도를 초과합니다.");
                return;
            }

            ownedQuantity -= quantity;
            stock += quantity;
            wallet.AddMoney(total);
            ShowResult($"판매 완료: {quantity:N0}개 / $ {total:N0}");
        }

        private void ShowResult(string message)
        {
            tradePanel.SetLog($"{message}\n보유: {ownedQuantity:N0}개 / 재고: {stock:N0}개");
        }
    }
}
