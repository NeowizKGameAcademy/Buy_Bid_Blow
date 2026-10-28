using BuyBidBlow.Common;
using BuyBidBlow.UI.Notebook;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Lottery
{
    public sealed class LotteryDevice : MonoBehaviour, IInteractable
    {
        public bool CanInteract => MatchNet.Current && MatchNet.Current.phase==MatchNet.Phase.Playing;
        public string Prompt => "E : LOTTERY";
        public bool Interact(GameObject player)
        {
            if(!CanInteract || NetworkNotebookController.BlocksInput)return false;
            var panel=FindFirstObjectByType<LotterySandboxPanel>();
            return panel && panel.Open(player);
        }
    }
}
