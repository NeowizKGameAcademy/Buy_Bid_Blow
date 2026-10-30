using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // The twin staircases (z 3..5, climbing outward along X to the 4 m mezzanine) are 0.167 m steps in
    // StructuralCollision; a capsule rolling over every nosing loses the ground and shakes. An invisible box
    // laid on the nosings turns each flight into one smooth slope. Measured from the collision itself.
    public static class HallStairRamps
    {
        const string Prefix = "StairRamp_";
        const float Lift = .01f, Thickness = .2f, Sample = .01f;

        // Adds (or replaces) the ramps under the hall root. Needs the hall's physics in an open scene.
        public static void Attach(Transform hall)
        {
            foreach (var old in hall.Cast<Transform>().Where(t => t.name.StartsWith(Prefix)).ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var structure = hall.Find("StructuralCollision").GetComponent<MeshCollider>();
            Physics.SyncTransforms();
            foreach (int side in new[] { -1, 1 })
            {
                var flight = Measure(structure, side);
                var go = new GameObject(Prefix + (side < 0 ? "Left" : "Right")) { layer = structure.gameObject.layer };
                go.transform.SetParent(hall, false);
                var up = new Vector2(flight.Top.x - flight.Foot.x, flight.Top.y - flight.Foot.y);
                float length = up.magnitude, angle = Mathf.Atan2(up.y, Mathf.Abs(up.x)) * Mathf.Rad2Deg;
                var mid = (flight.Foot + flight.Top) / 2;
                // Box top face on the line through the nosings, a little above so no step edge pokes through.
                var rotation = Quaternion.Euler(0, 0, side * angle);
                go.transform.SetPositionAndRotation(new Vector3(mid.x, mid.y + Lift, flight.Center) + rotation * Vector3.down * (Thickness / 2), rotation);
                go.AddComponent<BoxCollider>().size = new Vector3(length, Thickness, flight.Width);
                Debug.Log($"STAIR RAMP {go.name}: {flight.Foot} -> {flight.Top}, {angle:F1}°, z {flight.Center - flight.Width / 2:F2}..{flight.Center + flight.Width / 2:F2}");
            }
        }

        struct Flight { public Vector2 Foot, Top; public float Center, Width; }

        // Height of the structural collision alone, probed from just above where the step should be (the
        // mezzanine overhangs parts of the flights).
        static float Height(MeshCollider structure, float x, float z, float above) =>
            structure.Raycast(new Ray(new Vector3(x, above + .6f, z), Vector3.down), out var hit, 1.2f) ? hit.point.y : float.NaN;

        static Flight Measure(MeshCollider structure, int side)
        {
            const float z0 = 4, floor = 0, mezzanine = 4;
            // Risers along X: where the profile jumps up by a step.
            var risers = new List<Vector2>();
            float previous = floor;
            for (float d = 3; d < 16; d += Sample)
            {
                float h = Height(structure, side * d, z0, previous);
                if (float.IsNaN(h)) continue;
                if (h > previous + .05f) risers.Add(new Vector2(side * d, h));
                previous = h;
                if (h >= mezzanine - .01f) break;
            }
            if (risers.Count < 3) throw new InvalidOperationException($"No staircase found on side {side}");
            float run = Mathf.Abs(risers[risers.Count - 1].x - risers[0].x) / (risers.Count - 1);
            // The nosing line meets the floor one tread before the first riser.
            var foot = new Vector2(risers[0].x - side * run * risers[0].y / (risers[1].y - risers[0].y), floor);
            var top = risers[risers.Count - 1];
            // Width: the run of z around the stairs' centre where the mid-flight height matches the flight.
            float midX = (foot.x + top.x) / 2, expected = Mathf.Lerp(floor, top.y, (midX - foot.x) / (top.x - foot.x));
            bool OnFlight(float z) { float h = Height(structure, midX, z, expected); return !float.IsNaN(h) && Mathf.Abs(h - expected) < .25f; }
            float zMin = z0, zMax = z0;
            while (OnFlight(zMin - Sample) && zMin > z0 - 3) zMin -= Sample;
            while (OnFlight(zMax + Sample) && zMax < z0 + 3) zMax += Sample;
            return new Flight { Foot = foot, Top = top, Center = (zMin + zMax) / 2, Width = zMax - zMin };
        }

        [MenuItem("Tools/Buy Bid Blow/Hall/Add Stair Ramps")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var hall = scene.GetRootGameObjects().Single(g => g.name == "AuctionHall_Furnished").transform;
            Attach(hall);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            // The hall prefab gets the same boxes; its contents have no physics scene, so copy the measured poses.
            string path = HallLevelBuilder.Root + "/AuctionHall_Furnished.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var old in prefab.transform.Cast<Transform>().Where(t => t.name.StartsWith(Prefix)).ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (var ramp in hall.Cast<Transform>().Where(t => t.name.StartsWith(Prefix)))
                {
                    var copy = new GameObject(ramp.name) { layer = ramp.gameObject.layer };
                    copy.transform.SetParent(prefab.transform, false);
                    copy.transform.SetLocalPositionAndRotation(ramp.localPosition, ramp.localRotation);
                    copy.AddComponent<BoxCollider>().size = ramp.GetComponent<BoxCollider>().size;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        public static void ApplyBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
