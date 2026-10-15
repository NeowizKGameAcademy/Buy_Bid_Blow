using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BuyBidBlow.MainMenu {
 public sealed class MainMenuActions : MonoBehaviour {
  public GameObject howToPlayPanel,settingsPanel;
  public Button startButton,howToPlayButton,settingsButton,howClose,settingsClose;
  public void StartGame(){SceneManager.LoadScene("Assets/Project/Dev/Sandbox/Lobby/SC_Lobby.unity");}
  public void HowToPlay(){howToPlayPanel.SetActive(true);EventSystem.current.SetSelectedGameObject(howClose.gameObject);}
  public void Settings(){settingsPanel.SetActive(true);EventSystem.current.SetSelectedGameObject(settingsClose.gameObject);}
  public void CloseHow(){howToPlayPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(howToPlayButton.gameObject);}
  public void CloseSettings(){settingsPanel.SetActive(false);EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);}
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
