using System;
using System.Collections.Generic;
using System.IO;
using BuyBidBlow.Audio;
using CueAudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class MatchAudioSetup
    {
        const string Root="Assets/Project/Audio";
        public const string CatalogPath=Root+"/Cues/MatchAudioCatalog.asset";
        public const string HostPath=Root+"/Resources/GameAudioHost.prefab";
        [MenuItem("Tools/Buy Bid Blow/Audio/Connect Match Sounds")]
        public static void Apply()
        {
            var clips=new Dictionary<string,string>{
                {"closing_bell","Market/SFX_Market_PriceReveal.wav"},
                {"auction_start","Auction/SFX_Auction_Start.wav"},{"auction_end","Auction/SFX_Auction_Gavel.wav"},
                {"footstep","Movement/SFX_Footsteps_Wood.wav"},{"jump","Movement/SFX_Jump.wav"},
                {"closing_soon","Auction/SFX_Auction_ClosingSoon.wav"},
                {"navigate","UI/SFX_UI_Navigate.wav"},{"confirm","UI/SFX_UI_Confirm.wav"},{"close","UI/SFX_UI_BackClose.wav"},
                {"quantity","UI/SFX_UI_Quantity.wav"},{"notebook","UI/SFX_UI_Notebook.wav"},{"page","UI/SFX_UI_PageTurn.wav"},
                {"trade","Market/SFX_Trade_Complete.wav"},{"bet","Market/SFX_Bet_Place.wav"}};
            var document=new CueDocument();
            foreach(var pair in clips)document.cues.Add(new CueDefinition{id=Id(pair.Key),clips=new List<string>{pair.Key},selection="first",bus="sfx",volume=pair.Key=="footstep"?.35f:.65f,pitchRange=Vector2.one,spatialBlend=pair.Key=="footstep"||pair.Key=="jump"?1:0,minDistance=1.5f,maxDistance=18,cooldownSeconds=0,maxInstances=4,overflow="stopOldest"});
            document.cues.Add(new CueDefinition{id="bgm.menu",clips=new List<string>{"bgm_menu"},bus="music",volume=.25f,loop=true,maxInstances=1,overflow="stopOldest"});
            document.cues.Add(new CueDefinition{id="bgm.game",clips=new List<string>{"bgm_game"},bus="music",volume=.25f,loop=true,maxInstances=1,overflow="stopOldest"});
            var jsonPath=Root+"/Cues/MatchAudio.json";File.WriteAllText(jsonPath,JsonUtility.ToJson(document,true));AssetDatabase.ImportAsset(jsonPath);
            var catalog=AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<CueCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var so=new SerializedObject(catalog);so.FindProperty("definitions").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            var bindings=so.FindProperty("clips");bindings.arraySize=clips.Count+2;int index=0;
            foreach(var pair in clips)
            {
                var entry=bindings.GetArrayElementAtIndex(index++);entry.FindPropertyRelative("id").stringValue=pair.Key;
                entry.FindPropertyRelative("clip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Clips/CollectedSFX/"+pair.Value);
            }
            foreach(var entry in new[]{new[]{"bgm_menu","BGM_MainMenu.mp3"},new[]{"bgm_game","BGM_InGame.mp3"}})
            {
                var binding=bindings.GetArrayElementAtIndex(index++);binding.FindPropertyRelative("id").stringValue=entry[0];
                binding.FindPropertyRelative("clip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Clips/BGM/"+entry[1]);
                var musicImporter=(AudioImporter)AssetImporter.GetAtPath(Root+"/Clips/BGM/"+entry[1]);var sample=musicImporter.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;musicImporter.defaultSampleSettings=sample;musicImporter.SaveAndReimport();
            }
            so.ApplyModifiedPropertiesWithoutUndo();var errors=catalog.Validate();if(errors.Count>0)throw new Exception(string.Join("\n",errors));
            var host=new GameObject("Match CueAudio Host");host.SetActive(false);
            try
            {
                var audio=host.AddComponent<CueAudioHost>();var hostSO=new SerializedObject(audio);
                hostSO.FindProperty("catalog").objectReferenceValue=catalog;hostSO.FindProperty("persistAcrossScenes").boolValue=true;hostSO.ApplyModifiedPropertiesWithoutUndo();
                var driver=host.AddComponent<SceneAudioDriver>();driver.menuMusic=new CueReference(catalog,"bgm.menu");driver.gameMusic=new CueReference(catalog,"bgm.game");
                Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
                host.SetActive(true);PrefabUtility.SaveAsPrefabAsset(host,HostPath);
            }
            finally{Object.DestroyImmediate(host);}
            var prefab=PrefabUtility.LoadPrefabContents("Assets/Project/Characters/Player/Player.prefab");
            try
            {
                var actions=prefab.GetComponent<PlayerActionAudio>();if(!actions)actions=prefab.AddComponent<PlayerActionAudio>();
                actions.footstep=new CueReference(catalog,Id("footstep"));actions.jump=new CueReference(catalog,Id("jump"));
                PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Project/Characters/Player/Player.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            InstallSpeaker(catalog);
            AssetDatabase.SaveAssets();File.WriteAllText("Library/MatchAudioSetup.result","PASS: catalog validated, existing clips bound, hall speaker installed, action hooks prepared without RPC changes.");
        }
        const string HallScene="Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        // The hall PA speaker sits above the middle ticker board, so announcements come from the boards.
        [MenuItem("Tools/Buy Bid Blow/Audio/Install Hall Speaker")]
        public static void InstallSpeaker()=>InstallSpeaker(AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath));
        public static void InstallSpeakerBatch()
        {
            try{InstallSpeaker();EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void InstallSpeaker(CueCatalog catalog)
        {
            if(!catalog)throw new Exception("Match audio catalog is missing: "+CatalogPath);
            var scene=SceneManager.GetSceneByPath(HallScene);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(HallScene,OpenSceneMode.Additive);
            try
            {
                HallSpeaker speaker=null;GameObject board=null;
                foreach(var root in scene.GetRootGameObjects())
                {
                    if(!speaker)speaker=root.GetComponentInChildren<HallSpeaker>(true);
                    if(!board)foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="TickerBoard_Center"){board=t.gameObject;break;}
                }
                if(!speaker){var go=new GameObject();SceneManager.MoveGameObjectToScene(go,scene);speaker=go.AddComponent<HallSpeaker>();}
                speaker.gameObject.name="Hall Speaker";
                if(!board)throw new Exception("TickerBoard_Center not found in "+HallScene);
                var renderers=board.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                speaker.transform.SetPositionAndRotation(new Vector3(bounds.center.x,bounds.max.y+.5f,bounds.center.z),Quaternion.identity);
                speaker.roundStart=default;speaker.closingBell=new CueReference(catalog,Id("closing_bell"));
                speaker.closingSoon=new CueReference(catalog,Id("closing_soon"));
                speaker.auctionStart=new CueReference(catalog,Id("auction_start"));speaker.auctionEnd=new CueReference(catalog,Id("auction_end"));
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Audio] Hall speaker at {speaker.transform.position} above {board.name}");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static string Id(string key)=>key=="footstep"||key=="jump"?"player."+key:key=="navigate"||key=="confirm"||key=="close"||key=="quantity"||key=="notebook"||key=="page"?"ui."+key:key=="trade"||key=="bet"?"market."+key:"match."+key;
    }
}
