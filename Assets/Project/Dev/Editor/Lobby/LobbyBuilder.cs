using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    public static class LobbyBuilder
    {
        public const string Root="Assets/Project/UI/Lobby";
        public const string ScenePath=HallLevelBuilder.ScenePath;
        public const string LegacyScenePath="Assets/Project/Scenes/SC_Lobby.unity";
        [MenuItem("Tools/BuyBidBlow/Lobby/Create Sandbox Scene")]
        public static void Build()
        {
            if(File.Exists(ScenePath)) throw new InvalidOperationException("Lobby scene already exists; it will not be overwritten.");
            AssetDatabase.CopyAsset("Assets/Project/Dev/Sandbox/AuctionHall/AuctionHall-Deco.unity",ScenePath);
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                foreach(var root in scene.GetRootGameObjects()) {
                    if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(b=>b && b.GetType().Name=="HallWalkthrough")) UnityEngine.Object.DestroyImmediate(root);
                    else foreach(var camera in root.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
                var wolf=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Characters/Player/Wolf/WolfExpressions/PF_Wolf_Character.prefab"));
                wolf.name="Lobby Player";
                wolf.AddComponent<NetworkIdentity>();wolf.AddComponent<PlayerNet>();PlayerNetSync.Configure(wolf);
                var prefab=PrefabUtility.SaveAsPrefabAsset(wolf,"Assets/Project/Characters/Player/PlayerNet.prefab");
                UnityEngine.Object.DestroyImmediate(wolf);
                var manager=new GameObject("Lobby Network - KCP 7777");
                var transport=manager.AddComponent<kcp2k.KcpTransport>();
                var room=manager.AddComponent<GameNetworkManager>();room.transport=transport;room.playerPrefab=prefab;room.maxConnections=6;room.dontDestroyOnLoad=true;
                var cameraObject=new GameObject("Lobby Fixed Camera");
                var view=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent(Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime",true));
                cameraObject.tag="MainCamera";
                HallSessionSetup.PositionLobbyCamera(view);view.nearClipPlane=.1f;view.farClipPlane=100;
                var key=new GameObject("Lobby Soft Key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.color=new Color(1,.91f,.8f);key.transform.rotation=Quaternion.Euler(30,180,0);
                var ui=new GameObject("Lobby Screen").AddComponent<LobbyScreen>();ui.view=view;ui.room=room;
                ui.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/public/static/alternative/Pretendard-Regular.ttf");
                EditorSceneManager.SaveScene(scene);
            } finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        [InitializeOnLoadMethod] static void RunRequest()
        {
            if(!File.Exists("Library/LobbyBuild.request"))return;
            EditorApplication.delayCall+=()=>{
                File.Delete("Library/LobbyBuild.request");
                try {Build();File.WriteAllText("Library/LobbyBuild.result","OK");}
                catch(Exception ex){File.WriteAllText("Library/LobbyBuild.result",ex.ToString());Debug.LogException(ex);}
            };
        }
    }
}
