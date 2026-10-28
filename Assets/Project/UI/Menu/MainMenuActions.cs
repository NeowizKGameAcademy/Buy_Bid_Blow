using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu {
 public sealed class MainMenuActions : MonoBehaviour {
  public GameObject howToPlayPanel,settingsPanel;
  public CanvasGroup menuInput;
  public Button startButton,howToPlayButton,settingsButton,howClose,settingsClose;
  // 플레이 씬 로드
  public void StartGame(){SceneManager.LoadScene(BuyBidBlow.Network.GameNetworkManager.PlayScene);}
  // 메뉴 입력을 막고 게임 방법 패널 열기
  public void HowToPlay(){if(menuInput){menuInput.interactable=false;menuInput.blocksRaycasts=false;}howToPlayPanel.SetActive(true);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
  // 설정 패널을 열고 닫기 버튼 선택
  public void Settings(){settingsPanel.SetActive(true);EventSystem.current.SetSelectedGameObject(settingsClose.gameObject);}
  // 게임 방법 패널을 닫고 메뉴 입력 복구 후 버튼 선택
  public void CloseHow(){howToPlayPanel.SetActive(false);if(menuInput){menuInput.interactable=true;menuInput.blocksRaycasts=true;}if(EventSystem.current)EventSystem.current.SetSelectedGameObject(howToPlayButton.gameObject);}
  // 설정 패널을 닫고 설정 버튼 선택
  public void CloseSettings(){settingsPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
  // 전체 오디오 볼륨 설정
  public void Volume(float value){AudioListener.volume=value;}
  // 에디터 플레이 중지 또는 게임 종료
  public void QuitGame(){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
  // 시작 시 시작 버튼 선택
  void Start(){EventSystem.current.SetSelectedGameObject(startButton.gameObject);}
 }
}
