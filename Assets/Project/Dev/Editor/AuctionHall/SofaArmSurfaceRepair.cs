using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // The original mirrored roll used identical winding on both sides.
    // Repair only the inward-facing roll and end cap; preserve both UV sets and positions.
    public static class SofaArmSurfaceRepair
    {
        const string Level = "Assets/Project/Content/Levels/ExchangeFurnished";
        const string Source = Level + "/Meshes/A88_Red_Sofa_ROOT_118.asset";
        const string Baked = Level + "/Lighting/HallPolish/Meshes/da5f09fa3c9044b45980526d867024ee_4300000.asset";
        const string Output = "Docs/Arts/Environments/Lighting/SofaArmRepair";
        static readonly Quaternion Unrotate = Quaternion.Euler(0, -45, 0);
        static int frames;
        static Action pending;

        sealed class Arm
        {
            public int[] vertices;
            public int[] triangles;
            public float facing;
        }

        static Arm[] Inspect(Mesh mesh)
        {
            var v = mesh.vertices.Select(p => Unrotate * p).ToArray();
            var t = mesh.triangles;
            var parent = Enumerable.Range(0, v.Length).ToArray();
            int Find(int a) { while (parent[a] != a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; }
            void Join(int a, int b) { parent[Find(b)] = Find(a); }
            var welded = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < v.Length; i++)
            {
                var key = Vector3Int.RoundToInt(v[i] * 100000);
                if (welded.TryGetValue(key, out int other)) Join(i, other); else welded[key] = i;
            }
            for (int i = 0; i < t.Length; i += 3) { Join(t[i], t[i + 1]); Join(t[i], t[i + 2]); }
            var groups = Enumerable.Range(0, v.Length).GroupBy(Find).ToArray();
            var arms = new List<Arm>();
            foreach (var group in groups)
            {
                var bounds = new Bounds(v[group.First()], Vector3.zero);
                foreach (int index in group) bounds.Encapsulate(v[index]);
                if (Mathf.Abs(Mathf.Abs(bounds.center.x) - 1) > .02f) continue;
                bool roll = Mathf.Abs(bounds.center.y - .92f) < .02f && Mathf.Abs(bounds.size.x - .312f) < .02f
                    && Mathf.Abs(bounds.size.y - .312f) < .02f && Mathf.Abs(bounds.size.z - .89f) < .02f;
                bool cap = Mathf.Abs(bounds.center.y - .6523f) < .02f && Mathf.Abs(bounds.size.x - .3091f) < .02f
                    && Mathf.Abs(bounds.size.y - .8446f) < .02f && Mathf.Abs(bounds.size.z - .055f) < .01f;
                if (!roll && !cap) continue;
                var triangles = Enumerable.Range(0, t.Length / 3).Where(i => Find(t[i * 3]) == group.Key).Select(i => i * 3).ToArray();
                float sign = Mathf.Sign(bounds.center.x);
                var scores = new List<float>();
                foreach (int i in triangles)
                {
                    var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]];
                    var center = (a + b + c) / 3;
                    var normal = Vector3.Cross(b - a, c - a).normalized;
                    if (roll) scores.Add(Vector3.Dot(normal, new Vector3(center.x - sign, center.y - .92f, 0).normalized));
                    else if (center.z < -.445f && Mathf.Abs(normal.z) > .8f) scores.Add(-normal.z);
                }
                float facing = scores.Average();
                if (roll && triangles.Length != 2936 || cap && triangles.Length != (sign < 0 ? 458 : 459))
                    throw new InvalidOperationException("Unexpected arm topology: " + triangles.Length);
                arms.Add(new Arm { vertices = group.ToArray(), triangles = triangles, facing = facing });
            }
            if (arms.Count != 4) throw new InvalidOperationException("Could not identify both rolls and end caps in " + mesh.name);
            return arms.ToArray();
        }

        static int Repair(Mesh mesh)
        {
            var positions = mesh.vertices; var uv = mesh.uv; var uv2 = mesh.uv2;
            var triangles = mesh.triangles; var normals = mesh.normals; var tangents = mesh.tangents;
            var reversed = Inspect(mesh).Where(a => a.facing < -.9f).ToArray();
            foreach (var arm in reversed)
            {
                foreach (int i in arm.triangles) (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                foreach (int i in arm.vertices)
                {
                    normals[i] = -normals[i];
                    // Keep the tangent/bitangent basis consistent with the flipped normal.
                    if (tangents.Length > i) tangents[i].w = -tangents[i].w;
                }
            }
            mesh.triangles = triangles; mesh.normals = normals; mesh.tangents = tangents;
            if (!positions.SequenceEqual(mesh.vertices) || !uv.SequenceEqual(mesh.uv) || !uv2.SequenceEqual(mesh.uv2))
                throw new InvalidOperationException("Repair modified positions or UVs.");
            if (Inspect(mesh).Any(a => a.facing < .9f)) throw new InvalidOperationException("Arm surfaces still face inward.");
            return reversed.Sum(a => a.triangles.Length);
        }

        public static void PreviewBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                var source = AssetDatabase.LoadAssetAtPath<Mesh>(Source);
                var arms = Inspect(source);
                File.WriteAllText(Output + "/Audit.txt", "Arm roll/end-cap triangle counts: " + string.Join(", ", arms.Select(a => a.triangles.Length))
                    + ". Outward facing scores: " + string.Join(", ", arms.Select(a => a.facing.ToString("F4"))) + ".\n");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.3f, .3f, .3f);
                RenderSettings.ambientIntensity = 1; RenderSettings.reflectionIntensity = 0;
                var sofa = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Level + "/Prefabs/A88_Red_Sofa_ROOT.prefab"));
                sofa.transform.position = Vector3.zero; sofa.transform.rotation = Quaternion.Euler(0, -45, 0);
                var lamp = new GameObject("Diagnostic_Key").AddComponent<Light>();
                lamp.type = LightType.Directional; lamp.intensity = 1.3f; lamp.transform.rotation = Quaternion.Euler(35, -35, 0);
                lamp.shadows = LightShadows.None;
                Later(() =>
                {
                    var filter = sofa.GetComponentsInChildren<MeshFilter>().Single(f => f.sharedMesh == source);
                    var renderer = filter.GetComponent<MeshRenderer>();
                    var lit = new Material(renderer.sharedMaterial);
                    lit.SetFloat("_EnvironmentReflections", 0); lit.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    lit.SetFloat("_SpecularHighlights", 0); lit.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    renderer.sharedMaterial = lit;
                    void Shot(string name) => LoungeVisualStudy.CaptureTo(new Vector3(2.4f, 1.65f, -3.4f), new Vector3(0, .65f, 0), Output + "/" + name + ".png", 42);
                    if (arms.Any(a => a.facing < 0)) Shot("Before_NoReflection");
                    var corrected = Object.Instantiate(source); Repair(corrected); filter.sharedMesh = corrected;
                    Shot("After_NoReflection");
                    var unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    unlit.SetTexture("_BaseMap", lit.GetTexture("_BaseMap")); unlit.SetColor("_BaseColor", Color.white);
                    unlit.SetFloat("_Cull", 0); renderer.sharedMaterial = unlit; filter.sharedMesh = source;
                    Shot("BaseColor_Only");
                    Debug.Log("SOFA_ARM_PREVIEW_OK"); EditorApplication.Exit(0);
                });
            }
            catch (Exception e) { Fail(e); }
        }

        public static void ApplyBatch()
        {
            try
            {
                Directory.CreateDirectory(Output); Directory.CreateDirectory("Logs/SofaArmRepair");
                if (!File.Exists("Logs/SofaArmRepair/Before.unity")) File.Copy(HallLevelBuilder.ScenePath, "Logs/SofaArmRepair/Before.unity");
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                Later(() =>
                {
                    CaptureHall("Before");
                    foreach (string path in new[] { Source, Baked })
                    {
                        string backup = "Logs/SofaArmRepair/" + Path.GetFileName(path);
                        if (!File.Exists(backup)) File.Copy(path, backup);
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        int changed = Repair(mesh); EditorUtility.SetDirty(mesh);
                        File.AppendAllText(Output + "/Repair.txt", path + ": reversed " + changed + " faces; positions, UV0 and UV2 unchanged.\n");
                    }
                    AssetDatabase.SaveAssets();
                    Lightmapping.bakeCompleted += BakeComplete;
                    if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Bake could not start.");
                });
            }
            catch (Exception e) { Fail(e); }
        }

        static void BakeComplete()
        {
            Lightmapping.bakeCompleted -= BakeComplete;
            try
            {
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                Later(() =>
                {
                    foreach (string path in new[] { Source, Baked })
                        if (Inspect(AssetDatabase.LoadAssetAtPath<Mesh>(path)).Any(a => a.facing < .9f)) throw new InvalidOperationException("Saved repair failed validation.");
                    CaptureHall("After");
                    File.AppendAllText(Output + "/Repair.txt", "PASS: repaired meshes reloaded; lighting rebaked; both hall instances captured under unchanged lighting and materials.\n");
                    Debug.Log("SOFA_ARM_REPAIR_OK"); EditorApplication.Exit(0);
                });
            }
            catch (Exception e) { Fail(e); }
        }

        static void CaptureHall(string label)
        {
            LoungeVisualStudy.CaptureTo(new Vector3(7.6f, 1.6f, 8.9f), new Vector3(10.05f, .75f, 10.6f), Output + "/" + label + "_RightLounge.png", 47);
            LoungeVisualStudy.CaptureTo(new Vector3(5, 1.65f, -6), new Vector3(11.5f, .9f, -11.5f), Output + "/" + label + "_WaitingArea.png", 65);
        }

        static void Later(Action action)
        {
            frames = 0; pending = action; EditorApplication.update += Tick; EditorApplication.QueuePlayerLoopUpdate();
        }
        static void Tick()
        {
            if (++frames < 16) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            EditorApplication.update -= Tick;
            try { pending(); } catch (Exception e) { Fail(e); }
        }
        static void Fail(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
