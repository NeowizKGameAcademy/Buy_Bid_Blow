using UnityEngine;

namespace BuyBidBlow
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
