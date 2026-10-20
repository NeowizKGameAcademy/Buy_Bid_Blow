using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    // Puts lounge sofa and bench sitting points on the actual cushion: hips just above its surface and
    // far enough forward that the knees clear the front edge. The builders place them from bounding
    // boxes, which sinks the seated wolf into deep or low furniture. Mezzanine auction chairs are left alone.
    public static class SeatAnchorFitter
    {
        const string ScenePath = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        // Hip joint above the surface it sits on, and hip to the front edge (short of the thigh, so the shins hang clear).
        const float HipAboveCushion = .1f, HipFromFrontEdge = .16f;
        const int ProbeLayer = 30;

        [MenuItem("Tools/BuyBidBlow/Hall/Fit Lounge Seat Anchors")]
        public static void FitOpenScene() => Debug.Log(Fit(SceneManager.GetActiveScene()));

        public static void RunBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                string log = Fit(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/SeatAnchorFitter.result", "PASS\n" + log);
            }
            catch (Exception e) { File.WriteAllText("Logs/SeatAnchorFitter.result", e.ToString()); Debug.LogException(e); }
        }

        static string Fit(Scene scene)
        {
            var log = new StringBuilder();
            foreach (var seat in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Seat>(true)).Where(s => !s.auction))
            {
                var probes = AddProbes(seat);
                try
                {
                    foreach (var point in seat.sittingPoints)
                    {
                        var forward = Vector3.ProjectOnPlane(point.forward, Vector3.up).normalized;
                        var top = Surface(point.position);
                        if (top == null) { log.AppendLine($"{seat.name}/{point.name}: no cushion found, left as is"); continue; }
                        // Walk forward until the cushion drops away: that is its front edge.
                        float edge = 0;
                        for (float d = .02f; d < 1.2f; d += .02f)
                        {
                            var next = Surface(point.position + forward * d);
                            if (next == null || next < top - .08f) break;
                            edge = d;
                        }
                        var position = point.position + forward * Mathf.Max(0, edge - HipFromFrontEdge);
                        position.y = top.Value + HipAboveCushion;
                        log.AppendLine($"{seat.name}/{point.name}: cushion {top:0.00}, edge {edge:0.00} ahead, moved {Vector3.Distance(point.position, position):0.00}");
                        Undo.RecordObject(point, "Fit seat anchor");
                        point.position = position;
                        if (PrefabUtility.IsPartOfPrefabInstance(point))
                            PrefabUtility.ApplyObjectOverride(point, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(point), InteractionMode.AutomatedAction);
                    }
                }
                finally { foreach (var probe in probes) UnityEngine.Object.DestroyImmediate(probe); }
                EditorUtility.SetDirty(seat);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return log.ToString();
        }

        // Temporary mesh colliders on a probe layer: the seats' own box colliders cover the backrest too.
        static List<GameObject> AddProbes(Seat seat)
        {
            var probes = new List<GameObject>();
            foreach (var filter in seat.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh) continue;
                var probe = new GameObject("Seat Probe") { layer = ProbeLayer };
                probe.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
                probe.transform.localScale = filter.transform.lossyScale;
                probe.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                probes.Add(probe);
            }
            Physics.SyncTransforms();
            return probes;
        }

        static float? Surface(Vector3 at) =>
            Physics.Raycast(new Vector3(at.x, at.y + .45f, at.z), Vector3.down, out var hit, 1.2f, 1 << ProbeLayer, QueryTriggerInteraction.Ignore)
                ? hit.point.y : (float?)null;
    }
}
