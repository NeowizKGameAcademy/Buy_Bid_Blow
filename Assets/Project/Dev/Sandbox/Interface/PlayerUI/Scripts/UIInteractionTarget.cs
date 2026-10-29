using UnityEngine;
namespace BuyBidBlow.PlayerUISandbox {
 // Sensed like any interactable; PlayerUIController handles E for the preview itself.
 public sealed class UIInteractionTarget:MonoBehaviour,BuyBidBlow.Common.IInteractable {
 public enum Kind { Straw,Timber,Brick,AuctionSeat }
 public Kind kind;
 public Transform seatedView;
 public string Prompt=>kind==Kind.AuctionSeat?"E · 경매 참여":"E · "+kind+" 창구 이용";
 public bool CanInteract=>true;
 public bool Interact(GameObject player)=>false;
 }
}
