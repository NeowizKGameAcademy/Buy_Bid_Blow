using BuyBidBlow.Common;
using BuyBidBlow.UI.Notebook;
using UnityEngine;

namespace BuyBidBlow.Sandbox.StockGacha
{
    public sealed class StockGachaDevice : MonoBehaviour, IInteractable
    {
        public bool CanInteract => MatchNet.Current && MatchNet.Current.phase==MatchNet.Phase.Playing;
        public string Prompt => "E : STOCK GACHA";
        public bool Interact(GameObject player)
        {
            if(!CanInteract || NetworkNotebookController.BlocksInput)return false;
            var panel=FindFirstObjectByType<StockGachaSandboxPanel>();
            return panel && panel.Open(player);
        }
    }
}
