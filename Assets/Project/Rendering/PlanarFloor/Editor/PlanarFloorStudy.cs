using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Rendering.Editor
{
    public static class PlanarFloorStudy
    {
        const string Source = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string Root = "Assets/Project/Rendering/PlanarFloor/Study";
        const string ScenePath = Root + "/SC_FreeFloorReflection.unity";
        const string Output = "Docs/Arts/Environments/Lighting/FreeFloorReflection";
        static readonly Vector3[] Eyes = { new Vector3(7.7f, 1.65f, -3.1f), new Vector3(0, 1.65f, -5), new Vector3(-5.5f, 1.7f, 7.1f) };
        static readonly Vector3[] Targets = { new Vector3(13.7f, 1.3f, -5.5f), new Vector3(0, 2.8f, 12), new Vector3(-11.6f, 1.35f, 10.55f) };
        static readonly string[] Views = { "Entrance", "Hall", "Lounge" };

        [MenuItem("BuyBidBlow/Lighting/Open Free Floor Reflection Study")]
        public static void OpenStudy()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Debug.LogWarning("Run Build Free Floor Reflection Study first.");
        }

        [MenuItem("BuyBidBlow/Lighting/Build Free Floor Reflection Study")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run(false);
        }
        public static void BuildBatch() => Run(true);

        internal static void Run(bool batch)
        {
            try
            {
                Directory.CreateDirectory(Root); Directory.CreateDirectory(Output); AssetDatabase.Refresh();
                byte[] sourceBytes = File.ReadAllBytes(Source);
                EditorSceneManager.OpenScene(Source);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath, true);
                EditorSceneManager.OpenScene(ScenePath);
                // Disable only display UI in this study; keep geometry, lighting and interactions.
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    canvas.enabled = false;

                var report = new StringBuilder();
                var go = new GameObject("Free_Floor_Reflection_Study");
                var rig = go.AddComponent<PlanarFloorReflection>();
                var mesh = ExtractFloor(report);
                string meshPath = Root + "/FloorCoating.asset";
                SaveAsset(mesh, meshPath);
                var receiver = new GameObject("Floor_Coating"); receiver.transform.SetParent(go.transform, false);
                receiver.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                var renderer = receiver.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                rig.coatings = new Renderer[] { renderer };

                // Validate newly extracted surface before applying the reflective material.
                var grey = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                grey.SetColor("_BaseColor", new Color(.4f, .4f, .4f)); grey.SetFloat("_Cull", 2);
                renderer.sharedMaterial = grey;
                rig.enabled = false;
                Capture(Eyes[0], Targets[0], Output + "/Geometry_BackfaceCulling.png", null);
                Object.DestroyImmediate(grey);

                var material = new Material(Shader.Find("BuyBidBlow/Planar Floor Coating"));
                material.SetFloat("_Strength", .65f);
                if (!material.shader || ShaderUtil.ShaderHasError(material.shader)) throw new InvalidOperationException("Reflection shader failed to compile.");
                SaveAsset(material, Root + "/FloorCoating.mat");
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/FloorCoating.mat");
                // Original lighting and floor materials are intentionally identical for A/B.
                for (int i = 0; i < Views.Length; i++)
                {
                    rig.enabled = false;
                    Capture(Eyes[i], Targets[i], Output + "/Before_" + Views[i] + ".png", null);
                    rig.enabled = true;
                    Capture(Eyes[i], Targets[i], Output + "/After_" + Views[i] + ".png", rig);
                }
                report.AppendLine("PASS: before/after cameras, exposure, lights and base floor materials unchanged.");
                report.AppendLine("Reflection renders: " + rig.RenderCount + "; target=" + rig.ReflectionTexture.width + "x" + rig.ReflectionTexture.height);
                report.AppendLine("Last CPU render-request submission: " + rig.LastSubmissionMilliseconds.ToString("F2") + " ms (NOT isolated GPU cost or gameplay FPS).");
                if (rig.RenderCount < 3) throw new InvalidOperationException("Reflection camera did not render.");

                ValidateLifecycle(rig, report);
                // Keep a review camera so opening the saved study immediately shows the effect.
                // The production component can instead leave sourceCamera empty for Camera.main.
                foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    existingCamera.enabled = false;
                var previewObject = new GameObject("Floor_Reflection_Review_Camera");
                previewObject.transform.SetParent(go.transform, false);
                var preview = previewObject.AddComponent<Camera>();
                preview.transform.position = Eyes[1]; preview.transform.LookAt(Targets[1]);
                preview.fieldOfView = 65; preview.nearClipPlane = .05f; preview.farClipPlane = 100;
                preview.allowHDR = true;
                var previewData = preview.GetUniversalAdditionalCameraData();
                previewData.renderPostProcessing = true; previewData.renderShadows = true;
                previewData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                rig.sourceCamera = preview;
                rig.updateInEditMode = true;
                rig.RenderFor(preview);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                if (!sourceBytes.SequenceEqual(File.ReadAllBytes(Source))) throw new InvalidOperationException("Source scene was modified.");
                report.AppendLine("PASS: production scene file unchanged; no package or global pipeline changes.");
                File.WriteAllText(Output + "/Validation.txt", report.ToString());
                Debug.Log("FREE_FLOOR_REFLECTION_OK\n" + report);
                if (batch) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Error.txt", e.ToString());
                Debug.LogException(e); if (batch) EditorApplication.Exit(1);
            }
        }

        static Mesh ExtractFloor(StringBuilder report)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            int corrected = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) continue;
                Mesh original = filter.sharedMesh;
                var points = original.vertices;
                var materials = renderer.sharedMaterials;
                for (int s = 0; s < Mathf.Min(materials.Length, original.subMeshCount); s++)
                {
                    if (!materials[s] || !(materials[s].name.Contains("Hall_Dark_Herringbone") || materials[s].name.Contains("Hall_Dark_Walnut_Border"))) continue;
                    var triangles = original.GetTriangles(s);
                    for (int t = 0; t < triangles.Length; t += 3)
                    {
                        Vector3 a = renderer.transform.TransformPoint(points[triangles[t]]);
                        Vector3 b = renderer.transform.TransformPoint(points[triangles[t + 1]]);
                        Vector3 c = renderer.transform.TransformPoint(points[triangles[t + 2]]);
                        if (Mathf.Max(Mathf.Abs(a.y), Mathf.Abs(b.y), Mathf.Abs(c.y)) > .08f) continue;
                        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                        if (Mathf.Abs(normal.y) < .999f) continue;
                        if (normal.y < 0) { (b, c) = (c, b); corrected++; }
                        int start = vertices.Count;
                        vertices.Add(a + Vector3.up * .003f); vertices.Add(b + Vector3.up * .003f); vertices.Add(c + Vector3.up * .003f);
                        indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                    }
                }
            }
            if (indices.Count == 0) throw new InvalidOperationException("No ground floor triangles found.");
            var mesh = new Mesh { name = "FloorCoating_Upward", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (mesh.normals.Any(n => n.y < .999f)) throw new InvalidOperationException("Coating face normals are not upward.");
            report.AppendLine("PASS: " + indices.Count / 3 + " ground floor triangles; world-space identity transform; upward normals; backface culling.");
            report.AppendLine("Reversed source winding corrected on overlay only: " + corrected + ". Original geometry unchanged.");
            return mesh;
        }

        static void ValidateLifecycle(PlanarFloorReflection rig, StringBuilder report)
        {
            var go = new GameObject("Reflection_validation_camera"); var camera = go.AddComponent<Camera>();
            camera.enabled = false; camera.transform.position = Eyes[0]; camera.transform.LookAt(Targets[0]);
            camera.targetTexture = new RenderTexture(640, 400, 24);
            try
            {
                rig.RenderFor(camera);
                if (!rig.ReflectionTexture || rig.ReflectionTexture.width != 320) throw new InvalidOperationException("Reflection resize failed.");
                int count = rig.RenderCount;
                camera.transform.position = new Vector3(0, -1, 0); rig.RenderFor(camera);
                if (rig.RenderCount != count) throw new InvalidOperationException("Below-floor camera should not render.");
                rig.enabled = false;
                if (rig.ReflectionTexture) throw new InvalidOperationException("Disable leaked render texture.");
                rig.enabled = true; camera.transform.position = Eyes[0]; rig.RenderFor(camera);
                if (!rig.ReflectionTexture) throw new InvalidOperationException("Re-enable failed.");
                if (GL.invertCulling) throw new InvalidOperationException("Reflection leaked inverted culling state.");
                if (rig.coatings.Any(r => r.forceRenderingOff)) throw new InvalidOperationException("Reflection hid floor permanently.");
                report.AppendLine("PASS: resize, below-floor rejection, disable/re-enable, culling and receiver state restoration.");
            }
            finally
            {
                var target = camera.targetTexture; camera.targetTexture = null; target.Release();
                Object.DestroyImmediate(target); Object.DestroyImmediate(go);
            }
        }

        static void Capture(Vector3 eye, Vector3 target, string path, PlanarFloorReflection rig)
        {
            var go = new GameObject("Free_Reflection_Review_Camera"); var camera = go.AddComponent<Camera>();
            var rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
            try
            {
                camera.enabled = false; camera.transform.position = eye; camera.transform.LookAt(target);
                camera.fieldOfView = 65; camera.nearClipPlane = .05f; camera.farClipPlane = 100;
                camera.allowHDR = true; camera.targetTexture = rt;
                var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
                data.renderShadows = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                ShaderUtil.allowAsyncCompilation = false;
                VolumeManager.instance.Update(camera.transform, data.volumeLayerMask);
                camera.Render(); // Initialize the pipeline before sending standalone requests.
                if (rig) rig.RenderFor(camera);
                if (rig && rig.ReflectionTexture)
                {
                    RenderTexture.active = rig.ReflectionTexture;
                    var debug = new Texture2D(rig.ReflectionTexture.width, rig.ReflectionTexture.height, TextureFormat.RGB24, false);
                    debug.ReadPixels(new Rect(0, 0, debug.width, debug.height), 0, 0); debug.Apply();
                    File.WriteAllBytes(path.Replace(".png", "_Reflection.png"), debug.EncodeToPNG());
                    Object.DestroyImmediate(debug);
                }
                camera.Render(); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                if (rig && ShaderUtil.ShaderHasError(rig.coatings[0].sharedMaterial.shader)) throw new InvalidOperationException("Reflection shader compilation error.");
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image); Object.DestroyImmediate(go);
                ShaderUtil.allowAsyncCompilation = async;
            }
        }
        static void SaveAsset<T>(T value, string path) where T : Object
        {
            value.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) { EditorUtility.CopySerialized(value, existing); Object.DestroyImmediate(value); EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(value, path);
        }
    }
}
