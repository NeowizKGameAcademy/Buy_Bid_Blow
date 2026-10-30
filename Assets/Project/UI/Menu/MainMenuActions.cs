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
  public void Settings(){GameMenuController.Ensure()?.OpenSettings();}
  public void CloseHow(){BuyBidBlow.Audio.GameAudio.Play("ui.close",this);howToPlayPanel.SetActive(false);if(menuInput){menuInput.interactable=true;menuInput.blocksRaycasts=true;}if(EventSystem.current)EventSystem.current.SetSelectedGameObject(howToPlayButton.gameObject);}
  public void CloseSettings(){BuyBidBlow.Audio.GameAudio.Play("ui.close",this);settingsPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
  public void Volume(float value){AudioListener.volume=value;}
  // 에디터 플레이 중지 또는 게임 종료
  public void QuitGame(){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
  void Start(){GameMenuController.Ensure();if(settingsPanel)settingsPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(startButton.gameObject);}
 }
}
