using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.UI.Auction
{
    // While the local player sits on a mezzanine auction chair: shows the auction panel from the public
    // auction state on MatchNet and takes bids. The keys (A/D amount, E bid, C cancel the secret
    // submission) are read by PlayerInputHandler.
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

        private void OnEnable() => PlayerNet.LocalReply += ShowReply;
        private void OnDisable() => PlayerNet.LocalReply -= ShowReply;

        public bool SeatedForAuction
        {
            get { var seat = seating ? seating.CurrentSeat : null; return seat && seat.auction; }
        }

        private void Update()
        {
            if (!SeatedForAuction) { view.Present(default); return; }
            view.Present(Seated());
        }

        public void AdjustBid(int delta)
        {int before=bidAmount;bidAmount=Mathf.Max(MinimumBid(),bidAmount+delta);if(before!=bidAmount)BuyBidBlow.Audio.GameAudio.Play("ui.quantity",this);}

        public void SubmitBid()
        {
            if (Live(out _)) Send(bidAmount, false);
        }

        public void CancelSecret()
        {
            if (Live(out var stage) && stage == MatchNet.AuctionStage.Secret) Send(0, true);
        }

        private void Send(int amount, bool cancel)
        {
            var local = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<PlayerNet>() : null;
            if (local){BuyBidBlow.Audio.GameAudio.Play(cancel?"ui.close":"ui.confirm",this);local.CmdBid(amount, cancel);}
        }

        private void ShowReply(string message)
        {
            if (!SeatedForAuction) return;
            notice = message;
            noticeUntil = Time.unscaledTime + noticeSeconds;
        }

        private static bool Live(out MatchNet.AuctionStage stage)
        {
            var match = MatchNet.Current;
            stage = match ? match.auctionStage : MatchNet.AuctionStage.None;
            return stage != MatchNet.AuctionStage.None && match.auctionClosesAt > NetworkTime.time;
        }

        // Open auction: above the current highest bid. Secret auction: any positive amount.
        private static int MinimumBid()
        {
            var match = MatchNet.Current;
            return match && match.auctionStage == MatchNet.AuctionStage.Open ? match.highestBid + 1 : 1;
        }

        private AuctionPanelData Seated()
        {
            var data = new AuctionPanelData { seated = true, available = assets ? assets.Available : 0 };
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
            data.ownHighest = data.hasBid && local && match.highestBidder == local.nickname;
            if(data.ownHighest)data.ownSubmitted=data.highestBid;
            return data;
        }
    }
}
