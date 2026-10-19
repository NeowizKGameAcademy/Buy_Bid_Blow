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

        public void Interact_OpenTradePanel()
        {
            if (detect == null || tradePanel == null || localTrade == null || tradePanel.IsOpen)
                return;

            if (!detect.isDetected || detect.DetectedCollider == null)
                return;

            NPCInfo npcInfo = detect.DetectedCollider.GetComponentInParent<NPCInfo>();

            if (npcInfo != null)
                localTrade.OpenPanel(npcInfo.Company);
        }
    }
}
