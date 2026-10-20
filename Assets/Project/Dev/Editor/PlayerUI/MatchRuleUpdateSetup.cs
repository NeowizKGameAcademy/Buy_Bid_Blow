using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]public static class MatchRuleUpdateSetup
    {
        static MatchRuleUpdateSetup(){EditorApplication.update+=()=>
        {
            if(!File.Exists("Library/MatchRuleUpdate.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Library/MatchRuleUpdate.request");
            try{Apply();File.WriteAllText("Library/MatchRuleUpdate.result","PASS");}catch(Exception e){File.WriteAllText("Library/MatchRuleUpdate.result",e.ToString());Debug.LogException(e);}
        };}
        public static void Apply()
        {
            foreach(var path in new[]{"Assets/Project/UI/Notebook/PF_InformationNotebook.prefab","Assets/Project/UI/HUD/PF_NetworkPlayerHUD.prefab","Assets/Project/UI/Menu/HowToPlay/Prefabs/PF_HowToPlay.prefab","Assets/Project/UI/Menu/PF_MainMenu.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{UpdateTexts(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var pathScene="Assets/Project/Scenes/SC_MainMenu.unity";var scene=SceneManager.GetSceneByPath(pathScene);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(pathScene,OpenSceneMode.Additive);
            try{foreach(var root in scene.GetRootGameObjects())UpdateTexts(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            AssetDatabase.SaveAssets();
        }
        static void UpdateTexts(GameObject root)
        {
            foreach(var text in root.GetComponentsInChildren<Text>(true))
            {
                var value=text.text.Replace("17라운드","13라운드").Replace("17R","13R").Replace("240초","180초")
                    .Replace("2 · 6 · 10 · 14R","2 · 6 · 10R").Replace("2R · 6R · 10R · 14R","2R · 6R · 10R")
                    .Replace("시작 인원 × 4주","시작 인원 × 5 + 5주").Replace("I로 내 정보","Tab으로 내 정보")
                    .Replace("I / Esc 닫기","Q / Tab 닫기").Replace("Tab / Esc 닫기","Q / Tab 닫기");
                if(text.name=="Keyboard Guide" && text.GetComponentInParent<BuyBidBlow.UI.Notebook.InformationNotebookView>(true))value="↑ ↓ 스크롤 · Q / Tab 닫기";
                text.text=value;
            }
        }
    }
}
