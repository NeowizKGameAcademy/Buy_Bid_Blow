using System;
using BuyBidBlow.Systems.Types;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Auction
{
    // What the seated player sees of the running auction. Other players' secret bids never reach it.
    public struct AuctionPanelData
    {
        public bool seated, live, secret, complete;
        public Lot lot;
        public double secondsLeft;
        // Open auction: the current highest bid. Secret auction: only this player's submission.
        public bool hasBid, ownHighest;
        public int highestBid, ownSubmitted;
        public string highestBidder;
        public int bidAmount, available;
        // Shown while seated with no auction running, e.g. the upcoming auction rounds.
        public string idleNote;
        // The answer to the player's last bid; replaces the rule line while set.
        public string notice;
    }

    // Presentation only: shows the auction header, bid controls and lot; never places bids.
    public sealed class AuctionPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject header;
        [SerializeField] private GameObject controls;
        [SerializeField] private Text lotDetails;

        public void Present(AuctionPanelData data)
        {
            header.SetActive(data.seated);
            controls.SetActive(data.seated && data.live);
            if (lotDetails)
                lotDetails.transform.parent.gameObject.SetActive(data.seated && data.live);

            if (data.live)
            {
                if (lotDetails)
                    lotDetails.text = $"방향 정보\n{Describe(data.lot.First)}\n{Describe(data.lot.Second)}\n낙찰자만 방향 확인 · I 내 정보";

                Set(header, "Stage", data.secret ? "2차 비공개 경매" : "1차 공개 경매");
                Set(header, "Highest Bid", data.secret ? "내 제출 $" + data.ownSubmitted
                    : data.hasBid ? $"최고 입찰 ${data.highestBid} · {data.highestBidder}" : "입찰 없음");
                Set(header, "Leading Status", data.secret ? "다른 입찰 비공개" : data.ownHighest ? "현재 최고 입찰 중" : "");
                Set(header, "Countdown", TimeText(data.secondsLeft));

                Set(controls, "Amount", "$" + data.bidAmount);
                Set(controls, "Cash After", data.ownHighest ? "거래 가능 $" + data.available
                    : "입찰 후 거래 가능 $" + (data.available + data.ownSubmitted - data.bidAmount));
                Set(controls, "Submit - Visual Only/Submit Label", data.ownHighest ? "최고 입찰 중" : "입찰");
                Set(controls, "Rule", !string.IsNullOrEmpty(data.notice) ? data.notice : data.secret ? "C 취소 · 마감 전 수정 가능" : "최고 입찰 취소 불가");
                Set(controls, "Amount Label", data.secret ? "제출할 금액" : "다음 입찰 금액");
            }
            else if (data.seated)
            {
                Set(header, "Stage", data.complete ? "경기 종료" : "진행 중인 경매 없음");
                Set(header, "Highest Bid", data.idleNote);
                Set(header, "Leading Status", "");
                Set(header, "Countdown", "--:--");
            }
        }

        private static string Describe(Info info) => $"{info.Round}R · {info.CompanyA} / {info.CompanyB}";

        private static string TimeText(double seconds)
        {
            int n = Mathf.Max(0, (int)Math.Ceiling(seconds));
            return $"{n / 60:00}:{n % 60:00}";
        }

        private static void Set(GameObject parent, string path, string value)
        {
            var target = parent.transform.Find(path);
            if (target)
                target.GetComponent<Text>().text = value;
        }
    }
}
