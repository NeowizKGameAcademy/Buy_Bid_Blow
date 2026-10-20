using BuyBidBlow.Common;
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

        /// <summary>
        /// Handles the context-sensitive E interaction.  Seats take priority over
        /// trading: pressing E while seated releases the player, while looking at
        /// a seat reserves its first available sitting point.
        /// </summary>
        public bool Interact()
        {
            if (TryGetComponent(out PlayerSeating seating) && seating.IsSeated)
                return seating.TryStand();

            if (detect == null || !detect.isDetected || detect.DetectedCollider == null)
                return false;

            var target = detect.DetectedCollider.GetComponentInParent<IInteractable>();
            if (target != null && target.CanInteract && target.Interact(gameObject))
                return true;

            if (tradePanel == null || localTrade == null || tradePanel.IsOpen)
                return false;

            NPCInfo npcInfo = detect.DetectedCollider.GetComponentInParent<NPCInfo>();

            if (npcInfo != null)
            {
                localTrade.OpenPanel(npcInfo.Company);
                return true;
            }

            return false;
        }

        // Kept for any existing UnityEvent or external script references.
        public void Interact_OpenTradePanel() => Interact();
    }
}
