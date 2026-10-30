using System;
using BuyBidBlow.Systems.Types;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Auction
{
    // What the seated player sees of the running auction. Other players' secret bids never reach it.
    public struct AuctionPanelData
    {
        public bool seated, live, secret, complete, inMatch;
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
        [SerializeField] private GameObject lotSummary;
        [SerializeField] private Text[] lotRounds;
        [SerializeField] private Image[] lotCompanies;
        [SerializeField] private Sprite[] companyIcons;

        // 좌석·경매 진행 상태에 따라 헤더/입찰 컨트롤/로트 정보를 표시
        public void Present(AuctionPanelData data)
        {
            Set(controls,"Keycap ←/Key","A");
            Set(controls,"Keycap →/Key","D");
            bool statusVisible = !data.complete && data.seated;
            header.SetActive(statusVisible);
            controls.SetActive(data.seated && data.live);
            bool lotVisible = !data.complete && (data.inMatch || data.live) && data.lot.First.Round > 0;
            if (lotDetails)
                lotDetails.transform.parent.gameObject.SetActive(!lotSummary && lotVisible);
            if (lotSummary) lotSummary.SetActive(lotVisible);

            if (lotVisible)
            {
                if (lotDetails)
                    lotDetails.text = data.lot.HasSecond ? $"{Describe(data.lot.First)}\n{Describe(data.lot.Second)}" : Describe(data.lot.First);
                if(lotSummary)
                {
                    ((RectTransform)lotSummary.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.lot.HasSecond ? 110 : 64);
                    var rows = new[] { data.lot.First, data.lot.Second };
                    for(int row=0;row<2;row++)
                    {
                        bool visible = row == 0 || data.lot.HasSecond;
                        lotRounds[row].gameObject.SetActive(visible);
                        lotCompanies[row*2].gameObject.SetActive(visible);
                        lotCompanies[row*2+1].gameObject.SetActive(visible);
                        if (!visible) continue;
                        lotRounds[row].text = rows[row].Round + "R";
                        lotCompanies[row*2].sprite = companyIcons[(int)rows[row].CompanyA];
                        lotCompanies[row*2+1].sprite = companyIcons[(int)rows[row].CompanyB];
                    }
                }
            }

            if (data.live)
            {
                Set(header, "Stage", data.lot.HasSecond
                    ? (data.secret ? "2차 비공개 경매" : "1차 공개 경매")
                    : (data.secret ? "비공개 경매" : "공개 경매"));
                Set(header, "Highest Bid", data.secret ? "내 제출 $" + data.ownSubmitted
                    : data.hasBid ? $"최고 입찰 ${data.highestBid} · {data.highestBidder}" : "입찰 없음");
                Set(header, "Leading Status", data.secret ? "다른 입찰 비공개" : data.ownHighest ? "현재 최고 입찰 중" : "");
                Set(header, "Countdown", TimeText(data.secondsLeft));

                Set(controls, "Amount", "$" + data.bidAmount);
                Set(controls, "Cash After", "입찰 후 거래 가능 $" + (data.available + data.ownSubmitted - data.bidAmount));
                Set(controls, "Submit - Visual Only/Submit Label", data.ownHighest ? "입찰 올리기" : "입찰");
                Set(controls, "Rule", !string.IsNullOrEmpty(data.notice) ? data.notice : data.secret ? "C 취소 · 마감 전 수정 가능" : "최고 입찰 취소 불가");
                Set(controls, "Amount Label", data.secret ? "제출할 금액" : "다음 입찰 금액");
            }
            else if (statusVisible)
            {
                Set(header, "Stage", data.complete ? "경기 종료" : "진행 중인 경매 없음");
                Set(header, "Highest Bid", data.idleNote);
                Set(header, "Leading Status", "");
                Set(header, "Countdown", "--:--");
            }
        }

        // 정보 항목을 "라운드 · 회사A / 회사B" 문자열로 변환
        private static string Describe(Info info) => $"{info.Round}R · {info.CompanyA.DisplayName()} / {info.CompanyB.DisplayName()}";

        // 남은 초를 mm:ss 형식 문자열로 변환
        private static string TimeText(double seconds)
        {
            int n = Mathf.Max(0, (int)Math.Ceiling(seconds));
            return $"{n / 60:00}:{n % 60:00}";
        }

        // 부모 아래 경로의 Text 컴포넌트에 값을 설정
        private static void Set(GameObject parent, string path, string value)
        {
            var target = parent.transform.Find(path);
            if (target)
                target.GetComponent<Text>().text = value;
        }
    }
}
