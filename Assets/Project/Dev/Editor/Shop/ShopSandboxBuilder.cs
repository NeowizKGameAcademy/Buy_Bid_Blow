using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // 상점 Sandbox 도구.
    // Create and Open Sandbox: SC_ExchangeFurnished를 복사한 씬을 만든다(원본·Build Settings 미변경).
    public static class ShopSandboxBuilder
    {
        public const string Parent = "Assets/Project/Dev/Sandbox";
        public const string Root = Parent + "/Shop";
        public const string ScenePath = Root + "/ShopSandbox.unity";
        const string Source = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";

        [MenuItem("Tools/Buy Bid Blow/Shop/Create and Open Sandbox")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(Parent, "Shop");
            if (!AssetDatabase.CopyAsset(Source, ScenePath)) throw new Exception("Could not copy " + Source);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
            var config = new SerializedObject(manager);
            Set(config, "minPlayers", p => p.intValue = 1);       // 혼자서도 경기 시작
            Set(config, "prepareSeconds", p => p.floatValue = 2); // 시작 카운트다운 단축
            config.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("SHOP SANDBOX CREATED: " + ScenePath);
        }

        static void Set(SerializedObject target, string name, Action<SerializedProperty> apply)
        {
            var property = target.FindProperty(name);
            if (property == null) { Debug.LogWarning($"[ShopSandbox] GameNetworkManager.{name} not found; left unchanged."); return; }
            apply(property);
        }
    }
}
