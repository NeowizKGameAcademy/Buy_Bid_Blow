using Mirror;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // PlayerNet's position in the level comes from the owning client (NetworkTransform, client to server).
    public static class PlayerNetSync
    {
        const string PrefabPath = "Assets/Project/Characters/Player/PlayerNet.prefab";

        public static void Configure(GameObject root)
        {
            var sync = root.GetComponent<NetworkTransformUnreliable>();
            if (!sync) sync = root.AddComponent<NetworkTransformUnreliable>();
            sync.target = root.transform;
            sync.syncDirection = SyncDirection.ClientToServer;
            sync.coordinateSpace = CoordinateSpace.World;
            sync.syncPosition = true;
            sync.syncRotation = true;
            sync.syncScale = false;
        }

        [MenuItem("Tools/Buy Bid Blow/Player/Configure PlayerNet Sync")]
        public static void ConfigurePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("PlayerNet sync configured.");
        }

        public static void ConfigureAndBuild()
        {
            try { ConfigurePrefab(); GameBuild.Build(); GameBuild.BuildClient(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
