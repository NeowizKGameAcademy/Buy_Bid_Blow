using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuyBidBlow
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private DetectObject detect;
        [SerializeField] private TradePanelController tradePanel;
        [SerializeField] private LocalTradeController localTrade;

        public void Interact()
        {
            if (detect == null || tradePanel == null || localTrade == null || tradePanel.IsOpen)
                return;

            if (detect.isDetected && detect.DetectedCollider != null)
                localTrade.OpenPanel();
        }
    }
}
