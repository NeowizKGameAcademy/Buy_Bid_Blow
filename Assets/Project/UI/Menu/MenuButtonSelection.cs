using UnityEngine;
using UnityEngine.EventSystems;
namespace BuyBidBlow.UI.Menu {
 public sealed class MenuButtonSelection : MonoBehaviour,ISelectHandler,IDeselectHandler {
  public GameObject indicator;
  // 선택되면 표시기 활성화
  public void OnSelect(BaseEventData e){if(indicator)indicator.SetActive(true);}
  // 선택 해제되면 표시기 비활성화
  public void OnDeselect(BaseEventData e){if(indicator)indicator.SetActive(false);}
 }
}
