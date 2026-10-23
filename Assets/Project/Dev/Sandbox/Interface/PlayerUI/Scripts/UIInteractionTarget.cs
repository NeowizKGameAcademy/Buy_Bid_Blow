using UnityEngine;
namespace BuyBidBlow.PlayerUISandbox {
 public sealed class UIInteractionTarget:MonoBehaviour {
 public enum Kind { Straw,Timber,Brick,AuctionSeat }
 public Kind kind;
 public Transform seatedView;
 public string Prompt=>kind==Kind.AuctionSeat?"E · 경매 참여":"E · "+kind+" 창구 이용";
 }
}
