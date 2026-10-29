using BuyBidBlow.Common;
using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Labor
{
    // 자재 더미. 빈손이면 E로 한 짐 들고, 짐을 들고 있으면 E로 돌려놓는다. 판정은 서버가 한다.
    public sealed class LaborPile : MonoBehaviour, IInteractable
    {
        public Company material;

        public bool CanInteract => LaborSite.Instance;
        public string Prompt => LaborSite.Carrying < 0 ? $"Pick up {material}" : "Put back";

        public bool Interact(GameObject player)
        {
            if (LaborSite.Carrying < 0) LaborSite.Request(LaborAction.Pickup, (int)material);
            else LaborSite.Request(LaborAction.Return);
            return true;
        }
    }
}
