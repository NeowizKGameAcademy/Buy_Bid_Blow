using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // Removes invisible plates: thin, wide BoxColliders with almost no visible geometry inside. The exported
    // curved railing round the auction rostrum came with two of them (a false floor at 4.32 m and a false
    // ceiling at 5.0 m over the mezzanine) that stopped players walking up to the auctioneer.
    public static class HallPhantomCollision
    {
        const float MaxThickness = .3f, MinFootprint = 2f, MaxFilled = .25f;

        struct Phantom { public string Path; public Vector3 Center, Size; public Bounds World; }

        // Fraction of sample points across the box (mid-height and top face) that touch visible geometry.
        static float Filled(BoxCollider box)
        {
            var t = box.transform; var half = box.size / 2; int samples = 0, hits = 0;
            for (float u = -.9f; u <= .91f; u += .3f)
                for (float v = -.9f; v <= .91f; v += .3f)
                {
                    samples++;
                    var mid = t.TransformPoint(box.center + new Vector3(u * half.x, 0, v * half.z));
                    var top = t.TransformPoint(box.center + new Vector3(u * half.x, half.y, v * half.z));
                    if (Physics.CheckSphere(mid, .02f) || Physics.Raycast(top + Vector3.up * .01f, Vector3.down, Vector3.Distance(top, mid) * 2)) hits++;
                }
            return (float)hits / samples;
        }

        static List<Phantom> Find(Transform hall)
        {
            var boxes = hall.GetComponentsInChildren<BoxCollider>().Where(b => b.enabled && !b.isTrigger).ToArray();
            Physics.SyncTransforms();
            var world = boxes.ToDictionary(b => b, b => b.bounds);
            var candidates = boxes.Where(b => { var s = Vector3.Scale(b.size, b.transform.lossyScale);
                var dims = new[] { Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z) }.OrderBy(d => d).ToArray();
                return dims[0] < MaxThickness && dims[1] * dims[2] > MinFootprint; }).ToArray();
            // Only visible meshes answer the probes: every collider goes quiet, temporary mesh colliders stand in.
            var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
            foreach (var c in colliders) c.enabled = false;
            var probes = new List<GameObject>();
            try
            {
                foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    var r = mf.GetComponent<Renderer>();
                    if (!mf.sharedMesh || !r || !r.enabled || !candidates.Any(b => world[b].Intersects(r.bounds))) continue;
                    var probe = new GameObject("Visible Probe");
                    probe.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                    probe.transform.localScale = mf.transform.lossyScale;
                    probe.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    probes.Add(probe);
                }
                Physics.SyncTransforms();
                return candidates.Where(b => Filled(b) < MaxFilled).Select(b => new Phantom
                    { Path = PathOf(b.transform, hall), Center = b.center, Size = b.size, World = world[b] }).ToList();
            }
            finally
            {
                foreach (var p in probes) UnityEngine.Object.DestroyImmediate(p);
                foreach (var c in colliders) if (c) c.enabled = true;
            }
        }

        static string PathOf(Transform t, Transform root)
        {
            var path = t.name;
            for (t = t.parent; t && t != root; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        static int Remove(Transform hall, IEnumerable<Phantom> phantoms)
        {
            int removed = 0;
            foreach (var p in phantoms)
            {
                var owner = hall.Find(p.Path);
                var box = owner ? owner.GetComponents<BoxCollider>().FirstOrDefault(b => b.center == p.Center && b.size == p.Size) : null;
                if (!box) continue;
                UnityEngine.Object.DestroyImmediate(box); removed++;
            }
            return removed;
        }

        [MenuItem("Tools/Buy Bid Blow/Hall/Remove Phantom Collision")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var hall = scene.GetRootGameObjects().Single(g => g.name == "AuctionHall_Furnished").transform;
            var phantoms = Find(hall);
            foreach (var p in phantoms) Debug.Log($"PHANTOM {p.Path} center {p.Center} size {p.Size} world {p.World.min:F2}..{p.World.max:F2}");
            Debug.Log($"PHANTOM removed {Remove(hall, phantoms)} from the scene");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            string path = HallLevelBuilder.Root + "/AuctionHall_Furnished.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try { Debug.Log($"PHANTOM removed {Remove(prefab.transform, phantoms)} from the hall prefab"); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        public static void ApplyBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
