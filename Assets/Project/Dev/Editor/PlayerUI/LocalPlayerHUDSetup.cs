using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Network;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using BuyBidBlow.UI.Market;
using BuyBidBlow.UI.Notebook;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class LocalPlayerHUDSetup
    {
        const string PlayerPath="Assets/Project/Characters/Player/Player.prefab";
        static LocalPlayerHUDSetup(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(!File.Exists("Library/LocalPlayerHUDSetup.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Library/LocalPlayerHUDSetup.request");
            try{Apply();File.WriteAllText("Library/LocalPlayerHUDSetup.result","PASS: HUD belongs to spawned Player; local references assigned; scene HUD removed; dedicated server prefab aligned.");}
            catch(Exception e){File.WriteAllText("Library/LocalPlayerHUDSetup.result",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tools/Buy Bid Blow/UI/Attach HUD To Network Player")]
        public static void Apply()
        {
            var player=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var hud=player.GetComponentsInChildren<NetworkPlayerHUD>(true).SingleOrDefault();
                if(!hud)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NetworkHUDSetup.PrefabPath),player.transform);
                    instance.name="Network Player HUD";hud=instance.GetComponent<NetworkPlayerHUD>();
                }
                var movement=player.GetComponent<PlayerMove>();
                var trading=player.GetComponentInChildren<TradePanelController>(true);
                var notebook=hud.GetComponent<NetworkNotebookController>();notebook.movement=movement;notebook.trading=trading;
                var feedback=hud.GetComponent<NetworkMatchFeedback>();feedback.movement=movement;feedback.trading=trading;
                foreach(var money in player.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t=>t.name=="Player Money"))money.gameObject.SetActive(false);
                // Keep all player UI inactive until Mirror identifies the local owner.
                foreach(var canvas in player.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
                player.GetComponent<LocalTradeController>().enabled=false;
                player.GetComponent<SeatController>().enabled=false;
                PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            var originalScene=SceneManager.GetActiveScene();
            EditScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity",scene=>
            {
                var session=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HallSession>(true)).Single();
                foreach(var child in session.gameplayPresentation.transform.Cast<Transform>().Where(t=>t.GetComponent<NetworkPlayerHUD>()).ToArray())Object.DestroyImmediate(child.gameObject);
                session.gameplayPresentation.SetActive(false);
            });
            EditScene("Assets/Project/Scenes/SC_Server.unity",scene=>
            {
                var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
                manager.playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            });
            if(originalScene.IsValid() && originalScene.isLoaded)SceneManager.SetActiveScene(originalScene);
            AssetDatabase.SaveAssets();
        }
        static void EditScene(string path,Action<Scene> edit)
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{edit(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
