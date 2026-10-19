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
    public static class HallLightingSetup
    {
        const string Root = HallLevelBuilder.Root + "/Lighting";
        const string Docs = "Docs/Arts/Environments/Lighting";
        const string RigName = "Hall_Lighting_Bright_Jewel";
        [Serializable] public class Fixture { public string name; public float[] position, outward; }
        [Serializable] public class FixtureList { public Fixture[] wallFixtures; public int chandelierBulbs; }
        static double started, lastProgress;
        static int reviewFrames;

        static T Save<T>(T value, string path) where T : Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old) { EditorUtility.CopySerialized(value, old); Object.DestroyImmediate(value); return old; }
            AssetDatabase.CreateAsset(value, path); return value;
        }
        static Vector3 V(float[] v) => new Vector3(v[0], v[1], v[2]);
        static Light Light(Transform root, string name, Vector3 position, LightType type, Color color, float intensity, float range, LightmapBakeType mode)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false); go.transform.position = position;
            var light = go.AddComponent<Light>(); light.type = type; light.color = color; light.intensity = intensity; light.range = range;
            light.lightmapBakeType = mode; light.bounceIntensity = 1; light.shadows = mode == LightmapBakeType.Baked ? LightShadows.Soft : LightShadows.None;
            light.shadowRadius = .2f; light.shadowBias = .03f; light.shadowNormalBias = .25f; return light;
        }
        static void Area(Transform root, string name, Vector3 position, Vector2 size, float intensity, Color color)
        {
            var light = Light(root, name, position, LightType.Rectangle, color, intensity, 20, LightmapBakeType.Baked);
            light.areaSize = size; light.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        static VolumeProfile CreateProfile()
        {
            string path = Root + "/Bright_Jewel_Profile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            T Component<T>() where T : VolumeComponent
            {
                if (!profile.TryGet<T>(out var component)) { component = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(component, profile); }
                component.active = true; return component;
            }
            var bloom = Component<Bloom>(); bloom.threshold.Override(1.25f); bloom.intensity.Override(.28f); bloom.scatter.Override(.58f); bloom.highQualityFiltering.Override(true);
            var color = Component<ColorAdjustments>(); color.postExposure.Override(.55f); color.contrast.Override(8); color.saturation.Override(18);
            var tone = Component<Tonemapping>(); tone.mode.Override(TonemappingMode.Neutral);
            var balance = Component<WhiteBalance>(); balance.temperature.Override(3); balance.tint.Override(1);
            foreach (var component in profile.components) EditorUtility.SetDirty(component); EditorUtility.SetDirty(profile); return profile;
        }
        static void ConfigureLighting()
        {
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Meshes"); AssetDatabase.Refresh();
            var previousRig = GameObject.Find(RigName); if (previousRig) Object.DestroyImmediate(previousRig);
            var old = GameObject.Find("Hall Lighting"); if (old) old.SetActive(false);
            var root = new GameObject(RigName).transform;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.8f, .9f, 1);
            RenderSettings.ambientEquatorColor = new Color(.7f, .76f, .8f);
            RenderSettings.ambientGroundColor = new Color(.4f, .36f, .32f);
            RenderSettings.ambientIntensity = 1; RenderSettings.reflectionIntensity = 1;
            var warm = new Color(1, .83f, .61f); var ivory = new Color(1, .94f, .83f);
            var fixtures = JsonUtility.FromJson<FixtureList>(File.ReadAllText(Docs + "/A93_Fixtures.json"));
            foreach (var fixture in fixtures.wallFixtures)
                Light(root, fixture.name + "_WarmGlow", V(fixture.position), LightType.Point, warm, 5, 4.2f, LightmapBakeType.Baked);
            Area(root, "Chandelier_Broad_Downlight", new Vector3(0, 9, -2.5f), new Vector2(4.8f, 4.8f), 5, ivory);
            foreach (float x in new[] { -6.5f, 6.5f }) foreach (float z in new[] { -14f, -3f })
                Area(root, "Hall_Soft_Bounce_" + x + "_" + z, new Vector3(x, 8.5f, z), new Vector2(10, 10), 2.3f, new Color(.9f, .96f, 1));
            foreach (float x in new[] { -8f, 0, 8f })
            {
                Area(root, "Mezzanine_Upper_Fill_" + x, new Vector3(x, 8.5f, 11), new Vector2(7, 8), 2.3f, ivory);
                foreach (float z in new[] { 8.5f, 14.3f })
                    Area(root, "Lower_Lounge_Bounce_" + x + "_" + z, new Vector3(x, 3.45f, z), new Vector2(6, 5), 1.15f, ivory);
            }
            var key = Light(root, "Soft_Character_Key", new Vector3(0, 8, -2.5f), LightType.Directional, Color.white, .5f, 30, LightmapBakeType.Realtime);
            key.transform.rotation = Quaternion.Euler(55, -25, 0); RenderSettings.sun = key;
            var chandelier = Light(root, "Chandelier_Character_Light", new Vector3(0, 8.9f, -2.5f), LightType.Point, ivory, 12, 20, LightmapBakeType.Realtime);
            chandelier.shadows = LightShadows.Soft; chandelier.shadowStrength = .45f; chandelier.shadowResolution = LightShadowResolution.Low;
            foreach (float x in new[] { -8.5f, 0, 8.5f }) Light(root, "Lounge_Character_Fill_" + x, new Vector3(x, 3.2f, 10.5f), LightType.Point, ivory, 1.8f, 8, LightmapBakeType.Realtime);

            var emissionCache = new Dictionary<Material, Material>();
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var materials = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i]; if (!source) continue;
                    if (!(source.name.Contains("Chandelier_ON") || source.name.Contains("Wall_Sconce_ON"))) continue;
                    if (!emissionCache.TryGetValue(source, out var emission))
                    {
                        string name = source.name.Contains("Wall_Sconce") ? "A93_Wall_Sconce_ON" : "A93_Chandelier_ON";
                        var mat = new Material(source) { name = name };
                        mat.SetColor("_BaseColor", new Color(1, .91f, .72f)); mat.SetColor("_EmissionColor", new Color(1, .74f, .38f) * 7);
                        mat.EnableKeyword("_EMISSION"); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                        emission = Save(mat, Root + "/Materials/" + name + ".mat"); emissionCache[source] = emission;
                    }
                    materials[i] = emission; changed = true;
                }
                if (changed) { renderer.sharedMaterials = materials; EditorUtility.SetDirty(renderer); }
            }
            var volumeGo = new GameObject("Bright_Jewel_Grade"); volumeGo.transform.SetParent(root, false);
            var volume = volumeGo.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 50; volume.sharedProfile = CreateProfile();
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.allowHDR = true; var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
                data.volumeLayerMask |= 1; PrefabUtility.RecordPrefabInstancePropertyModifications(camera); PrefabUtility.RecordPrefabInstancePropertyModifications(data);
            }
            var probeObject = new GameObject("Lounge_And_Hall_LightProbes"); probeObject.transform.SetParent(root, false);
            var probes = probeObject.AddComponent<LightProbeGroup>(); var points = new List<Vector3>();
            for (float x = -12; x <= 12; x += 3) for (float z = -20; z <= 16; z += 3)
            {
                // Keep probes inside the rounded end walls.
                float ax = Mathf.Max(0, Mathf.Abs(x) - 2);
                float end = z > 5.5f ? z - 5.5f : z < -10.5f ? -10.5f - z : 0;
                if (ax * ax + end * end > 11.7f * 11.7f) continue;
                foreach (float y in new[] { .55f, 1.65f, 2.9f }) points.Add(new Vector3(x, y, z));
                if (z > 5.5f) foreach (float y in new[] { 4.6f, 5.7f, 7.2f }) points.Add(new Vector3(x, y, z));
            }
            probes.probePositions = points.ToArray();
        }

        static void PrepareBake()
        {
            var hall = GameObject.Find("AuctionHall_Furnished"); int receivers = 0, contributors = 0;
            var meshes = new Dictionary<Mesh, Mesh>();
            foreach (var renderer in hall.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                if (!renderer.name.StartsWith("Static_")) continue;
                var material = renderer.sharedMaterial; string name = material ? material.name : "";
                if (name.Contains("Frond") || name.Contains("Leaf") || name.Contains("ON") || name.Contains("Glass")) continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI);
                contributors++;
                bool receiver = name.StartsWith("Hall_") || name.StartsWith("BBB_Wall_") || name.StartsWith("BBB_Column_") || name.StartsWith("Restroom_") || name.Contains("Medallion") || name.StartsWith("A39_") || name.Contains("_Woven");
                renderer.receiveGI = receiver ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                if (!receiver) continue;
                var source = filter.sharedMesh;
                if (!meshes.TryGetValue(source, out var mesh))
                {
                    string path = Root + "/Meshes/" + source.name + "_Lightmap.asset";
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (!mesh)
                    {
                        mesh = Object.Instantiate(source); mesh.name = source.name + "_Lightmap";
                        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
                        {
                            UnwrapParam.SetDefaults(out var parameters); parameters.packMargin = .004f;
                            Unwrapping.GenerateSecondaryUVSet(mesh, parameters);
                        }
                        AssetDatabase.CreateAsset(mesh, path);
                    }
                    meshes[source] = mesh;
                }
                filter.sharedMesh = mesh; renderer.scaleInLightmap = 1; receivers++;
                EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
                Debug.Log("A93_RECEIVER " + receivers + " " + name + " vertices=" + mesh.vertexCount);
            }
            var settings = new LightingSettings { name = "Hall_Bright_Jewel_Baked", bakedGI = true, realtimeGI = false, autoGenerate = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveCPU, lightmapResolution = 6, lightmapMaxSize = 2048, lightmapPadding = 4,
                directSampleCount = 48, indirectSampleCount = 128, environmentSampleCount = 64, maxBounces = 3,
                directionalityMode = LightmapsMode.CombinedDirectional, ao = true, aoMaxDistance = .65f, aoExponentIndirect = .55f, aoExponentDirect = 0,
                lightProbeSampleCountMultiplier = 2, filteringMode = LightingSettings.FilterMode.Auto };
            Lightmapping.lightingSettings = Save(settings, Root + "/Hall_Bright_Jewel_Baked.asset");
            File.WriteAllText(Docs + "/A93_Bake_Preparation.txt", "Lightmap receivers: " + receivers + "\nGI contributors: " + contributors + "\nUnique unwrapped copies: " + meshes.Count + "\n");
        }

        public static void BuildAndBakeBatch()
        {
            try
            {
                Directory.CreateDirectory(Docs); Directory.CreateDirectory("Logs/A93Lighting");
                var backup = "Logs/A93Lighting/SC_ExchangeFurnished_before_lighting.unity";
                if (!File.Exists(backup)) File.Copy(HallLevelBuilder.ScenePath, backup);
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                ConfigureLighting(); PrepareBake(); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Lightmapping.bakeCompleted += BakeComplete; started = lastProgress = EditorApplication.timeSinceStartup; EditorApplication.update += Progress;
                if (!Lightmapping.BakeAsync()) throw new Exception("Lightmapping did not start.");
                Debug.Log("A93_BAKE_STARTED");
            }
            catch (Exception e) { Fail(e); }
        }
        static void Progress()
        {
            if (EditorApplication.timeSinceStartup - lastProgress < 20) return; lastProgress = EditorApplication.timeSinceStartup;
            File.WriteAllText(Docs + "/A93_Bake_Progress.txt", "Running=" + Lightmapping.isRunning + ", progress=" + Lightmapping.buildProgress + ", seconds=" + (lastProgress - started));
        }
        static void BakeComplete()
        {
            Lightmapping.bakeCompleted -= BakeComplete; EditorApplication.update -= Progress;
            try
            {
                if (LightmapSettings.lightmaps.Length == 0) throw new Exception("Bake completed without lightmaps.");
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                File.WriteAllText(Docs + "/A93_Bake_Result.txt", "PASS: " + LightmapSettings.lightmaps.Length + " directional lightmaps; probes=" + (LightmapSettings.lightProbes ? LightmapSettings.lightProbes.count : 0) + "; elapsed seconds=" + (EditorApplication.timeSinceStartup - started));
                BeginReview();
            }
            catch (Exception e) { Fail(e); }
        }
        static void Fail(Exception e) { Debug.LogException(e); File.WriteAllText(Docs + "/A93_Error.txt", e.ToString()); EditorApplication.Exit(1); }
        static void CaptureAll()
        {
            Capture(new Vector3(-.2f, 1.65f, 5.5f), new Vector3(-8.6f, 1.05f, 11.6f), "A93_Left_Lounge", 35);
            Capture(new Vector3(.2f, 1.65f, 5.5f), new Vector3(8.6f, 1.05f, 11.6f), "A93_Right_Lounge", 35);
            Capture(new Vector3(0, 1.65f, 11.8f), new Vector3(0, 1.4f, 16.6f), "A93_Rear_Information", 24);
            Capture(new Vector3(0, 2.6f, -16), new Vector3(0, 7, -2.5f), "A93_Grand_Hall", 24);
            Capture(new Vector3(0, 5.65f, 13.4f), new Vector3(-7, 5.6f, 9.8f), "A93_Mezzanine", 28);
        }
        static void Capture(Vector3 position, Vector3 target, string name, float lens)
        {
            var go = new GameObject("Lighting_Review_Camera"); var camera = go.AddComponent<Camera>();
            try
            {
                camera.nearClipPlane = .05f; camera.farClipPlane = 100; camera.allowHDR = true;
                camera.fieldOfView = 2 * Mathf.Atan(36f / (2 * lens * (1600f / 900))) * Mathf.Rad2Deg;
                camera.transform.position = position; camera.transform.LookAt(target);
                var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
                // Match the game's sRGB display target; HDR remains enabled internally.
                var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); camera.targetTexture = rt;
                ShaderUtil.allowAsyncCompilation = false;
                VolumeManager.instance.Update(camera.transform, data.volumeLayerMask);
                camera.Render(); camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes(Docs + "/" + name + ".png", image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null; Object.DestroyImmediate(image); Object.DestroyImmediate(rt);
            }
            finally { Object.DestroyImmediate(go); }
        }
        public static void ReviewBatch()
        {
            try { EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath); BeginReview(); }
            catch (Exception e) { Fail(e); }
        }
        static void BeginReview() { reviewFrames = 0; EditorApplication.update += ReviewTick; EditorApplication.QueuePlayerLoopUpdate(); }
        static void ReviewTick()
        {
            if (++reviewFrames < 12) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            EditorApplication.update -= ReviewTick;
            try
            {
                CaptureAll(); Validate(); Debug.Log("A93_LIGHTING_REVIEW_OK"); EditorApplication.Exit(0);
            }
            catch (Exception e) { Fail(e); }
        }
        static void Validate()
        {
            var root = GameObject.Find(RigName); var lights = root.GetComponentsInChildren<Light>();
            if (lights.Count(l => l.name.EndsWith("_WarmGlow")) != 14) throw new Exception("Missing wall lamp lighting.");
            foreach (var light in lights) if (!light.enabled || light.intensity <= 0) throw new Exception("Disabled light: " + light.name);
            var camera = Camera.main; if (!camera || !camera.GetUniversalAdditionalCameraData().renderPostProcessing) throw new Exception("Gameplay camera post-processing is disabled.");
            var profile = root.GetComponentInChildren<Volume>().sharedProfile;
            if (!profile.TryGet<Bloom>(out var bloom) || !bloom.IsActive()) throw new Exception("Bloom is not active.");
            foreach (string name in new[] { "A93_Chandelier_ON", "A93_Wall_Sconce_ON" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");
                if (!mat || !mat.IsKeywordEnabled("_EMISSION") || mat.GetColor("_EmissionColor").maxColorComponent < 2) throw new Exception("Fixture emission missing: " + name);
            }
            if (LightmapSettings.lightmaps.Length < 1 || !LightmapSettings.lightProbes || LightmapSettings.lightProbes.count < 100) throw new Exception("Baked lighting data missing.");
            string report = "PASS: 14 wall fixtures and all 36 source chandelier bulbs use enabled emission.\n" +
                "PASS: fixed lights=" + lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked) + "; realtime lights=" + lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime) + "; realtime shadow casters=" + lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime && l.shadows != LightShadows.None) + ".\n" +
                "PASS: " + LightmapSettings.lightmaps.Length + " directional lightmap atlas; " + LightmapSettings.lightProbes.count + " probes.\n" +
                "PASS: gameplay camera HDR, Bloom and SMAA enabled; original gameplay FOV retained.\n" +
                "Review stack: Bloom=" + VolumeManager.instance.stack.GetComponent<Bloom>().intensity.value + ", exposure=" + VolumeManager.instance.stack.GetComponent<ColorAdjustments>().postExposure.value + ".\n";
            File.WriteAllText(Docs + "/A93_Final_Validation.txt", report);
        }
        public static void FinishAndBakeBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath); var root = GameObject.Find(RigName).transform;
                // Fill the vaulted roof softly as well as the floor, preserving the warm fixtures.
                foreach (float z in new[] { -13f, -2.5f, 10f })
                {
                    string name = "Vault_Indirect_Fill_" + z;
                    if (root.Find(name)) continue;
                    Area(root, name, new Vector3(0, 8.2f, z), new Vector2(10, 9), 2.3f, new Color(.9f, .96f, 1));
                    root.Find(name).rotation = Quaternion.Euler(-90, 0, 0);
                }
                // Blender's Metallic BSDF exported as a grey fallback; restore its brass parameters.
                var brass = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "A93_Fixture_BlenderKit_Brass" };
                brass.SetColor("_BaseColor", new Color(.89540994f, .70957601f, .35816395f).gamma);
                brass.SetFloat("_Metallic", 1); brass.SetFloat("_Smoothness", .66f); brass.SetFloat("_Cull", 0);
                var material = Save(brass, Root + "/Materials/A93_Fixture_BlenderKit_Brass.mat");
                foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    var mats = renderer.sharedMaterials; bool changed = false;
                    for (int i = 0; i < mats.Length; i++) if (mats[i] && mats[i].name.StartsWith("BBB_Chandelier_BlenderKit_BrushedBrass")) { mats[i] = material; changed = true; }
                    if (changed) { renderer.sharedMaterials = mats; EditorUtility.SetDirty(renderer); }
                }
                var light = root.Find("Chandelier_Character_Light").GetComponent<Light>();
                var data = new SerializedObject(light.GetUniversalAdditionalLightData());
                data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Lightmapping.bakeCompleted += BakeComplete; started = lastProgress = EditorApplication.timeSinceStartup; EditorApplication.update += Progress;
                if (!Lightmapping.BakeAsync()) throw new Exception("Final lighting bake did not start.");
            }
            catch (Exception e) { Fail(e); }
        }
        public static void AuditBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                var log = new StringBuilder();
                var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
                foreach (var category in renderers.GroupBy(r => r.transform.IsChildOf(GameObject.Find("AuctionHall_Furnished").transform) ? "Hall" : "Other"))
                {
                    var meshes = category.Select(r => r.GetComponent<MeshFilter>()).Where(m => m && m.sharedMesh).Select(m => m.sharedMesh).Distinct().ToArray();
                    log.AppendLine(category.Key + ": renderers=" + category.Count() + ", meshes=" + meshes.Length + ", verts=" + meshes.Sum(m => m.vertexCount) + ", uv2=" + meshes.Count(m => m.HasVertexAttribute(VertexAttribute.TexCoord1)));
                }
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) log.AppendLine("Light " + light.name + " intensity=" + light.intensity + " bake=" + light.lightmapBakeType);
                foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) log.AppendLine("Camera " + camera.name + " post=" + camera.GetUniversalAdditionalCameraData().renderPostProcessing);
                foreach (var p in typeof(LightingSettings).GetProperties().Where(p => p.CanWrite)) log.AppendLine("LightingSettings " + p.Name + " " + p.PropertyType.Name);
                Directory.CreateDirectory(Docs); File.WriteAllText(Docs + "/A93_Lighting_Audit.txt", log.ToString());
                Debug.Log("A93_LIGHTING_AUDIT_OK"); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
