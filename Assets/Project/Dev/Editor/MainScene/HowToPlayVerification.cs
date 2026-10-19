using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using BuyBidBlow.UI.Menu;
namespace BuyBidBlow.Dev.Editor {
[InitializeOnLoad] public static class HowToPlayVerification {
 static int step,frames; static MainMenuActions menu; static HowToPlayPanel panel;
 const string Report="Assets/Project/UI/Menu/HowToPlay/PlayVerification.txt";
 static HowToPlayVerification(){if(SessionState.GetBool("HowToPlayQA",false))EditorApplication.update+=Tick;}
 public static void Run(){try{HowToPlayBuilder.Build();EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_MainMenu.unity");SessionState.SetBool("HowToPlayQA",true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;}catch(Exception e){File.WriteAllText(Report,e.ToString());EditorApplication.Exit(1);}}
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static int index;static bool released;static Key[] keys={Key.LeftArrow,Key.RightArrow,Key.RightArrow,Key.RightArrow,Key.RightArrow,Key.RightArrow,Key.RightArrow,Key.RightArrow,Key.LeftArrow,Key.Escape};static int[] expected={0,1,2,3,4,5,6,6,5,5};
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(++frames<15)return;frames=0;try{
 if(step==0){Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();menu=UnityEngine.Object.FindFirstObjectByType<MainMenuActions>();menu.howToPlayButton.onClick.Invoke();panel=menu.howToPlayPanel.GetComponent<HowToPlayPanel>();Check(panel.isActiveAndEnabled&&!menu.menuInput.interactable,"Open or menu isolation");Check(panel.PageIndex==0,"Open first page");step=1;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys[index]));return;}
 if(step==1){if(!released){Check(panel.PageIndex==expected[index],"Keyboard "+keys[index]+" expected "+expected[index]+" actual "+panel.PageIndex);if(keys[index]==Key.Escape){Check(!menu.howToPlayPanel.activeSelf&&menu.menuInput.interactable,"Escape close");Check(EventSystem.current.currentSelectedGameObject==menu.howToPlayButton.gameObject,"Restore focus");}InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());released=true;return;}released=false;if(++index<keys.Length){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys[index]));return;}menu.HowToPlay();Check(panel.PageIndex==0,"Reopen reset");menu.CloseHow();File.WriteAllText(Report,"PASS: Play mode; actual HowToPlay button; keyboard arrows all 7 pages and both boundaries; Esc close; menu isolation; restored focus; reopen resets page.\n");step=2;EditorApplication.isPlaying=false;SessionState.SetBool("HowToPlayQA",false);EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
 }catch(Exception e){File.WriteAllText(Report,e.ToString());SessionState.SetBool("HowToPlayQA",false);EditorApplication.Exit(1);}}
}
}