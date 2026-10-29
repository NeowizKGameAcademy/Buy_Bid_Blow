using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class AuctionLotDisplaySetup
    {
        public const string ScenePath = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        [MenuItem("Tools/Buy Bid Blow/Hall/Install Auction Lot Display")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            // Use the existing collision surface: its exported bounds match the furnished pedestal.
            var pedestal = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(c => c.name == "02_Lot_Pedestal_ROOT");
            var surface = pedestal.bounds;
            var old = pedestal.transform.Find("Auction Lot Display");
            if (old) Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject("Auction Lot Display");
            holder.transform.SetParent(pedestal.transform, true);
            holder.transform.position = new Vector3(surface.center.x, surface.max.y, surface.center.z);
            var seats = Object.FindObjectsByType<Seat>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.auction).ToArray();
            if (seats.Length > 0)
            {
                var toward = seats.Aggregate(Vector3.zero, (sum,s) => sum+s.transform.position) / seats.Length - holder.transform.position;
                toward.y = 0;
                if (toward.sqrMagnitude > .01f) holder.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Project/Content/Props/AuctionEnvelope/PF_AuctionEnvelope.prefab"), holder.transform);
            instance.name = "Auction Envelope";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            var display = instance.GetComponent<AuctionEnvelopeDisplay>();
            display.firstRound = display.secondRound = 0; display.RefreshLabel();
            var bounds = BoundsOf(instance);
            float footprint = Mathf.Max(bounds.size.x,bounds.size.z);
            float scale = Mathf.Min(surface.size.x,surface.size.z) * .8f / footprint;
            instance.transform.localScale = Vector3.one * scale;
            bounds = BoundsOf(instance);
            instance.transform.position += Vector3.up * (surface.max.y + .015f - bounds.min.y);
            var client = holder.AddComponent<AuctionLotDisplayClient>();client.display = display;
            instance.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Library/AuctionLotDisplaySetup.result", $"PASS\nPedestal bounds: {surface}\nEnvelope scale: {scale:F4}\nRoot position: {instance.transform.position:F4}\nTray base above top: 0.015m\nParent stays active; envelope starts hidden.");
        }
        public static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new Exception("The envelope has no renderers.");
            var bounds=renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
