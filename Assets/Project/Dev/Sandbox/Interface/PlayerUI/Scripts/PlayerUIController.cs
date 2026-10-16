using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BuyBidBlow.PlayerUISandbox {
 public sealed class PlayerUIController:MonoBehaviour {
 public enum Mode { Explore,Trade,Auction }
 public PlayerMove movement;public DetectObject detection;public Camera view;public Rigidbody body;
 public GameObject hud,tradePanel,auctionHeader,auctionControls,crosshair;public Text prompt;
 public Mode CurrentMode{get;private set;}
 public GameObject[] companyPanels; int company; public bool isLocalPreview=true;
 Vector3 cameraPosition;Quaternion cameraRotation;Transform cameraParent;int quantity=1,tab;float relock;
 public void Awake(){if(!isLocalPreview){hud.SetActive(false);enabled=false;return;}cameraParent=view.transform.parent;cameraPosition=view.transform.localPosition;cameraRotation=view.transform.localRotation;Close();}
 void OnDisable(){if(movement){movement.SetMoveInput(Vector2.zero);movement.SetJumpHeld(false);}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 void Update(){var k=Keyboard.current;var m=Mouse.current;if(k==null)return;
 if(CurrentMode==Mode.Explore){movement.SetMoveInput(new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)));movement.SetJumpHeld(k.spaceKey.isPressed);if(m!=null&&Cursor.lockState==CursorLockMode.Locked)movement.Rotate(m.delta.ReadValue());
 var target=detection.DetectedCollider?detection.DetectedCollider.GetComponentInParent<UIInteractionTarget>():null;prompt.text=target?target.Prompt:"WASD 이동 · 마우스 시점 · E 상호작용";if(k.eKey.wasPressedThisFrame&&target)Open(target);
 if(k.escapeKey.wasPressedThisFrame){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}if(m!=null&&m.leftButton.wasPressedThisFrame){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 }else {if(k.escapeKey.wasPressedThisFrame){Close();return;}if(CurrentMode==Mode.Trade){if(k.leftArrowKey.wasPressedThisFrame)quantity=Mathf.Max(1,quantity-1);if(k.rightArrowKey.wasPressedThisFrame)quantity++;if(k.upArrowKey.wasPressedThisFrame)tab=(tab+2)%3;if(k.downArrowKey.wasPressedThisFrame)tab=(tab+1)%3;RefreshTrade();if(k.eKey.wasPressedThisFrame)prompt.text=tradePanel.GetComponent<TradingPanelView>().CanConfirm?"UI 프리뷰 · 실제 거래 미연결":tradePanel.GetComponent<TradingPanelView>().Status;}else if(k.eKey.wasPressedThisFrame)prompt.text="입찰 입력 테스트 · 실제 입찰 미연결 · Esc 퇴석";}
 if(relock>0&&Time.unscaledTime>=relock){relock=0;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 }
 public void Open(UIInteractionTarget target){if(CurrentMode!=Mode.Explore)return;movement.SetMoveInput(Vector2.zero);movement.SetJumpHeld(false);movement.enabled=false;body.linearVelocity=Vector3.zero;body.isKinematic=true;detection.enabled=false;crosshair.SetActive(false);
 if(target.kind==UIInteractionTarget.Kind.AuctionSeat){CurrentMode=Mode.Auction;auctionHeader.SetActive(true);auctionControls.SetActive(true);view.transform.SetParent(target.seatedView,false);view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;prompt.text="E 입찰 테스트 · Esc 퇴석";}else{CurrentMode=Mode.Trade;company=(int)target.kind;if(companyPanels!=null&&companyPanels.Length==3)tradePanel=companyPanels[company];tradePanel.SetActive(true);quantity=1;tab=0;SetText("Company Name",target.kind.ToString());prompt.text="";RefreshTrade();}
 }
 void RefreshTrade(){tradePanel.GetComponent<TradingPanelView>().Present(TradePreviewSource.ForCompany(company),tab,ref quantity);} void SetText(string name,string value){foreach(var t in tradePanel.GetComponentsInChildren<Text>(true))if(t.name==name)t.text=value;}
 public void Close(){view.transform.SetParent(cameraParent,false);view.transform.localPosition=cameraPosition;view.transform.localRotation=cameraRotation;CurrentMode=Mode.Explore;tradePanel.SetActive(false);auctionHeader.SetActive(false);auctionControls.SetActive(false);crosshair.SetActive(true);detection.enabled=true;movement.enabled=true;movement.SetMoveInput(Vector2.zero);body.isKinematic=false;relock=Time.unscaledTime+.05f;}
 }
}
