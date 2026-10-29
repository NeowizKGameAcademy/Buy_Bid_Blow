using BuyBidBlow.Common;
using UnityEngine;

namespace BuyBidBlow.Sandbox.Shop
{
    // 상점 판매대. E로 상점창을 연다(PlayerInteraction → IInteractable).
    public sealed class ShopCounter : MonoBehaviour, IInteractable
    {
        public bool CanInteract => ShopSite.Instance;
        public string Prompt => "Shop";

        public bool Interact(GameObject player) => ShopSite.Instance && ShopSite.Instance.OpenShop();
    }
}
