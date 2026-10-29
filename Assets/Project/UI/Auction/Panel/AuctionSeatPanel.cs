using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BuyBidBlow.UI.Auction
{
    // While the local player sits on a mezzanine auction chair: shows the auction panel from the public
    // auction state on MatchNet and takes bids (A/D amount, E bid, C cancel the secret submission).
    public sealed class AuctionSeatPanel : MonoBehaviour
    {
        [SerializeField] private AuctionPanelView view;
        [SerializeField] private PlayerSeating seating;
        [SerializeField] private PlayerAssetStatus assets;
        [SerializeField, Min(0)] private float noticeSeconds = 3;

        private int bidAmount = 1;
        private MatchNet.AuctionStage lastStage;
        private string notice;
        private float noticeUntil;

        // 로컬 플레이어 응답 이벤트 구독
        private void OnEnable() => PlayerNet.LocalReply += ShowReply;
        // 로컬 플레이어 응답 이벤트 구독 해제
        private void OnDisable() => PlayerNet.LocalReply -= ShowReply;

        private bool SeatedForAuction
        {
            get { var seat = seating ? seating.CurrentSeat : null; return seat && seat.auction; }
        }

        // 경매석 착석 시 키 입력으로 입찰 조정/제출/취소 처리 후 패널 갱신
        private void Update()
        {
            var keyboard = Keyboard.current;
            var hud = GetComponentInParent<BuyBidBlow.UI.HUD.HUDController>();
            if (keyboard != null && SeatedForAuction && (!hud || !hud.ModalOpen) && !BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput)
            {
                if (keyboard.dKey.wasPressedThisFrame) AdjustBid(1);
                if (keyboard.aKey.wasPressedThisFrame) AdjustBid(-1);
                if (keyboard.eKey.wasPressedThisFrame) SubmitBid();
                if (keyboard.cKey.wasPressedThisFrame) CancelSecret();
            }
            view.Present(Seated());
        }

        public void AdjustBid(int delta)
        {int before=bidAmount;bidAmount=Mathf.Max(MinimumBid(),bidAmount+delta);if(before!=bidAmount)BuyBidBlow.Audio.GameAudio.Play("ui.quantity",this);}

        // 경매 진행 중이면 현재 금액으로 입찰 전송
        public void SubmitBid()
        {
            if (Live(out _)) Send(bidAmount, false);
        }

        // 비공개 경매 진행 중이면 제출한 입찰 취소 요청
        public void CancelSecret()
        {
            if (Live(out var stage) && stage == MatchNet.AuctionStage.Secret) Send(0, true);
        }

        // 로컬 플레이어를 통해 서버에 입찰/취소 명령 전송
        private void Send(int amount, bool cancel)
        {
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (local){BuyBidBlow.Audio.GameAudio.Play(cancel?"ui.close":"ui.confirm",this);local.CmdBid(amount, cancel);}
        }

        // 착석 중이면 입찰 응답 메시지를 일정 시간 표시하도록 저장
        private void ShowReply(RequestResult result)
        {
            if (result.Kind != RequestKind.Bid && result.Kind != RequestKind.Seat) return;
            notice = result.Message;
            noticeUntil = Time.unscaledTime + noticeSeconds;
        }

        // 현재 경매 단계를 구하고 마감 전 진행 중인지 반환
        private static bool Live(out MatchNet.AuctionStage stage)
        {
            var match = MatchNet.Current;
            stage = match ? match.auctionStage : MatchNet.AuctionStage.None;
            return stage != MatchNet.AuctionStage.None && match.auctionClosesAt > NetworkTime.time;
        }

        // 경매 단계에 맞는 최소 입찰 금액 계산
        // Open auction: above the current highest bid. Secret auction: any positive amount.
        private static int MinimumBid()
        {
            var match = MatchNet.Current;
            return match && match.auctionStage == MatchNet.AuctionStage.Open ? match.highestBid + 1 : 1;
        }

        // 매치 경매 상태로 착석 플레이어용 패널 데이터 구성
        private AuctionPanelData Seated()
        {
            var data = new AuctionPanelData { seated = SeatedForAuction, available = assets ? assets.Available : 0 };
            var match = MatchNet.Current;
            if (!match)
                return data;

            data.complete = match.phase == MatchNet.Phase.Finished;
            data.idleNote = match.nextAuctionRound > 0 ? $"다음 경매 {match.nextAuctionRound}R" : "남은 경매 없음";
            data.live = Live(out var stage);
            // A new auction slot starts the amount again from its minimum.
            if (stage != lastStage) { lastStage = stage; bidAmount = 1; }
            bidAmount = Mathf.Max(bidAmount, MinimumBid());
            data.bidAmount = bidAmount;
            data.notice = Time.unscaledTime < noticeUntil ? notice : null;
            if (!data.live)
                return data;

            data.secret = stage == MatchNet.AuctionStage.Secret;
            data.secondsLeft = match.auctionClosesAt - NetworkTime.time;
            var lot = match.auctionLot;
            // Directions are never sent; the view only shows rounds and companies.
            data.lot = new Lot(
                new Info(lot.firstRound, (Company)lot.firstA, default, (Company)lot.firstB, default),
                new Info(lot.secondRound, (Company)lot.secondA, default, (Company)lot.secondB, default));
            data.hasBid = !data.secret && match.highestBid > 0;
            data.highestBid = match.highestBid;
            data.highestBidder = match.highestBidder;
            data.ownSubmitted = data.secret ? PlayerNet.LocalSecretBid : 0;
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            data.ownHighest = data.hasBid && local && match.highestBidderId == local.playerId;
            if(data.ownHighest)data.ownSubmitted=data.highestBid;
            return data;
        }
    }
}
