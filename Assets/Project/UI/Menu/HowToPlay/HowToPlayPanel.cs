using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu {
public sealed class HowToPlayPanel:MonoBehaviour {
 public GameObject[] pages;public Image[] tabs;public Text[] tabLabels;
 public Text counter,previousHint,nextHint;public MainMenuActions menu;
 public Sprite normalTab,selectedTab,disabledTab;
 public int PageIndex {get;private set;}
 // 활성화 시 첫 페이지 표시
 void OnEnable(){if(pages!=null&&pages.Length>0)Show(0);}
 // Esc로 닫고 좌우 방향키로 페이지 넘김
 void Update(){var k=Keyboard.current;if(k==null)return;if(k.escapeKey.wasPressedThisFrame){menu.CloseHow();return;}if(k.leftArrowKey.wasPressedThisFrame)Turn(-1);if(k.rightArrowKey.wasPressedThisFrame)Turn(1);}
 // 현재 페이지에서 delta만큼 이동
 public void Turn(int delta){Show(Mathf.Clamp(PageIndex+delta,0,pages.Length-1));}
 // 지정 페이지만 표시하고 탭·카운터·이전/다음 힌트 색상 갱신
 public void Show(int index){PageIndex=Mathf.Clamp(index,0,pages.Length-1);for(int i=0;i<pages.Length;i++){pages[i].SetActive(i==PageIndex);tabs[i].sprite=i==PageIndex?selectedTab:normalTab;tabLabels[i].color=i==PageIndex?new Color(.17f,.10f,.04f):new Color(.94f,.88f,.70f);tabLabels[i].fontStyle=i==PageIndex?FontStyle.Bold:FontStyle.Normal;}counter.text=$"{PageIndex+1:00} / {pages.Length:00}";previousHint.color=PageIndex==0?new Color(.5f,.5f,.46f):new Color(.04f,.18f,.17f);nextHint.color=PageIndex==pages.Length-1?new Color(.5f,.5f,.46f):new Color(.04f,.18f,.17f);}
}
}
