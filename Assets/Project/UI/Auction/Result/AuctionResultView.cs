using BuyBidBlow.UI.Shared;
namespace BuyBidBlow.UI.Auction
{
    public sealed class AuctionResultView : ModalView
    {
        // 공개 경매 낙찰 결과를 낙찰자·가격과 함께 표시
        public void ShowPublic(string winner, int price, bool isLocalWinner)
        {
            SetButtons(isLocalWinner ? new[] { "정보 확인", "닫기" } : new[] { "닫기" }); EscapeAction = -1;
            Show("낙찰", $"{winner} · ${price:N0}\n정보는 낙찰자만 확인할 수 있습니다.");
        }
        // 비공개 경매에서 내 낙찰 여부와 입찰금만 표시
        // Deliberately accepts no other player's identity, amount or result.
        public void ShowSecret(bool won, int ownBid)
        {
            SetButtons(won ? new[] { "정보 확인", "닫기" } : new[] { "닫기" }); EscapeAction = -1;
            Show(won ? "낙찰 성공" : "미낙찰", won ? $"내 입찰금 ${ownBid:N0}\n획득한 정보는 나만 확인할 수 있습니다." : "이번 상품을 낙찰받지 못했습니다.");
        }
        // 입찰이 없어 유찰된 결과 표시
        public void ShowUnsold() { SetButtons("닫기"); Show("유찰", "입찰이 없어 상품이 판매되지 않았습니다."); }
    }
}
