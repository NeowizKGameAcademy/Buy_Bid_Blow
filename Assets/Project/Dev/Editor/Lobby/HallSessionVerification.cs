using BuyBidBlow.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallSessionVerification
    {
        const string Key="BBB_HallSession_Verification";
        static int stage,loads,errors;
        static double deadline,waitUntil;
        static HallSession session;
        static GameNetworkManager room;
        static GameObject hall,collision,lounge;
        static Texture lightmap;
        static readonly List<LoopbackLobbyPeer> peers=new List<LoopbackLobbyPeer>();
        static readonly StringBuilder report=new StringBuilder();
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Go(int next){stage=next;deadline=EditorApplication.timeSinceStartup+25;}
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            HallSessionSetup.Validate();
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod]
        static void Restore()
        {
            if(!SessionState.GetBool(Key,false))return;
            Go(0);EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string trace,LogType type)
        {
            if(type==LogType.Error||type==LogType.Exception){errors++;File.AppendAllText("Logs/HallSession/runtime-errors.txt",message+"\n"+trace+"\n");}
        }
        static void Loaded(Scene scene,LoadSceneMode mode){loads++;}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout at stage "+stage+" time="+Time.time+" frame="+Time.frameCount+" client="+NetworkClient.isConnected+" ready="+NetworkClient.ready+" connections="+NetworkServer.connections.Count+" players="+Object.FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length);
                foreach(var peer in peers)peer.Tick();
                if(stage==0)
                {
                    if(Time.time<.3f)return;
                    session=Object.FindFirstObjectByType<HallSession>();room=session.room;
                    Check(!session.IsPlaying&&!session.gameplayPresentation.activeInHierarchy&&session.lobbyPresentation.activeInHierarchy,"Lobby must be active before connecting");
                    Check(!Object.FindFirstObjectByType<PlayerSeating>(),"Gameplay input started in lobby");
                    hall=GameObject.Find("AuctionHall_Furnished");collision=hall.transform.Find("HallFurnitureCollision").gameObject;lounge=GameObject.Find("UnderMezzanine_Lounge_Clusters");
                    lightmap=LightmapSettings.lightmaps[0].lightmapColor;
                    SceneManager.sceneLoaded+=Loaded;
                    ((kcp2k.KcpTransport)room.transport).Port=17777;
                    typeof(GameNetworkManager).GetField("prepareSeconds",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(room,.25f);
                    room.ClientConnected+=()=>Debug.Log("FLOW connected");
                    room.AdmissionApproved+=()=>Debug.Log("FLOW approved");
                    room.AdmissionRejected+=reason=>Debug.LogError("FLOW rejected "+reason);
                    room.LocalPlayerReady+=()=>Debug.Log("FLOW player ready");
                    room.LocalNickname="FlowTest";room.StartHost();Go(1);return;
                }
                var local=NetworkClient.localPlayer?NetworkClient.localPlayer.GetComponent<PlayerNet>():null;
                if(stage==1)
                {
                    if(!local||!NetworkClient.ready)return;
                    local.SetReady(true);for(int i=1;i<6;i++)peers.Add(new LoopbackLobbyPeer("Flow"+i));Go(2);return;
                }
                if(stage==2)
                {
                    var players=Object.FindObjectsByType<PlayerNet>(FindObjectsSortMode.None);
                    if(players.Length!=6||peers.Any(p=>!p.Accepted)||!local.ready)return;
                    // The UDP peers exercise admission/spawning; set their server ready
                    // flags here so the real local command can start the match.
                    foreach(var player in players.Where(p=>!p.isLocalPlayer))player.ready=true;
                    Check(room.CanStartMatch,"Six admitted players cannot start");
                    Check(players.Select(p=>p.seat).Distinct().Count()==6,"Duplicate waiting slots");
                    ValidateWaitingArea(players);
                    // This test uses StartHost directly rather than the client-only Join UI.
                    // Admission has completed; reflect that state in the lobby presentation.
                    typeof(ConnectionController).GetMethod("SetState",BindingFlags.Instance|BindingFlags.NonPublic)
                        .Invoke(room.GetComponent<ConnectionController>(),new object[]{ConnectionState.InRoom,"대기실 입장 완료"});
                    CaptureLobby();
                    report.AppendLine("PASS: host client and five independent UDP KCP peers admitted with unique seats.");
                    local.CmdStartMatch();Go(3);return;
                }
                if(stage==3)
                {
                    if(!session.IsPlaying||!MatchNet.Current||MatchNet.Current.round<1)return;
                    Check(loads==0&&SceneManager.GetActiveScene().name==GameNetworkManager.PlayScene,"Match start loaded another scene");
                    Check(GameObject.Find("AuctionHall_Furnished")==hall&&hall.transform.Find("HallFurnitureCollision").gameObject==collision&&GameObject.Find("UnderMezzanine_Lounge_Clusters")==lounge,"Hall geometry was recreated");
                    Check(LightmapSettings.lightmaps[0].lightmapColor==lightmap,"Baked lighting changed at match start");
                    Check(session.gameplayPresentation.activeInHierarchy&&!session.lobbyPresentation.activeInHierarchy,"Presentation did not switch");
                    Check(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"Duplicate gameplay camera");
                    Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"Duplicate audio listener");
                    Check(Vector3.Distance(session.player.transform.position,PlayerNet.MatchStartPosition(local.seat))<.2f,"Player did not start at its match start position");
                    Check(local.inMatch&&room.ClientInMatch,"Client match state missing");
                    report.AppendLine("PASS: lobby -> match -> round 1 with ZERO sceneLoaded events; same hall, furniture collision and lightmap instances.");
                    report.AppendLine("PASS: one camera/listener; lobby HUD hidden; local player activated at its own waiting position.");
                    Go(4);return;
                }
                if(stage==4)
                {
                    foreach(var peer in peers)peer.Disconnect();peers.Clear();
                    room.StopHost();Go(5);return;
                }
                if(stage==5)
                {
                    if(SceneManager.GetActiveScene().name!="SC_MainMenu"||NetworkManager.singleton)return;
                    report.AppendLine("PASS: leaving the match returns to the menu and removes the old manager.");
                    SceneManager.LoadScene(GameNetworkManager.PlayScene);Go(6);return;
                }
                if(stage==6)
                {
                    var fresh=Object.FindFirstObjectByType<HallSession>();if(!fresh)return;
                    Check(!fresh.IsPlaying&&!fresh.gameplayPresentation.activeInHierarchy&&fresh.lobbyPresentation.activeInHierarchy,"Re-entry did not restore lobby mode");
                    Check(Object.FindObjectsByType<GameNetworkManager>(FindObjectsSortMode.None).Length==1,"Duplicate manager on re-entry");
                    Check(errors==0,"Runtime errors: "+errors);
                    report.AppendLine("PASS: re-entering the shared hall restores the lobby with one fresh manager. Runtime errors: 0.");
                    File.WriteAllText("Docs/Arts/Environments/UnityVerification/HallSession_Verification.txt",report.ToString());
                    Finish(0);
                }
            }
            catch(Exception e){File.WriteAllText("Logs/HallSession/verification-error.txt",e.ToString()+"\n"+report);Debug.LogException(e);Finish(1);}
        }
        static void ValidateWaitingArea(PlayerNet[] players)
        {
            var camera=session.lobbyPresentation.GetComponentInChildren<LobbyHud>().view;
            Physics.SyncTransforms();
            foreach(var player in players)
            {
                var p=player.transform.position;
                Check(Mathf.Abs(p.x)<5.6f&&p.z>5.6f&&p.z<13.6f,"Waiting player outside the protected under-mezzanine area");
                Check(Vector3.Distance(p,PlayerNet.SeatPosition(player.seat))<.02f,"Avatar is not at its waiting slot");
                Check(!Physics.CheckCapsule(p+Vector3.up*.35f,p+Vector3.up*1.5f,.28f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Waiting slot intersects furniture: "+player.seat);
                foreach(float height in new[]{.1f,3f})
                {
                    var screen=camera.WorldToViewportPoint(p+Vector3.up*height);
                    Check(screen.z>0&&screen.x>.02f&&screen.x<.98f&&screen.y>.02f&&screen.y<.98f,"Waiting avatar does not fit in the lobby camera");
                }
            }
            Check(camera.transform.position.z<5.6f&&camera.transform.forward.z>0,"Lobby camera faces the old side of the hall");
            report.AppendLine("PASS: all six waiting slots lie on the central under-mezzanine area, fit the lobby camera and are clear of furniture colliders.");
        }

        static void CaptureLobby()
        {
            var hud=session.lobbyPresentation.GetComponentInChildren<LobbyHud>();var camera=hud.view;
            var canvas=hud.GetComponentInChildren<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;
            float distance=canvas.planeDistance,aspect=camera.aspect;
            var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.aspect=16f/9f;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
                Canvas.ForceUpdateCanvases();hud.Refresh(false);Canvas.ForceUpdateCanvases();ShaderUtil.allowAsyncCompilation=false;
                VolumeManager.instance.Update(camera.transform,camera.GetUniversalAdditionalCameraData().volumeLayerMask);
                camera.Render();camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
                File.WriteAllBytes("Docs/Arts/Environments/UnityVerification/MatchingArea_Lobby.png",image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=null;camera.targetTexture=null;camera.aspect=aspect;
                canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;
                Object.DestroyImmediate(image);Object.DestroyImmediate(target);
            }
        }
        static void Finish(int code)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;SceneManager.sceneLoaded-=Loaded;Application.logMessageReceived-=Log;
            foreach(var peer in peers)peer.Disconnect();peers.Clear();EditorApplication.Exit(code);
        }
    }
}
