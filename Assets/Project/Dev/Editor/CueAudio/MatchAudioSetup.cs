using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Audio;
using CueAudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // MatchAudio.json is the only place to set match audio: each cue lists its clip files by asset path, and buses
    // carry their volumes. MatchAudioCatalog is generated from it so builds include the clips; don't edit it by hand.
    public static class MatchAudioSetup
    {
        const string Root="Assets/Project/Audio";
        public const string JsonPath=Root+"/Cues/MatchAudio.json";
        public const string CatalogPath=Root+"/Cues/MatchAudioCatalog.asset";
        public const string HostPath=Root+"/Resources/GameAudioHost.prefab";

        // The fields of MatchAudio.json that CueDocument does not read.
        [Serializable] sealed class BusSetting{public string id;public float volume=1;}
        [Serializable] sealed class Settings{public List<BusSetting> buses=new List<BusSetting>();}

        // Rebuilds the catalog's clip bindings and bus volumes from the JSON. Returns validation errors.
        public static List<string> Sync()
        {
            var json=AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if(!json)throw new Exception("Match audio settings are missing: "+JsonPath);
            var document=CueDocument.Parse(json.text);
            var settings=JsonUtility.FromJson<Settings>(json.text);
            var catalog=AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<CueCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var so=new SerializedObject(catalog);
            so.FindProperty("definitions").objectReferenceValue=json;
            var paths=document.cues.SelectMany(c=>c.clips).Distinct().ToList();
            var bindings=so.FindProperty("clips");bindings.arraySize=paths.Count;
            for(int i=0;i<paths.Count;i++)
            {
                var entry=bindings.GetArrayElementAtIndex(i);entry.FindPropertyRelative("id").stringValue=paths[i];
                entry.FindPropertyRelative("clip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
            }
            // Keeps any mixer group assigned in the catalog; only ids and volumes come from the JSON.
            var buses=so.FindProperty("buses");var mixers=new Dictionary<string,Object>();
            for(int i=0;i<buses.arraySize;i++){var b=buses.GetArrayElementAtIndex(i);mixers[b.FindPropertyRelative("id").stringValue]=b.FindPropertyRelative("mixerGroup").objectReferenceValue;}
            buses.arraySize=settings.buses.Count;
            for(int i=0;i<settings.buses.Count;i++)
            {
                var b=buses.GetArrayElementAtIndex(i);b.FindPropertyRelative("id").stringValue=settings.buses[i].id;
                b.FindPropertyRelative("volume").floatValue=settings.buses[i].volume;
                b.FindPropertyRelative("mixerGroup").objectReferenceValue=mixers.TryGetValue(settings.buses[i].id,out var mixer)?mixer:null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            // Music streams from disk instead of sitting decoded in memory.
            foreach(var cue in document.cues.Where(c=>c.bus=="music"))
                foreach(var path in cue.clips)
                {
                    if(!(AssetImporter.GetAtPath(path) is AudioImporter importer))continue;
                    var sample=importer.defaultSampleSettings;if(sample.loadType==AudioClipLoadType.Streaming)continue;
                    sample.loadType=AudioClipLoadType.Streaming;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
                }
            var errors=catalog.Validate();
            if(errors.Count>0)Debug.LogError("[Audio] "+JsonPath+":\n"+string.Join("\n",errors),json);
            return errors;
        }

        // Saving the JSON, or importing a clip it may list, updates the catalog.
        sealed class SyncOnImport:AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
            {
                if(imported.Concat(deleted).Concat(moved).Any(p=>p==JsonPath||p.StartsWith(Root+"/Clips/",StringComparison.Ordinal)&&!p.EndsWith(".meta")))
                    EditorApplication.delayCall+=()=>Sync();
            }
        }

        [MenuItem("Tools/Buy Bid Blow/Audio/Connect Match Sounds")]
        public static void Apply()
        {
            var errors=Sync();if(errors.Count>0)throw new Exception(string.Join("\n",errors));
            var catalog=AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            var host=new GameObject("Match CueAudio Host");host.SetActive(false);
            try
            {
                var audio=host.AddComponent<CueAudioHost>();var hostSO=new SerializedObject(audio);
                hostSO.FindProperty("catalog").objectReferenceValue=catalog;hostSO.FindProperty("persistAcrossScenes").boolValue=true;hostSO.ApplyModifiedPropertiesWithoutUndo();
                var driver=host.AddComponent<SceneAudioDriver>();driver.menuMusic=new CueReference(catalog,"bgm.menu");driver.gameMusic=new CueReference(catalog,"bgm.game");
                System.IO.Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
                host.SetActive(true);PrefabUtility.SaveAsPrefabAsset(host,HostPath);
            }
            finally{Object.DestroyImmediate(host);}
            var prefab=PrefabUtility.LoadPrefabContents("Assets/Project/Characters/Player/Player.prefab");
            try
            {
                var actions=prefab.GetComponent<PlayerActionAudio>();if(!actions)actions=prefab.AddComponent<PlayerActionAudio>();
                actions.footstep=new CueReference(catalog,"player.footstep");actions.jump=new CueReference(catalog,"player.jump");
                PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Project/Characters/Player/Player.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            InstallSpeaker(catalog);
            AssetDatabase.SaveAssets();System.IO.File.WriteAllText("Library/MatchAudioSetup.result","PASS: catalog synced from MatchAudio.json, hall speaker installed, action hooks prepared without RPC changes.");
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
                speaker.matchStart=new CueReference(catalog,"match.start");speaker.matchEnd=new CueReference(catalog,"match.end");
                speaker.roundTurn=new CueReference(catalog,"match.round_turn");speaker.closingSoon=new CueReference(catalog,"match.closing_soon");
                speaker.auctionStart=new CueReference(catalog,"match.auction_start");speaker.auctionEnd=new CueReference(catalog,"match.auction_end");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Audio] Hall speaker at {speaker.transform.position} above {board.name}");
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
