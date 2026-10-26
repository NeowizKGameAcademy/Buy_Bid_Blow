using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Rendering.Editor
{
    // A separate lighting proposal: existing warm lobby palette, focused auction/trade lighting.
    public static class ContentLightingStudy
    {
        const string Source = "Assets/Project/Rendering/PlanarFloor/Study/SC_FreeFloorReflection.unity";
        const string Production = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string Root = "Assets/Project/Rendering/ContentLighting/Study";
        const string ScenePath = Root + "/SC_ContentLighting.unity";
        const string Output = "Docs/Arts/Environments/Lighting/ContentLighting";
        static readonly string[] Names = { "Auction", "Trading", "Hall", "Entrance" };
        static readonly Vector3[] Eyes = { new(0, 6.65f, 14), new(0, 2.1f, -9.2f), new(0, 1.65f, -5), new(7.7f, 1.65f, -3.1f) };
        static readonly Vector3[] Targets = { new(0, 5.75f, 8.1f), new(0, 1.6f, -16), new(0, 2.8f, 12), new(13.7f, 1.3f, -5.5f) };
        static readonly float[] Fovs = { 62, 78, 65, 65 };
        static Dictionary<string, string> protectedAssets;
        static StringBuilder report;
        static double started, lastProgress;

        public static void BuildBatch() => Build();

        [MenuItem("BuyBidBlow/Lighting/Build Content Lighting Study")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || Lightmapping.isRunning) { Debug.LogWarning("Stop play mode / existing bake before building the study."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                if (!File.Exists(Source)) throw new InvalidOperationException("Build the free floor reflection study first.");
                Directory.CreateDirectory(Root); Directory.CreateDirectory(Output); AssetDatabase.Refresh();
                protectedAssets = AssetDatabase.GetDependencies(Source, true).Concat(new[] { Production, Source })
                    .Where(p => File.Exists(p) && (p.EndsWith(".unity") || p.EndsWith(".mat") || p.Contains("/Lighting/") || p.Contains("/SC_ExchangeFurnished/")))
                    .Distinct().ToDictionary(p => p, Digest);
                report = new StringBuilder("Reference: Docs/Arts/Environments/조명예시.png\nWarm concept palette with auction/trade accents; existing geometry and gameplay code retained.\n");
                EditorSceneManager.OpenScene(Source);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath, true);
                EditorSceneManager.OpenScene(ScenePath);
                var rig = Object.FindFirstObjectByType<PlanarFloorReflection>();
                if (!rig) throw new InvalidOperationException("Missing floor reflection rig.");
                rig.updateInEditMode = false;
                // Show the real lot display in both comparison images, including its baseline.
                var envelope = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.name == "Auction Envelope");
                if (envelope) { envelope.gameObject.SetActive(true); report.AppendLine("Study only: auction envelope shown for lighting review, before and after."); }
                CaptureAll("Before", rig);
                Configure(rig);
                rig.enabled = false;
                var settings = Object.Instantiate(Lightmapping.lightingSettings);
                settings.name = "ContentLighting_Bake";
                Save(settings, Root + "/ContentLighting_Bake.asset");
                Lightmapping.lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(Root + "/ContentLighting_Bake.asset");
                // A new scene gets its own bake output; never rebake into the source data asset.
                Lightmapping.lightingDataAsset = null;
                LightmapSettings.lightmaps = Array.Empty<LightmapData>();
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                started = lastProgress = EditorApplication.timeSinceStartup;
                Lightmapping.bakeCompleted += Complete;
                EditorApplication.update += Progress;
                File.WriteAllText(Output + "/Status.txt", "Baking new lightmaps and light probes for the separate study scene.");
                if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Lighting bake did not start.");
            }
            catch (Exception e) { Fail(e); }
        }

        static void Configure(PlanarFloorReflection rig)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.52f, .48f, .42f);
            RenderSettings.ambientEquatorColor = new Color(.38f, .33f, .27f);
            RenderSettings.ambientGroundColor = new Color(.20f, .15f, .10f);
            ConfigureLampMaterials();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                string n = light.name;
                if (n.StartsWith("Hall_Soft_Bounce")) { light.intensity *= .68f; light.color = new Color(1, .83f, .61f); }
                else if (n.StartsWith("Vault_Indirect_Fill")) { light.intensity *= .7f; light.color = new Color(1, .80f, .55f); }
                else if (n.StartsWith("Mezzanine_Upper_Fill")) { light.intensity *= .48f; light.color = new Color(1, .85f, .65f); }
                else if (n.StartsWith("Lower_Lounge_Bounce")) { light.intensity *= .83f; light.color = new Color(1, .85f, .65f); }
                else if (n == "Soft_Character_Key") { light.intensity = .12f; light.color = new Color(1, .94f, .85f); }
                else if (n == "Chandelier_Character_Light") { light.intensity = 6.5f; light.color = new Color(1, .84f, .61f); }
                else if (n == "Chandelier_Broad_Downlight") { light.intensity *= .92f; light.color = new Color(1, .80f, .53f); }
                else if (n.StartsWith("Lounge_Character_Fill")) { light.intensity *= .65f; light.color = new Color(1, .88f, .72f); }
                else if (n.EndsWith("_WarmGlow")) { light.intensity = 3.8f; light.range = 3.2f; light.color = new Color(1, .78f, .48f); }
                else if (n == "Auction_Rostrum_Key")
                {
                    light.intensity = 48; light.spotAngle = 55; light.innerSpotAngle = 32;
                    light.color = new Color(1, .88f, .73f);
                    var auctioneer = GameObject.Find("Auctioneer Pig");
                    if (!auctioneer) throw new InvalidOperationException("Auctioneer target missing.");
                    var renderers = auctioneer.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    Vector3 face = bounds.center; face.y = bounds.max.y - .5f;
                    light.transform.LookAt(face);
                    report.AppendLine("Auctioneer key target: " + face);
                }
                else if (n.StartsWith("Trade_Counter_Key")) { light.intensity = 46; light.spotAngle = 44; light.innerSpotAngle = 25; light.color = new Color(1, .88f, .73f); }
                else if (n == "Auction_Rostrum_Softbox") light.intensity = 1.7f;
                else if (n == "Auction_Audience_Softbox") light.intensity = .9f;
                else if (n == "Trade_Counter_Softbox") light.intensity = .7f;
                else if (n.StartsWith("Right_Lounge_") || n.StartsWith("Rear_Library_") || n.StartsWith("Waiting_Area_")) light.intensity *= .6f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                report.AppendLine($"Light: {n}; {light.lightmapBakeType}; intensity={light.intensity:F2}; angle={light.spotAngle:F0}; color={light.color}");
            }
            var root = new GameObject("Content_Lighting_Accents").transform;
            var pedestal = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(c => c.name == "02_Lot_Pedestal_ROOT");
            Vector3 lot = pedestal.bounds.center; lot.y = pedestal.bounds.max.y + .18f;
            Spot(root, "Lot_Focused_Key", lot + new Vector3(-1.7f, 2.4f, 1.8f), lot, 24, 38);
            report.AppendLine("Lot target: " + lot);

            // Independent Volume asset, so the original grade is never edited.
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "ContentLighting_Grade";
            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(.35f); grade.contrast.Override(12); grade.saturation.Override(10);
            var white = profile.Add<WhiteBalance>(true); white.temperature.Override(5); white.tint.Override(1);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(1.5f); bloom.intensity.Override(.18f); bloom.scatter.Override(.55f);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
            string profilePath = Root + "/ContentLighting_Grade.asset";
            var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (old)
            {
                foreach (var component in old.components.ToArray()) Object.DestroyImmediate(component, true);
                old.components.Clear();
                foreach (var component in profile.components) { old.components.Add(component); AssetDatabase.AddObjectToAsset(component, old); }
                Object.DestroyImmediate(profile); profile = old; EditorUtility.SetDirty(profile);
            }
            else
            {
                AssetDatabase.CreateAsset(profile, profilePath);
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            }
            var volume = new GameObject("Content_Lighting_Grade").AddComponent<Volume>();
            volume.transform.SetParent(root, false); volume.isGlobal = true; volume.priority = 1000; volume.sharedProfile = profile;
            // The existing floor reflection is retained at the same strength for a fair lighting comparison.
            rig.sourceCamera.transform.position = Eyes[0]; rig.sourceCamera.transform.LookAt(Targets[0]); rig.sourceCamera.fieldOfView = Fovs[0];
        }

        static void ConfigureLampMaterials()
        {
            // Preserve the source materials. A lower HDR emission keeps the bulb's amber
            // color through tone mapping; separate fixture lights still illuminate the room.
            string materialRoot = Root + "/Materials";
            Directory.CreateDirectory(materialRoot); AssetDatabase.Refresh();
            var replacements = new Dictionary<Material, Material>();
            int slots = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    if (!original || (original.name != "A93_Chandelier_ON" && original.name != "A93_Wall_Sconce_ON")) continue;
                    if (!replacements.TryGetValue(original, out var warm))
                    {
                        warm = new Material(original) { name = original.name + "_WarmStudy" };
                        warm.SetColor("_BaseColor", new Color(1, .88f, .62f));
                        warm.SetColor("_EmissionColor", new Color(1, .65f, .29f) * 2.2f);
                        warm.EnableKeyword("_EMISSION");
                        warm.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                        string path = materialRoot + "/" + warm.name + ".mat";
                        Save(warm, path);
                        warm = AssetDatabase.LoadAssetAtPath<Material>(path);
                        replacements.Add(original, warm);
                        report.AppendLine($"Bulb material: {original.name}; emission {original.GetColor("_EmissionColor")} -> {warm.GetColor("_EmissionColor")}; isolated copy {path}");
                    }
                    materials[i] = warm; changed = true; slots++;
                }
                if (!changed) continue;
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if (replacements.Count != 2) throw new InvalidOperationException("Expected both chandelier and wall-sconce bulb materials.");
            report.AppendLine($"Warm bulb materials assigned to {slots} renderer material slots. Global grade and focus beam shapes retained.");
        }

        static void Spot(Transform root, string name, Vector3 from, Vector3 target, float intensity, float angle)
        {
            var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(root, false);
            light.transform.position = from; light.transform.LookAt(target);
            light.type = LightType.Spot; light.range = 6; light.intensity = intensity;
            light.color = new Color(1, .86f, .7f); light.spotAngle = angle; light.innerSpotAngle = angle * .55f;
            light.lightmapBakeType = LightmapBakeType.Mixed; light.shadows = LightShadows.Soft;
            light.shadowBias = .025f; light.shadowNormalBias = .1f; light.shadowNearPlane = .1f;
            var data = new SerializedObject(light.GetUniversalAdditionalLightData());
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Progress()
        {
            if (EditorApplication.timeSinceStartup - lastProgress < 10) return;
            lastProgress = EditorApplication.timeSinceStartup;
            File.WriteAllText(Output + "/Status.txt", $"Baking={Lightmapping.isRunning}; progress={Lightmapping.buildProgress:P0}; elapsed={lastProgress - started:F0}s");
        }

        static void Complete()
        {
            Lightmapping.bakeCompleted -= Complete; EditorApplication.update -= Progress;
            // Capture after Unity has finished assigning the new baked data to the scene.
            EditorApplication.delayCall += Finish;
        }
        static void Finish()
        {
            try
            {
                if (EditorSceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Active scene changed during bake.");
                if (LightmapSettings.lightmaps.Length == 0) throw new InvalidOperationException("Bake produced no lightmaps.");
                string dataPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                if (!dataPath.StartsWith(Root + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Bake output is not isolated: " + dataPath);
                var rig = Object.FindFirstObjectByType<PlanarFloorReflection>();
                // Refresh reflections against the new illumination in separate output files.
                int i = 0;
                foreach (var probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
                {
                    string path = Root + "/Reflection_" + i++ + ".exr";
                    if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new InvalidOperationException("Probe bake failed: " + probe.name);
                    AssetDatabase.ImportAsset(path);
                    probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                }
                rig.enabled = true; CaptureAll("After", rig);
                rig.updateInEditMode = true; rig.RenderFor(rig.sourceCamera);
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                foreach (var pair in protectedAssets)
                    if (Digest(pair.Key) != pair.Value) throw new InvalidOperationException("Source asset changed: " + pair.Key);
                report.AppendLine($"PASS: {LightmapSettings.lightmaps.Length} lightmaps; {i} reflection probes; independent lighting data: {dataPath}");
                report.AppendLine("PASS: four matched before/after camera views; production scene, original study, source lighting/material assets unchanged.");
                report.AppendLine("GPU performance and multiplayer gameplay have not been measured. This is a lighting proposal scene.");
                File.WriteAllText(Output + "/Validation.txt", report.ToString());
                File.WriteAllText(Output + "/Status.txt", "PASS: lighting baked, comparison images captured, source assets unchanged.");
                Debug.Log("CONTENT_LIGHTING_STUDY_OK");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Fail(e); }
        }

        static void CaptureAll(string prefix, PlanarFloorReflection rig)
        {
            for (int i = 0; i < Names.Length; i++) Capture(Eyes[i], Targets[i], Fovs[i], Output + "/" + prefix + "_" + Names[i] + ".png", rig);
        }
        static void Capture(Vector3 eye, Vector3 target, float fov, string path, PlanarFloorReflection rig)
        {
            var go = new GameObject("Content_Lighting_Capture"); var camera = go.AddComponent<Camera>();
            var rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                camera.enabled = false; camera.transform.position = eye; camera.transform.LookAt(target);
                camera.fieldOfView = fov; camera.nearClipPlane = .05f; camera.farClipPlane = 100; camera.allowHDR = true; camera.targetTexture = rt;
                var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true; data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                ShaderUtil.allowAsyncCompilation = false; VolumeManager.instance.Update(camera.transform, data.volumeLayerMask);
                camera.Render(); rig.RenderFor(camera); camera.Render(); camera.Render();
                RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
                Object.DestroyImmediate(rt); Object.DestroyImmediate(image); Object.DestroyImmediate(go);
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
            }
        }
        static string Digest(string path) { using var file = File.OpenRead(path); using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(file)); }
        static void Save<T>(T value, string path) where T : Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old) { EditorUtility.CopySerialized(value, old); Object.DestroyImmediate(value); EditorUtility.SetDirty(old); }
            else AssetDatabase.CreateAsset(value, path);
        }
        static void Fail(Exception e)
        {
            Lightmapping.bakeCompleted -= Complete; EditorApplication.update -= Progress;
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Status.txt", "ERROR: " + e);
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
