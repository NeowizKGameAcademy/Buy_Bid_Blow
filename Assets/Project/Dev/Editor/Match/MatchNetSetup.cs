using BuyBidBlow.Common;
using BuyBidBlow.Network;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // Creates the MatchNet prefab and assigns it to the lobby's GameNetworkManager.
    // The server scene copies that manager on the next server build.
    public static class MatchNetSetup
    {
        const string PrefabPath = "Assets/Project/Common/MatchNet.prefab";
        const string LobbyScene = LobbyBuilder.ScenePath;

        [MenuItem("Tools/Buy Bid Blow/Match/Set Up MatchNet")]
        public static void Setup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab)
            {
                var go = new GameObject("MatchNet", typeof(NetworkIdentity), typeof(MatchNet));
                prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                Object.DestroyImmediate(go);
            }
            var scene = EditorSceneManager.OpenScene(LobbyScene);
            var manager = Object.FindFirstObjectByType<GameNetworkManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("matchNetPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MatchNet set up: " + PrefabPath);
        }

        public static void SetupBatch()
        {
            try { Setup(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
