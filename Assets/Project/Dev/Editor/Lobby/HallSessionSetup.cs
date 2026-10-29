using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Notebook;
using BuyBidBlow.UI.Result;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallSessionSetup
    {
        const string Backup="Logs/HallSession";

        // Wire the existing production views and private player state without rebuilding the hall.
        [MenuItem("Tools/BuyBidBlow/Lobby/Wire Match Results And Information")]
        public static void WireMatchUI()
        {
            const string playerPath = "Assets/Project/Characters/Player/Player.prefab";
            const string netPath = "Assets/Project/Characters/Player/PlayerNet.prefab";
            var player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var hud = player.GetComponentInChildren<HUDController>(true);
                if (!hud) throw new InvalidOperationException("Player prefab has no HUDController.");
                var data = new SerializedObject(hud);
                data.FindProperty("notebookPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InformationNotebookView>("Assets/Project/UI/Notebook/PF_InformationNotebook.prefab");
                data.FindProperty("resultsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MatchResultView>("Assets/Project/UI/Result/PF_MatchResults.prefab");
                data.ApplyModifiedPropertiesWithoutUndo();
                var prompt = data.FindProperty("InteractionText").objectReferenceValue as TMPro.TMP_Text;
                if (prompt) prompt.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Font/TMP/Pretendard 500 SDF.asset");
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            var net = PrefabUtility.LoadPrefabContents(netPath);
            try
            {
                var state = net.GetComponent<PlayerAssetsNet>();
                if (!state) state = net.AddComponent<PlayerAssetsNet>();
                state.syncMode = Mirror.SyncMode.Owner;
                PrefabUtility.SaveAsPrefabAsset(net, netPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(net); }
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var events = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (!events) events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
            events.transform.SetParent(null, true); events.gameObject.SetActive(true);
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module)
            {
                module.AssignDefaultActions(); module.move = null; module.submit = null; module.cancel = null;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            BuyBidBlow.Dev.WolfAnimatorBuilder.EnsureJumpState();
            AssetDatabase.SaveAssets();
        }

        public static void WireMatchUIBatch()
        {
            try { WireMatchUI(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        [MenuItem("Tools/BuyBidBlow/Lobby/Use Furnished Hall For Lobby And Match")]
        public static void Apply()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Backup);
            string backup=Backup+"/SC_ExchangeFurnished_before_session.unity";
            if(!File.Exists(backup))File.Copy(HallLevelBuilder.ScenePath,backup);
            var hall=EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var existing=Object.FindFirstObjectByType<HallSession>();
            if(!existing)
            {
                var player=Object.FindFirstObjectByType<PlayerSeating>();
                if(!player)throw new Exception("The hall must have one local gameplay player.");
                var gameplayCamera=player.GetComponentInChildren<Camera>(true);
                var lightmaps=LightmapSettings.lightmaps;
                var lightingData=Lightmapping.lightingDataAsset;
                var lobby=EditorSceneManager.OpenScene(LobbyBuilder.LegacyScenePath,OpenSceneMode.Additive);
                GameObject presentation=null;
                try
                {
                    var hud=lobby.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyHud>(true)).Single();
                    // Clone the four related roots together so all serialized UI/room
                    // references remap to their copies, not to the retired scene.
                    var bundle=new GameObject("Lobby Presentation");
                    EditorSceneManager.MoveGameObjectToScene(bundle,lobby);
                    foreach(var root in new[]{hud.transform.root,hud.room.transform.root,hud.view.transform.root,hud.selectionRing.root}.Distinct())
                        root.SetParent(bundle.transform,true);
                    presentation=Object.Instantiate(bundle);
                    presentation.name="Lobby Presentation";
                    EditorSceneManager.MoveGameObjectToScene(presentation,hall);
                }
                finally { EditorSceneManager.CloseScene(lobby,true); SceneManager.SetActiveScene(hall); }
                var copiedHud=presentation.GetComponentInChildren<LobbyHud>(true);
                var manager=copiedHud.room;
                // Mirror's persistent manager must remain a root GameObject.
                manager.transform.SetParent(null,true);
                var gameplay=new GameObject("Gameplay Presentation");
                gameplay.SetActive(false);
                player.transform.SetParent(gameplay.transform,true);
                var session=new GameObject("Hall Session").AddComponent<HallSession>();
                session.room=manager;session.lobbyPresentation=presentation;session.gameplayPresentation=gameplay;session.player=player;
                var view=copiedHud.view;
                view.allowHDR=gameplayCamera.allowHDR;
                var lobbyData=view.GetUniversalAdditionalCameraData();
                var gameData=gameplayCamera.GetUniversalAdditionalCameraData();
                lobbyData.renderPostProcessing=gameData.renderPostProcessing;
                lobbyData.antialiasing=gameData.antialiasing;
                lobbyData.antialiasingQuality=gameData.antialiasingQuality;
                // Opening the legacy scene must not replace the approved bake.
                LightmapSettings.lightmaps=lightmaps;
                Lightmapping.lightingDataAsset=lightingData;
                if(!session.room.GetComponent<ConnectionController>())throw new Exception("Missing connection controller.");
                EditorSceneManager.SaveScene(hall);
            }
            AlignWaitingArea();
            // Retire the old backdrop scene from the executable routes.
            EditorBuildSettings.scenes=EditorBuildSettings.scenes
                .Where(s=>s.path!=LobbyBuilder.LegacyScenePath).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            Validate();
        }

        public static void PositionLobbyCamera(Camera camera)
        {
            // Preserve the original lobby framing, facing the waiting line in the lobby room.
            camera.transform.position=PlayerNet.WaitingCenter+new Vector3(0,3.26f,-9.7f);
            camera.transform.LookAt(PlayerNet.WaitingCenter+Vector3.up*1.56f);
            camera.fieldOfView=48;
        }

        [MenuItem("Tools/BuyBidBlow/Lobby/Align Lobby Waiting Area")]
        public static void AlignWaitingArea()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Backup);
            string backup=Backup+"/Before_WaitingArea.unity";
            if(!File.Exists(backup))File.Copy(HallLevelBuilder.ScenePath,backup);
            var scene=EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var session=Object.FindFirstObjectByType<HallSession>();
            if(!session)throw new Exception("Missing shared hall session.");
            var hud=session.lobbyPresentation.GetComponentInChildren<LobbyHud>(true);
            PositionLobbyCamera(hud.view);
            hud.selectionRing.position=PlayerNet.SeatPosition(0)+Vector3.up*.02f;
            // The actual match starts at the admitted player's own seat in BeginMatch.
            session.player.transform.SetPositionAndRotation(PlayerNet.MatchStartPosition(0),Quaternion.Euler(0,180,0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(session.player.transform);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Validate();
        }

        public static void AlignWaitingAreaAndVerifyBatch()
        {
            try { AlignWaitingArea();HallSessionVerification.RunBatch(); }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }

        public static void Validate()
        {
            var session=Object.FindFirstObjectByType<HallSession>();
            if(!session||!session.room||!session.player||!session.lobbyPresentation||!session.gameplayPresentation)
                throw new Exception("Incomplete hall session setup.");
            if(session.gameplayPresentation.activeSelf||!session.lobbyPresentation.activeSelf)
                throw new Exception("The hall must initially show the lobby, with gameplay input inactive.");
            if(Object.FindObjectsByType<GameNetworkManager>(FindObjectsSortMode.None).Length!=1)
                throw new Exception("Duplicate network managers.");
            var hud=session.lobbyPresentation.GetComponentInChildren<LobbyHud>();
            if(hud.room!=session.room||!hud.selectionRing||!hud.view)
                throw new Exception("Lobby references still point to the old scene.");
            if(!GameObject.Find("AuctionHall_Furnished")||!GameObject.Find("UnderMezzanine_Lounge_Clusters")||!GameObject.Find("Hall_Lighting_Bright_Jewel"))
                throw new Exception("Approved hall content is missing.");
            if(LightmapSettings.lightmaps.Length==0||!LightmapSettings.lightProbes||LightmapSettings.lightProbes.count<100)
                throw new Exception("Approved lightmaps or probes missing.");
            if(!GameObject.Find("AuctionHall_Furnished").transform.Find("HallFurnitureCollision"))
                throw new Exception("Furniture collision missing.");
            if(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)!=1 || Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled)!=1)
                throw new Exception("Lobby must have exactly one enabled camera and audio listener.");
            Debug.Log("HALL_SESSION_SETUP_OK");
        }
        public static void RunBatch()
        {
            try { Apply();EditorApplication.Exit(0); }
            catch(Exception e) { Directory.CreateDirectory(Backup);File.WriteAllText(Backup+"/setup-error.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1); }
        }
    }
}
