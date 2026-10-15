using UnityEngine;
using UnityEngine.EventSystems;
namespace BuyBidBlow.MainMenu {
 public sealed class MenuButtonSelection : MonoBehaviour,ISelectHandler,IDeselectHandler {
  public GameObject indicator;
  public void OnSelect(BaseEventData e){if(indicator)indicator.SetActive(true);}
  public void OnDeselect(BaseEventData e){if(indicator)indicator.SetActive(false);}
 }
}
