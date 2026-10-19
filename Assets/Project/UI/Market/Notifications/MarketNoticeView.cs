using BuyBidBlow.UI.Shared;
using UnityEngine;
namespace BuyBidBlow.UI.Market
{
    public sealed class MarketNoticeView : MonoBehaviour
    {
        public NotificationStackView Stack;
        public void TradeCompleted(string company, int quantity, int cashDelta, bool buy) => Stack.Push(buy ? "매수 완료" : "매도 완료", $"{company} {quantity}주 · {cashDelta:+$0;-$0;$0}", NoticeTone.Success);
        public void Rejected(string reason) => Stack.Push("요청을 처리할 수 없습니다", reason, NoticeTone.Failure);
        public void BetSettled(string company, int stake, int payout, bool won) => Stack.Push(won ? "하락 베팅 성공" : "하락 베팅 실패", won ? $"{company} · ${payout} 수령 · 순이익 ${payout-stake}" : $"{company} · 참가금 ${stake} 손실", won ? NoticeTone.Success : NoticeTone.Failure);
    }
}
