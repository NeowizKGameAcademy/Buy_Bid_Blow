using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Room;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad] public static class LobbyNicknameUpgrade
    {
        const string Request="Library/LobbyNickname.request";
        const string StageKey="LobbyNickname.TestStage";
        static double next;
        static LobbyNicknameUpgrade(){EditorApplication.update+=Tick;}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void SaveLayout()
        {
            var scene=SceneManager.GetSceneByPath(LobbyBuilder.ScenePath);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(LobbyBuilder.ScenePath,OpenSceneMode.Additive);
            try {
                var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyHud>(true)).Single();
                hud.UpgradeConnectionPanel();EditorUtility.SetDirty(hud);EditorSceneManager.SaveScene(scene);
                var connection=hud.transform.Find("Lobby HUD/Connection");Require(connection.Find("Nickname").GetComponent<InputField>()!=null,"Nickname field missing.");
                Require(Nickname.TryNormalize("  경매왕  ",out var name)&&name=="경매왕","Korean trim validation failed.");
                Require(!Nickname.TryNormalize("   ",out _)&&!Nickname.TryNormalize("1234567890123",out _)&&!Nickname.TryNormalize("<b>이름</b>",out _)&&!Nickname.TryNormalize("이름\n두줄",out _),"Invalid nickname accepted.");
                Require(Nickname.TryNormalize("123456789012",out _),"12-character boundary rejected.");
                RenderConnection(hud);
                File.WriteAllText("Library/LobbyNickname.result","PASS: layout saved; blank, length, Korean, rich-text and newline validation checked. Host test pending.");
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        public static void RenderConnection(LobbyHud hud)
        {
            var hidden=new System.Collections.Generic.List<GameObject>();var canvas=hud.GetComponentInChildren<Canvas>();
            var target=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var mode=canvas.renderMode;var camera=canvas.worldCamera;float distance=canvas.planeDistance;
            try {
                for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s==hud.gameObject.scene)continue;foreach(var root in s.GetRootGameObjects())if(root.activeSelf){root.SetActive(false);hidden.Add(root);}}
                foreach(var root in hud.gameObject.scene.GetRootGameObjects())if(root.name=="Lobby Art Preview - Editor Only"&&root.activeSelf){root.SetActive(false);hidden.Add(root);}
                hud.Refresh(false);hud.view.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=hud.view;canvas.planeDistance=.5f;
                Canvas.ForceUpdateCanvases();hud.view.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(LobbyBuilder.Root+"/LobbyNicknamePreview.png",image.EncodeToPNG());
            }finally {
                RenderTexture.active=null;hud.view.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance;
                foreach(var root in hidden)if(root)root.SetActive(true);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
                hud.Refresh(true);
            }
        }
        static void Tick()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
            int stage=SessionState.GetInt(StageKey,0);
            try {
                if(stage==0) {
                    Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play mode before applying lobby layout.");
                    SaveLayout();
                    var scene=SceneManager.GetSceneByPath(LobbyBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(LobbyBuilder.ScenePath,OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene);
                    SessionState.SetString("LobbyNickname.SavedPreference",PlayerPrefs.GetString(Nickname.PreferenceKey,""));
                    SessionState.SetBool("LobbyNickname.HadPreference",PlayerPrefs.HasKey(Nickname.PreferenceKey));
                    SessionState.SetInt(StageKey,1);EditorApplication.isPlaying=true;next=EditorApplication.timeSinceStartup+3;return;
                }
                if(!EditorApplication.isPlaying)return;
                var hud=UnityEngine.Object.FindFirstObjectByType<LobbyHud>();Require(hud!=null,"Runtime lobby missing.");
                var field=hud.transform.Find("Lobby HUD/Connection/Nickname").GetComponent<InputField>();
                if(stage==1) {
                    EventSystem.current.SetSelectedGameObject(field.gameObject);field.ActivateInputField();SessionState.SetInt(StageKey,10);next=EditorApplication.timeSinceStartup+1;return;
                }
                if(stage==10) {
                    Require(field.isFocused,"Nickname input focus failed.");Require(EventSystem.current.currentInputModule.inputOverride is LobbyTextInputBridge,"Input System text bridge missing.");
                    field.text="   ";hud.room.StartHost();Require(!NetworkServer.active,"Blank nickname started a host.");
                    field.text="  경매왕  ";hud.room.StartHost();SessionState.SetInt(StageKey,2);next=EditorApplication.timeSinceStartup+3;return;
                }
                var player=UnityEngine.Object.FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).FirstOrDefault(p=>p.isLocalPlayer);
                if(stage==2) {
                    Require(player&&player.nickname=="경매왕",$"Host nickname did not reach server SyncVar. player={(player?player.nickname:"missing")}, requested={hud.room.LocalNickname}, field={field.text}, server={NetworkServer.active}, client={NetworkClient.active}, ready={NetworkClient.ready}.");
                    hud.Refresh(false);var name=hud.transform.Find("Lobby HUD/Player "+(player.seat+1)+"/Nameplate/Name").GetComponent<Text>();Require(name.text=="경매왕","HUD still displays default name.");
                    hud.ToggleReady();SessionState.SetInt(StageKey,3);next=EditorApplication.timeSinceStartup+1;return;
                }
                if(stage==3) {
                    Require(player&&player.ready,"Ready command regression.");hud.room.StopHost();SessionState.SetInt(StageKey,4);next=EditorApplication.timeSinceStartup+2;return;
                }
                if(stage==4) {
                    field.text="두번째";hud.room.StartHost();SessionState.SetInt(StageKey,5);next=EditorApplication.timeSinceStartup+3;return;
                }
                if(stage==5) {
                    Require(player&&player.nickname=="두번째","Reconnect retained stale nickname.");hud.room.StopHost();
                    var saved=SessionState.GetString("LobbyNickname.SavedPreference","");if(SessionState.GetBool("LobbyNickname.HadPreference",false))PlayerPrefs.SetString(Nickname.PreferenceKey,saved);else PlayerPrefs.DeleteKey(Nickname.PreferenceKey);PlayerPrefs.Save();
                    File.AppendAllText("Library/LobbyNickname.result","\nPASS: Play mode blank-name guard; Korean host nickname submitted through owned Command + server SyncVar; HUD name; ready command; stop/reconnect with a different name. Remote second-client and physical Korean IME entry not exercised.");
                    File.WriteAllText(LobbyBuilder.Root+"/LobbyNicknameVerification.txt",File.ReadAllText("Library/LobbyNickname.result"));File.Delete(Request);SessionState.SetInt(StageKey,0);EditorApplication.isPlaying=false;AssetDatabase.Refresh();
                }
            }catch(Exception ex){File.WriteAllText("Library/LobbyNickname.result",ex.ToString());File.Delete(Request);SessionState.SetInt(StageKey,0);if(EditorApplication.isPlaying){var room=UnityEngine.Object.FindFirstObjectByType<GameNetworkManager>();if(room&&NetworkServer.active)room.StopHost();EditorApplication.isPlaying=false;}Debug.LogException(ex);}
        }
    }
}
