using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // SC_ExchangeFurnished를 복사해 노동 수입 시험용 Sandbox 씬을 만든다.
    // 원본 씬과 Build Settings는 건드리지 않는다. 이미 있으면 덮어쓰지 않고 연다.
    public static class LaborSandboxBuilder
    {
        public const string Parent="Assets/Project/Dev/Sandbox";
        public const string Root=Parent+"/Labor";
        public const string ScenePath=Root+"/LaborSandbox.unity";
        const string Source="Assets/Project/Scenes/SC_ExchangeFurnished.unity";

        [MenuItem("Tools/Buy Bid Blow/Labor/Create and Open Sandbox")]
        public static void Build()
        {
            if(File.Exists(ScenePath))
            {
                if(!Application.isBatchMode)EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder(Parent,"Labor");
            if(!AssetDatabase.CopyAsset(Source,ScenePath))throw new Exception("Could not copy "+Source);

            var scene=EditorSceneManager.OpenScene(ScenePath);
            var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
            var config=new SerializedObject(manager);
            Set(config,"minPlayers",p=>p.intValue=1);        // 혼자서도 경기 시작
            Set(config,"prepareSeconds",p=>p.floatValue=2);  // 시작 카운트다운 단축
            config.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("LABOR SANDBOX CREATED: "+ScenePath);
        }
        static void Set(SerializedObject target,string name,Action<SerializedProperty> apply)
        {
            var property=target.FindProperty(name);
            if(property==null){Debug.LogWarning($"[LaborSandbox] GameNetworkManager.{name} not found; left unchanged.");return;}
            apply(property);
        }
    }
}
