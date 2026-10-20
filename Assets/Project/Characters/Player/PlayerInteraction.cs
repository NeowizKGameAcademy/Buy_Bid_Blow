using BuyBidBlow.Common;
using BuyBidBlow.Props;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Market;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private DetectObject detect;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private LocalTradeController localTrade;
        [SerializeField] private SeatController seats;

        // E on what the player is aiming at: a desk clerk opens trading, a free chair seats the player.
        public void Interact()
        {
            if (detect == null || (tradePanel != null && tradePanel.IsOpen) || (seats != null && seats.IsSeated))
                return;

            if (!detect.isDetected || detect.DetectedCollider == null)
                return;

            var npcInfo = detect.DetectedCollider.GetComponentInParent<NPCInfo>();
            if (npcInfo != null && localTrade != null)
            {
                localTrade.OpenPanel(npcInfo.Company);
                return;
            }

            var seat = detect.DetectedCollider.GetComponentInParent<Seat>();
            if (seat != null && seats != null && seat.CanInteract)
                seats.RequestSit(seat);
        }
    }
}
