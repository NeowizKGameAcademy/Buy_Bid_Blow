using BuyBidBlow.Common;
using UnityEngine;

namespace BuyBidBlow.Props
{
    // Client presentation only. MatchNet already supplies the public auction state.
    // Keep this on an active parent and hide only the envelope child.
    public sealed class AuctionLotDisplayClient : MonoBehaviour
    {
        public AuctionEnvelopeDisplay display;

        void OnEnable() => Refresh();
        void Update() => Refresh();
        void OnDisable() => SetVisible(false);

        public void Refresh()
        {
            var match = MatchNet.Current;
            bool visible = match && match.phase == MatchNet.Phase.Playing
                && (match.auctionStage == MatchNet.AuctionStage.Open || match.auctionStage == MatchNet.AuctionStage.Secret)
                && match.auctionLot.firstRound > 0;
            if (visible && display) display.SetRounds(match.auctionLot.firstRound, match.auctionLot.secondRound);
            SetVisible(visible);
        }

        void SetVisible(bool visible)
        {
            if (!display || display.gameObject == gameObject) return;
            if (display.gameObject.activeSelf != visible) display.gameObject.SetActive(visible);
        }
    }
}
