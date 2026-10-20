using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu {
 public sealed class MainMenuActions : MonoBehaviour {
  public GameObject howToPlayPanel,settingsPanel;
  public CanvasGroup menuInput;
  public Button startButton,howToPlayButton,settingsButton,howClose,settingsClose;
  public void StartGame(){SceneManager.LoadScene(BuyBidBlow.Network.GameNetworkManager.PlayScene);}
  public void HowToPlay(){if(menuInput){menuInput.interactable=false;menuInput.blocksRaycasts=false;}howToPlayPanel.SetActive(true);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
  public void Settings(){settingsPanel.SetActive(true);EventSystem.current.SetSelectedGameObject(settingsClose.gameObject);}
  public void CloseHow(){BuyBidBlow.Audio.GameAudio.Play("ui.close",this);howToPlayPanel.SetActive(false);if(menuInput){menuInput.interactable=true;menuInput.blocksRaycasts=true;}if(EventSystem.current)EventSystem.current.SetSelectedGameObject(howToPlayButton.gameObject);}
  public void CloseSettings(){BuyBidBlow.Audio.GameAudio.Play("ui.close",this);settingsPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
  public void Volume(float value){AudioListener.volume=value;}
  public void QuitGame(){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
  void Start(){EventSystem.current.SetSelectedGameObject(startButton.gameObject);}
 }
}
