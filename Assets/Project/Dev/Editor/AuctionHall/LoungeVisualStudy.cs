using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // The left seating group is the visual test area. Shared source materials stay untouched.
    public static class LoungeVisualStudy
    {
        const string ScenePath = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string Root = "Assets/Project/Content/Levels/ExchangeFurnished/Lighting/LoungeStudy";
        const string Output = "Docs/Arts/Environments/Lighting/LoungeStudy";
        const string RigName = "Left_Lounge_Visual_Study";
        static int frames;
        static Action ready;
        static double bakeStarted;
        static double lastProgress;

        static readonly string[] FurnitureNames = {
            "A91_Left_Teal_Sofa", "A91_Left_Bench", "A91_Left_Rug", "A91_Left_Planter",
            "A90_Table_A", "A90_Bookcase_A", "A90_Shelf_A", "A90_ServiceTrolley_A", "A90_InformationBoard_A"
        };

        static T Save<T>(T value, string path) where T : Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!old) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, old); Object.DestroyImmediate(value); return old;
        }

        static MeshRenderer[] Furniture()
        {
            var root = GameObject.Find("UnderMezzanine_Lounge_Clusters");
            if (!root) throw new InvalidOperationException("Lounge furniture group missing.");
            return FurnitureNames.SelectMany(name => {
                var child = root.transform.Find(name);
                if (!child) throw new InvalidOperationException("Missing lounge object: " + name);
                return child.GetComponentsInChildren<MeshRenderer>(true);
            }).ToArray();
        }

        static void ConfigureMaterialsAndLightmaps()
        {
            var materials = new Dictionary<Material, Material>();
            int receivers = 0;
            foreach (var renderer in Furniture())
            {
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    var source = assigned[i];
                    if (!source) throw new InvalidOperationException("Missing material: " + renderer.name);
                    if (AssetDatabase.GetAssetPath(source).StartsWith(Root + "/")) continue;
                    if (!materials.TryGetValue(source, out var material))
                    {
                        material = new Material(source) { name = "Lounge_" + source.name };
                        string name = source.name;
                        if (name.Contains("TealVelvet"))
                        {
                            material.SetFloat("_Smoothness", .23f);
                            material.SetFloat("_BumpScale", .24f);
                            material.SetColor("_BaseColor", new Color(.86f, .94f, .93f, 1));
                        }
                        else if (name.Contains("BurgundyVelvet")) material.SetFloat("_Smoothness", .24f);
                        else if (name.Contains("Walnut")) material.SetFloat("_Smoothness", .38f);
                        else if (name.Contains("Brass"))
                        {
                            material.SetFloat("_Metallic", .9f); material.SetFloat("_Smoothness", .57f);
                        }
                        else if (name.Contains("Lounge_BK_Atlas"))
                        {
                            // Preserve the atlas' metal/wood masks; its alpha controls local roughness.
                            material.SetFloat("_Smoothness", .78f);
                            material.SetFloat("_BumpScale", .65f);
                        }
                        else if (name.Contains("Rug")) material.SetFloat("_Smoothness", .02f);
                        material = Save(material, Root + "/Materials/" + material.name + ".mat");
                        materials.Add(source, material);
                    }
                    assigned[i] = material;
                }
                renderer.sharedMaterials = assigned;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

                // Thin foliage remains probe-lit. LODGroups keep both levels in Unity's bake workflow.
                bool foliage = assigned.Any(m => m.name.Contains("Frond") || m.name.Contains("Leaf"));
                if (!foliage)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (!filter || !filter.sharedMesh) throw new InvalidOperationException("Missing mesh: " + renderer.name);
                    string meshPath = Root + "/Meshes/" + renderer.transform.parent.name + "_" + renderer.name + ".asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (!mesh)
                    {
                        mesh = Object.Instantiate(filter.sharedMesh); mesh.name = filter.sharedMesh.name + "_LoungeLightmap";
                        UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .015f;
                        Unwrapping.GenerateSecondaryUVSet(mesh, unwrap);
                        AssetDatabase.CreateAsset(mesh, meshPath);
                    }
                    filter.sharedMesh = mesh;
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                        GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                    renderer.receiveGI = ReceiveGI.Lightmaps;
                    renderer.scaleInLightmap = renderer.name.Contains("Rug") ? 5 : 3;
                    EditorUtility.SetDirty(filter); PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                    receivers++;
                }
                EditorUtility.SetDirty(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
            }
            int materialCount = Furniture().SelectMany(r => r.sharedMaterials).Distinct().Count();
            File.WriteAllText(Output + "/Changes.txt", "Left lounge only. Dedicated materials: " + materialCount + "; prepared lightmap receivers: " + receivers + ".\n");
        }

        static void ConfigureLighting()
        {
            var originalRig = GameObject.Find("Hall_Lighting_Bright_Jewel").transform;
            void Adjust(string name, float intensity, float range = -1)
            {
                var light = originalRig.Find(name).GetComponent<Light>(); light.intensity = intensity;
                if (range > 0) light.range = range;
                EditorUtility.SetDirty(light);
            }
            Adjust("Lower_Lounge_Bounce_-8_8.5", .32f);
            Adjust("Lower_Lounge_Bounce_-8_14.3", .42f);
            Adjust("Lounge_Character_Fill_-8.5", .4f, 5.5f);
            var previous = GameObject.Find(RigName); if (previous) Object.DestroyImmediate(previous);
            var rig = new GameObject(RigName).transform;

            var keyObject = new GameObject("Warm_Seating_Key"); keyObject.transform.SetParent(rig);
            keyObject.transform.position = new Vector3(-7.6f, 3.25f, 8.7f);
            keyObject.transform.LookAt(new Vector3(-11.1f, .85f, 11.5f));
            var key = keyObject.AddComponent<Light>(); key.type = LightType.Spot;
            key.lightmapBakeType = LightmapBakeType.Mixed;
            key.color = new Color(1, .84f, .66f); key.intensity = 18;
            key.range = 8.5f; key.spotAngle = 86; key.innerSpotAngle = 48;
            key.shadows = LightShadows.Soft; key.shadowStrength = .9f;
            key.shadowBias = .025f; key.shadowNormalBias = .12f; key.shadowNearPlane = .1f;
            key.shadowResolution = LightShadowResolution.High;
            key.shadowRadius = .16f;
            var keyData = new SerializedObject(key.GetUniversalAdditionalLightData());
            keyData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            keyData.ApplyModifiedPropertiesWithoutUndo();

            var bounceObject = new GameObject("Warm_Seating_Baked_Softbox"); bounceObject.transform.SetParent(rig);
            bounceObject.transform.position = new Vector3(-11.3f, 3.35f, 9.3f);
            bounceObject.transform.LookAt(new Vector3(-9.7f, .45f, 10.6f));
            var bounce = bounceObject.AddComponent<Light>(); bounce.type = LightType.Rectangle;
            bounce.areaSize = new Vector2(1.6f, .85f); bounce.lightmapBakeType = LightmapBakeType.Baked;
            bounce.color = new Color(1, .86f, .72f); bounce.intensity = 2.6f; bounce.range = 7;
            bounce.shadows = LightShadows.Soft;

            var probeObject = new GameObject("Left_Lounge_Interior_Reflection"); probeObject.transform.SetParent(rig);
            probeObject.transform.position = new Vector3(-9.4f, 1.65f, 10.4f);
            var probe = probeObject.AddComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(7, 3.25f, 8); probe.center = Vector3.zero;
            probe.boxProjection = true; probe.blendDistance = 1.2f; probe.importance = 100;
            probe.resolution = 256; probe.hdr = true; probe.intensity = .85f;
            probe.nearClipPlane = .1f; probe.farClipPlane = 45;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        }

        public static void ApplyBatch()
        {
            try
            {
                if (!File.Exists(Output + "/Before_Left.png")) throw new InvalidOperationException("Capture the baseline first.");
                EditorSceneManager.OpenScene(ScenePath);
                foreach (string dir in new[] { "Materials", "Meshes", "Reflections" }) Directory.CreateDirectory(Root + "/" + dir);
                AssetDatabase.Refresh();
                ConfigureMaterialsAndLightmaps(); ConfigureLighting();
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                bakeStarted = lastProgress = EditorApplication.timeSinceStartup;
                Lightmapping.bakeCompleted += BakeFinished; EditorApplication.update += BakeProgress;
                if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Lighting bake did not start.");
            }
            catch (Exception e) { Fail(e); }
        }

        static void BakeProgress()
        {
            if (EditorApplication.timeSinceStartup - lastProgress < 10) return;
            lastProgress = EditorApplication.timeSinceStartup;
            File.WriteAllText(Output + "/Progress.txt", "Baking=" + Lightmapping.isRunning + " progress=" + Lightmapping.buildProgress + " elapsed=" + (lastProgress - bakeStarted));
        }

        static void BakeFinished()
        {
            Lightmapping.bakeCompleted -= BakeFinished; EditorApplication.update -= BakeProgress;
            try
            {
                var probe = GameObject.Find(RigName).GetComponentInChildren<ReflectionProbe>();
                string path = Root + "/Reflections/Left_Lounge.exr";
                if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new InvalidOperationException("Reflection bake failed.");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                EditorUtility.SetDirty(probe);
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                File.AppendAllText(Output + "/Changes.txt", "Bake complete in " + (EditorApplication.timeSinceStartup - bakeStarted).ToString("F1") + " seconds.\n");
                EditorSceneManager.OpenScene(ScenePath);
                Later(() => { CaptureViews("After"); Validate(); Finish(); });
            }
            catch (Exception e) { Fail(e); }
        }

        static void Validate()
        {
            var renderers = Furniture();
            if (renderers.Any(r => r.sharedMaterials.Any(m => !m || !m.shader))) throw new InvalidOperationException("Unresolved material.");
            var primary = renderers.Where(r => !r.name.EndsWith("LOD1") && !r.name.Contains("Frond") && !r.name.Contains("Leaf")).ToArray();
            if (primary.Any(r => r.lightmapIndex < 0 || r.lightmapIndex >= LightmapSettings.lightmaps.Length))
                throw new InvalidOperationException("Missing baked lighting: " + string.Join(", ", primary.Where(r => r.lightmapIndex < 0).Select(r => r.name)));
            var probe = GameObject.Find(RigName).GetComponentInChildren<ReflectionProbe>();
            if (!probe.bakedTexture) throw new InvalidOperationException("Reflection texture was not saved.");
            var right = GameObject.Find("UnderMezzanine_Lounge_Clusters").transform.Find("A91_Right_Red_Sofa");
            bool hallPolishApplied = GameObject.Find("Hall_Visual_Polish");
            if (!hallPolishApplied && right.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterials.Any(m => AssetDatabase.GetAssetPath(m).StartsWith(Root))))
                throw new InvalidOperationException("Study materials leaked into the right lounge.");
            var log = new StringBuilder();
            log.AppendLine("PASS: scene reopened; " + primary.Length + " primary left-lounge renderers have saved lightmaps.");
            log.AppendLine("PASS: local 256px HDR box-projected reflection probe has a saved texture.");
            log.AppendLine(hallPolishApplied
                ? "INFO: approved materials have subsequently been extended across the hall; see HallPolish/Validation.txt."
                : "PASS: right sofa retains its original shared materials. Global ambient and grading settings untouched.");
            log.AppendLine("Comparison: 1600x1000, identical camera positions, field of view, HDR, SMAA, post-processing and exposure.");
            foreach (var r in primary) log.AppendLine(Hierarchy(r.transform) + " lightmap=" + r.lightmapIndex + " scale=" + r.scaleInLightmap);
            File.WriteAllText(Output + "/Validation.txt", log.ToString());
        }

        public static void ReviewBatch()
        {
            try { EditorSceneManager.OpenScene(ScenePath); Later(() => { CaptureViews("After"); Validate(); Finish(); }); }
            catch (Exception e) { Fail(e); }
        }

        static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;

        public static void AuditBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                Directory.CreateDirectory("Logs/LoungeVisualStudy");
                if (!File.Exists("Logs/LoungeVisualStudy/Before.unity"))
                    File.Copy(ScenePath, "Logs/LoungeVisualStudy/Before.unity");
                EditorSceneManager.OpenScene(ScenePath);
                Later(() =>
                {
                    CaptureViews("Before");
                    var log = new StringBuilder();
                    foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                        .Where(r => r.bounds.Intersects(new Bounds(new Vector3(-10, 1.6f, 10.5f), new Vector3(8, 3.4f, 9)))))
                    {
                        log.AppendLine(Hierarchy(r.transform) + " | center=" + r.bounds.center.ToString("F2") + " size=" + r.bounds.size.ToString("F2")
                            + " | GI=" + r.receiveGI + " map=" + r.lightmapIndex + " shadows=" + r.shadowCastingMode + "/" + r.receiveShadows);
                        foreach (var m in r.sharedMaterials.Where(m => m))
                            log.AppendLine("  " + AssetDatabase.GetAssetPath(m) + " | " + m.shader.name + " | smooth=" + (m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : -1)
                                + " metallic=" + (m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : -1));
                    }
                    foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.transform.position.x < -4))
                        log.AppendLine("LIGHT " + Hierarchy(l.transform) + " " + l.transform.position + " " + l.type + " " + l.intensity + " " + l.lightmapBakeType);
                    File.WriteAllText(Output + "/Audit.txt", log.ToString());
                    Finish();
                });
            }
            catch (Exception e) { Fail(e); }
        }

        static void CaptureViews(string label)
        {
            Capture(new Vector3(-5.5f, 1.7f, 7.1f), new Vector3(-11.6f, 1.35f, 10.55f), label + "_Left", 53);
            Capture(new Vector3(-7.0f, 1.65f, 8.6f), new Vector3(-10.05f, .75f, 10.8f), label + "_Detail", 55);
            Capture(new Vector3(5.5f, 1.7f, 7.1f), new Vector3(11.6f, 1.35f, 10.55f), label + "_Right_Control", 53);
        }

        static void Capture(Vector3 eye, Vector3 target, string name, float fov)
        {
            CaptureTo(eye, target, Output + "/" + name + ".png", fov);
        }

        internal static void CaptureTo(Vector3 eye, Vector3 target, string path, float fov)
        {
            const int width = 1600, height = 1000;
            var go = new GameObject("Lounge_Comparison_Camera");
            var camera = go.AddComponent<Camera>();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                camera.transform.position = eye; camera.transform.LookAt(target);
                camera.fieldOfView = fov; camera.nearClipPlane = .05f; camera.farClipPlane = 100;
                camera.allowHDR = true; camera.targetTexture = rt;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true; data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                ShaderUtil.allowAsyncCompilation = false;
                VolumeManager.instance.Update(camera.transform, data.volumeLayerMask);
                camera.Render(); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image); Object.DestroyImmediate(go);
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
            }
        }

        static void Later(Action action)
        {
            frames = 0; ready = action;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        static void Tick()
        {
            if (++frames < 16) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            EditorApplication.update -= Tick;
            try { ready(); } catch (Exception e) { Fail(e); }
        }
        static void Finish() { Debug.Log("LOUNGE_VISUAL_STUDY_OK"); EditorApplication.Exit(0); }
        static void Fail(Exception e)
        {
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Error.txt", e.ToString());
            Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
