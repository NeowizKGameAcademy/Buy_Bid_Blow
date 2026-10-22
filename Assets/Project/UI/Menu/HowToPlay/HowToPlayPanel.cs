using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu {
public sealed class HowToPlayPanel:MonoBehaviour {
 public GameObject[] pages;public Image[] tabs;public Text[] tabLabels;
 public Text counter,previousHint,nextHint;public MainMenuActions menu;
 public Sprite normalTab,selectedTab,disabledTab;
 public int PageIndex {get;private set;}
 bool controlsBound;Button previousButton,nextButton;
 void OnEnable(){if(pages!=null&&pages.Length>0){ConfigureNavigation();Show(0);}}
 void Update(){var k=Keyboard.current;if(k==null)return;if(k.escapeKey.wasPressedThisFrame){menu.CloseHow();return;}if(k.aKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)Turn(-1);else if(k.dKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)Turn(1);}
 public void ConfigureNavigation(){
 previousHint.text="[A/W] 이전";nextHint.text="[D/S] 다음";
 if(controlsBound)return;controlsBound=true;
 previousButton=Bind(previousHint,()=>Turn(-1));nextButton=Bind(nextHint,()=>Turn(1));
 for(int i=0;i<tabs.Length;i++){int page=i;Bind(tabs[i],()=>{if(PageIndex!=page){BuyBidBlow.Audio.GameAudio.Play("ui.page",this);Show(page);}});}
 if(menu&&menu.howClose){var graphic=menu.howClose.GetComponent<Graphic>();if(graphic){graphic.raycastTarget=true;menu.howClose.targetGraphic=graphic;}}
 }
 static Button Bind(Graphic graphic,UnityEngine.Events.UnityAction action){
 var button=graphic.GetComponent<Button>();if(!button)button=graphic.gameObject.AddComponent<Button>();
 graphic.raycastTarget=true;button.targetGraphic=graphic;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);return button;
 }
 public void Turn(int delta){int next=Mathf.Clamp(PageIndex+delta,0,pages.Length-1);if(next!=PageIndex)BuyBidBlow.Audio.GameAudio.Play("ui.page",this);Show(next);}
 public void Show(int index){PageIndex=Mathf.Clamp(index,0,pages.Length-1);for(int i=0;i<pages.Length;i++){pages[i].SetActive(i==PageIndex);tabs[i].sprite=i==PageIndex?selectedTab:normalTab;tabLabels[i].color=i==PageIndex?new Color(.17f,.10f,.04f):new Color(.94f,.88f,.70f);tabLabels[i].fontStyle=i==PageIndex?FontStyle.Bold:FontStyle.Normal;}counter.text=$"{PageIndex+1:00} / {pages.Length:00}";previousHint.color=PageIndex==0?new Color(.5f,.5f,.46f):new Color(.04f,.18f,.17f);nextHint.color=PageIndex==pages.Length-1?new Color(.5f,.5f,.46f):new Color(.04f,.18f,.17f);if(previousButton)previousButton.interactable=PageIndex>0;if(nextButton)nextButton.interactable=PageIndex<pages.Length-1;}
}
}
