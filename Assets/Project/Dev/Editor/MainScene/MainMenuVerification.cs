using System;
using System.IO;
using BuyBidBlow.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace BuyBidBlow.Dev.Editor {
 public static class MainMenuVerification {
 static double next;static int stage;
 public static void RunBatch(){MainSceneBuilder.Build();EditorApplication.update+=Tick;EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+3;SessionState.SetBool("MainMenuVerify",true);}
 [InitializeOnLoadMethod] static void Restore(){if(SessionState.GetBool("MainMenuVerify",false)){next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Tick;}}
 static void Tick(){if(EditorApplication.timeSinceStartup<next||!EditorApplication.isPlaying)return;try{
 if(stage==0){var actions=UnityEngine.Object.FindFirstObjectByType<MainMenuActions>();if(!actions)throw new Exception("Missing menu actions");if(EventSystem.current.currentSelectedGameObject!=actions.startButton.gameObject)throw new Exception("Initial focus");actions.howToPlayButton.onClick.Invoke();if(!actions.howToPlayPanel.activeSelf)throw new Exception("HowToPlay did not open");actions.howClose.onClick.Invoke();if(actions.howToPlayPanel.activeSelf)throw new Exception("HowToPlay did not close");actions.settingsButton.onClick.Invoke();if(!actions.settingsPanel.activeSelf)throw new Exception("Settings did not open");actions.Volume(.4f);if(Math.Abs(AudioListener.volume-.4f)>.01)throw new Exception("Volume");actions.Volume(1);actions.settingsClose.onClick.Invoke();actions.startButton.onClick.Invoke();stage=1;next=EditorApplication.timeSinceStartup+3;}
 else {if(SceneManager.GetActiveScene().name!=BuyBidBlow.Network.GameNetworkManager.PlayScene)throw new Exception("Shared lobby hall did not load");File.AppendAllText("Assets/Project/UI/Menu/Verification.txt","\nPLAY PASS: first selected button; HowToPlay open/close; Settings open/close; volume changes; GameStart loads the shared furnished hall. Physical keyboard input and standalone Quit remain manual.");SessionState.SetBool("MainMenuVerify",false);EditorApplication.Exit(0);}
 }catch(Exception ex){SessionState.SetBool("MainMenuVerify",false);Debug.LogException(ex);EditorApplication.Exit(1);}}
 }
}
