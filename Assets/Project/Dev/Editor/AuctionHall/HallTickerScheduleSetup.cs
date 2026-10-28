using System;
using System.Linq;
using BuyBidBlow.UI.Ticker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // Builds the ticker director prefab (schedule JSON + videos) and places one in the hall scene.
    public static class HallTickerScheduleSetup
    {
        const string Root="Assets/Project/Content/Props/TickerBoard";
        const string Prefab=Root+"/TickerDirector.prefab";
        const string Schedule=Root+"/TickerSchedule.json";
        const string ScenePath="Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        static readonly (string id,string path)[] Videos={("roundover",Root+"/Video/RoundOver.mp4"),("transition",Root+"/Video/Transition.mp4")};

        [MenuItem("BuyBidBlow/Level/Install ticker schedule")]
        public static void Install()
        {
            var json=AssetDatabase.LoadAssetAtPath<TextAsset>(Schedule);
            if(!json)throw new Exception("Missing "+Schedule);
            var entries=Videos.Select(v=>new TickerDirector.VideoEntry{id=v.id,clip=AssetDatabase.LoadAssetAtPath<VideoClip>(v.path)}).ToArray();
            foreach(var e in entries)if(!e.clip)throw new Exception("Missing video clip "+e.id);
            var errors=TickerSchedule.FromJson(json.text).Validate(entries.Select(e=>e.id).ToList());
            if(errors.Count>0)throw new Exception("Ticker schedule:\n"+string.Join("\n",errors));

            var go=new GameObject("Ticker Director");
            try
            {
                var director=go.AddComponent<TickerDirector>();director.schedule=json;director.videos=entries;
                var welcome=AssetDatabase.LoadAssetAtPath<VideoClip>(Root+"/Video/Welcome.mp4");
                if(!welcome)throw new Exception("Missing welcome video");
                var serialized=new SerializedObject(director);
                serialized.FindProperty("welcomeClip").objectReferenceValue=welcome;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,Prefab);
            }
            finally{Object.DestroyImmediate(go);}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);

            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                bool placed=scene.GetRootGameObjects().Any(r=>r.GetComponentInChildren<TickerDirector>(true));
                if(!placed){var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.name="Ticker Director";}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();
            Debug.Log("[Ticker] Schedule director installed: "+Prefab);
        }

        public static void InstallBatch()
        {
            try{Install();EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
