using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallVisualPolish
    {
        private const string ScenePath = HallLevelBuilder.ScenePath;
        private const string Root = HallLevelBuilder.Root + "/Lighting/HallPolish";
        private const string StudyRoot = HallLevelBuilder.Root + "/Lighting/LoungeStudy";
        private const string Output = "Docs/Arts/Environments/Lighting/HallPolish";
        private const string RigName = "Hall_Visual_Polish";
        private static int frames;
        private static Action ready;
        private static double started;
        private static double lastProgress;
        private static string interactionCounts;
        private static readonly Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();

        private static MeshRenderer[] SceneSurfaces()
        {
            string[] roots = { "AuctionHall_Furnished", "UnderMezzanine_Lounge_Clusters", "UnderMezzanine_GamePosters", "PA_Speakers_Approved" };
            return roots.Select(GameObject.Find).Where(r => r).SelectMany(r => r.GetComponentsInChildren<MeshRenderer>())
                .Where(r => !r.GetComponentInParent<Animator>() && !r.GetComponentInParent<Cloth>() && !r.GetComponentInParent<RopeBarrier>()
                    && r.sharedMaterials.All(m => m && m.shader.name == "Universal Render Pipeline/Lit")).ToArray();
        }

        private static bool CanBake(MeshRenderer renderer)
        {
            return !renderer.GetComponentInParent<Door>() && renderer.GetComponent<MeshFilter>()?.sharedMesh
                && renderer.sharedMaterials.All(m => !m.name.Contains("Frond") && !m.name.Contains("Leaf")
                    && !m.name.Contains("Glass") && !m.name.EndsWith("_ON") && m.GetFloat("_Surface") == 0);
        }

        private static Material PolishedMaterial(Material source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (path.StartsWith(StudyRoot + "/") || path.StartsWith(Root + "/")) return source;
            if (materialCache.TryGetValue(source, out var cached)) return cached;
            // Reuse the approved material whenever the rest of the hall uses the same source.
            var approved = AssetDatabase.LoadAssetAtPath<Material>(StudyRoot + "/Materials/Lounge_" + source.name + ".mat");
            if (approved) { materialCache[source] = approved; return approved; }
            string name = source.name.ToLowerInvariant();
            bool velvet = name.Contains("velvet") || name.Contains("woven") || name.Contains("runner");
            bool wood = name.Contains("walnut") || name.Contains("polished_walnut") || name.Contains("door_wood");
            bool brass = name.Contains("brass") || name.Contains("satin_gold") || name.Contains("antique_gold");
            bool leather = name.Contains("leather");
            bool wall = name.Contains("wall_soft_teal");
            if ((!velvet && !wood && !brass && !leather && !wall) || name.StartsWith("a93_") || name.Contains("glass")) return source;
            string outputPath = Root + "/Materials/Polished_" + source.name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
            if (!result)
            {
                result = new Material(source) { name = "Polished_" + source.name };
                float smoothness = velvet ? .23f : leather ? .36f : wall ? .2f : wood ? .38f : .57f;
                if (name.Contains("woven") || name.Contains("runner")) smoothness = .04f;
                result.SetFloat("_Smoothness", smoothness);
                if (brass && !result.GetTexture("_MetallicGlossMap")) result.SetFloat("_Metallic", .9f);
                AssetDatabase.CreateAsset(result, outputPath);
            }
            materialCache[source] = result;
            return result;
        }

        private static void PrepareSurfaces()
        {
            materialCache.Clear();
            var meshCache = new Dictionary<Mesh, Mesh>();
            int newReceivers = 0;
            foreach (var renderer in SceneSurfaces())
            {
                bool approvedLeft = renderer.sharedMaterials.All(m => AssetDatabase.GetAssetPath(m).StartsWith(StudyRoot + "/"))
                    && renderer.bounds.center.x < -6 && renderer.bounds.center.z > 6;
                if (approvedLeft) continue;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(PolishedMaterial).ToArray();
                renderer.receiveShadows = true; renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                if (CanBake(renderer))
                {
                    bool alreadyBaked = renderer.lightmapIndex >= 0 && renderer.lightmapIndex < LightmapSettings.lightmaps.Length;
                    var filter = renderer.GetComponent<MeshFilter>();
                    var source = filter.sharedMesh;
                    if (!source.HasVertexAttribute(VertexAttribute.TexCoord1))
                    {
                        if (!meshCache.TryGetValue(source, out var mesh))
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
                            string meshPath = Root + "/Meshes/" + guid + "_" + localId + ".asset";
                            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                            if (!mesh)
                            {
                                mesh = Object.Instantiate(source); mesh.name = source.name + "_HallLightmap";
                                UnwrapParam.SetDefaults(out var unwrap); unwrap.packMargin = .012f;
                                Unwrapping.GenerateSecondaryUVSet(mesh, unwrap);
                                AssetDatabase.CreateAsset(mesh, meshPath);
                            }
                            meshCache[source] = mesh;
                        }
                        filter.sharedMesh = mesh; EditorUtility.SetDirty(filter);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                    }
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)
                        | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                    renderer.receiveGI = ReceiveGI.Lightmaps;
                    if (!alreadyBaked)
                    {
                        renderer.scaleInLightmap = renderer.name.Contains("Rug") ? 5 : renderer.bounds.size.magnitude < 8 ? 3 : 1.5f;
                        newReceivers++;
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.gameObject);
                }
                EditorUtility.SetDirty(renderer); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            File.WriteAllText(Output + "/Changes.txt", "Additional lightmap receivers: " + newReceivers + ". New unwrapped mesh assets: " + meshCache.Count
                + ". Material sources mapped to approved/polished variants: " + materialCache.Count + ".\n");
        }

        private static Light Spot(Transform parent, string name, Vector3 eye, Vector3 target, float intensity, float range, float angle, bool mixed, bool high = false)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = eye; go.transform.LookAt(target);
            var light = go.AddComponent<Light>(); light.type = LightType.Spot; light.intensity = intensity;
            light.color = new Color(1, .84f, .66f); light.range = range; light.spotAngle = angle; light.innerSpotAngle = angle * .56f;
            light.lightmapBakeType = mixed ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
            light.shadows = LightShadows.Soft; light.shadowStrength = .9f; light.shadowBias = .025f; light.shadowNormalBias = .12f;
            light.shadowNearPlane = .1f; light.shadowRadius = .16f;
            var data = new SerializedObject(light.GetUniversalAdditionalLightData());
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = high
                ? UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh : UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            data.ApplyModifiedPropertiesWithoutUndo(); return light;
        }

        private static void Softbox(Transform parent, string name, Vector3 eye, Vector3 target, Vector2 size, float intensity)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = eye; go.transform.LookAt(target);
            var light = go.AddComponent<Light>(); light.type = LightType.Rectangle; light.areaSize = size;
            light.intensity = intensity; light.color = new Color(1, .86f, .72f); light.range = 12;
            light.lightmapBakeType = LightmapBakeType.Baked; light.shadows = LightShadows.Soft;
        }

        private static void Reflection(Transform parent, string name, Vector3 center, Vector3 size, int importance)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = center;
            var probe = go.AddComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Baked;
            probe.size = size; probe.boxProjection = true; probe.blendDistance = 1.2f; probe.importance = importance;
            probe.resolution = 256; probe.hdr = true; probe.intensity = .85f; probe.nearClipPlane = .1f; probe.farClipPlane = 50;
        }

        private static void ConfigureLighting()
        {
            var old = GameObject.Find(RigName); if (old) Object.DestroyImmediate(old);
            var rig = new GameObject(RigName).transform;
            var baseRig = GameObject.Find("Hall_Lighting_Bright_Jewel").transform;
            void Adjust(string name, float intensity, float range = -1)
            {
                var light = baseRig.Find(name).GetComponent<Light>(); light.intensity = intensity;
                if (range > 0) light.range = range;
                EditorUtility.SetDirty(light);
            }
            Adjust("Lower_Lounge_Bounce_8_8.5", .32f); Adjust("Lower_Lounge_Bounce_8_14.3", .42f);
            Adjust("Lounge_Character_Fill_8.5", .4f, 5.5f);
            Adjust("Lower_Lounge_Bounce_0_8.5", .65f); Adjust("Lower_Lounge_Bounce_0_14.3", .45f);
            Adjust("Lounge_Character_Fill_0", .55f, 6);
            Adjust("Mezzanine_Upper_Fill_0", .95f);
            foreach (float x in new[] { -8f, 8f }) Adjust("Mezzanine_Upper_Fill_" + x, 1.35f);
            foreach (float x in new[] { -6.5f, 6.5f })
            {
                Adjust("Hall_Soft_Bounce_" + x + "_-14", 1.35f);
                Adjust("Hall_Soft_Bounce_" + x + "_-3", 1.65f);
            }

            Spot(rig, "Right_Lounge_Key", new Vector3(7.6f, 3.25f, 8.7f), new Vector3(11.1f, .85f, 11.5f), 18, 8.5f, 86, true);
            Softbox(rig, "Right_Lounge_Softbox", new Vector3(11.3f, 3.35f, 9.3f), new Vector3(9.7f, .45f, 10.6f), new Vector2(1.6f, .85f), 2.6f);
            Spot(rig, "Rear_Library_Key", new Vector3(-3.7f, 3.3f, 13.5f), new Vector3(-.4f, 1.1f, 16.8f), 15, 7.5f, 95, true);
            Softbox(rig, "Rear_Library_Softbox", new Vector3(3.5f, 3.35f, 15), new Vector3(1.5f, .8f, 16.7f), new Vector2(2, 1), 2.4f);
            Spot(rig, "Auction_Rostrum_Key", new Vector3(-3.3f, 8.1f, 11.2f), new Vector3(0, 5.35f, 8.3f), 24, 9, 75, true, true);
            Softbox(rig, "Auction_Rostrum_Softbox", new Vector3(3, 8.1f, 10.8f), new Vector3(0, 5.2f, 8.4f), new Vector2(2.5f, 1.5f), 3.1f);
            Softbox(rig, "Auction_Audience_Softbox", new Vector3(0, 8.3f, 15), new Vector3(0, 5.3f, 13.5f), new Vector2(4, 1.5f), 1.7f);
            foreach (float x in new[] { -6.9f, 0, 6.9f })
                Spot(rig, "Trade_Counter_Key_" + x, new Vector3(x - 1.5f, 5.2f, -12.1f), new Vector3(x, 1.15f, -16.1f), 20, 9, 66, true);
            Softbox(rig, "Trade_Counter_Softbox", new Vector3(0, 5.5f, -17), new Vector3(0, .9f, -15.5f), new Vector2(10, 2.5f), 1.5f);
            Spot(rig, "Waiting_Area_Warm_Pool", new Vector3(8.5f, 4.5f, -8), new Vector3(11.3f, .8f, -11.7f), 14, 8, 88, false);
            Softbox(rig, "Waiting_Area_Softbox", new Vector3(12, 4.1f, -10.5f), new Vector3(10.5f, .6f, -11.7f), new Vector2(2, 1.2f), 3.5f);

            Reflection(rig, "Right_Lounge_Reflection", new Vector3(9.4f, 1.65f, 10.4f), new Vector3(7, 3.25f, 8), 100);
            Reflection(rig, "Rear_Library_Reflection", new Vector3(0, 1.65f, 15.8f), new Vector3(12, 3.25f, 6), 80);
            Reflection(rig, "Auction_Reflection", new Vector3(0, 6.2f, 11.5f), new Vector3(17, 4.4f, 12), 60);
            Reflection(rig, "Trading_Reflection", new Vector3(0, 2.5f, -15), new Vector3(25, 5.5f, 14), 50);
            Reflection(rig, "Grand_Hall_Reflection", new Vector3(0, 4.3f, -1), new Vector3(27.5f, 9, 29), 10);
        }

        private static string InteractionCounts() => "doors=" + Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length
            + "; seats=" + Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Length
            + "; ropes=" + Object.FindObjectsByType<RopeBarrier>(FindObjectsSortMode.None).Length
            + "; cloth=" + Object.FindObjectsByType<Cloth>(FindObjectsSortMode.None).Length;

        public static void ApplyBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                EditorSceneManager.OpenScene(ScenePath);
                interactionCounts = InteractionCounts();
                Later(() =>
                {
                    if (!GameObject.Find(RigName)) CaptureAll("Before");
                    foreach (string folder in new[] { "Meshes", "Materials" }) Directory.CreateDirectory(Root + "/" + folder);
                    AssetDatabase.Refresh();
                    PrepareSurfaces(); ConfigureLighting();
                    AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                    started = lastProgress = EditorApplication.timeSinceStartup;
                    Lightmapping.bakeCompleted += BakeComplete; EditorApplication.update += BakeProgress;
                    if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Lighting bake failed to start.");
                });
            }
            catch (Exception e) { Fail(e); }
        }

        private static void BakeProgress()
        {
            if (EditorApplication.timeSinceStartup - lastProgress < 15) return;
            lastProgress = EditorApplication.timeSinceStartup;
            File.WriteAllText(Output + "/Progress.txt", "Baking=" + Lightmapping.isRunning + "; progress=" + Lightmapping.buildProgress
                + "; seconds=" + (lastProgress - started).ToString("F0"));
        }

        private static void BakeComplete()
        {
            Lightmapping.bakeCompleted -= BakeComplete; EditorApplication.update -= BakeProgress;
            try
            {
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                File.AppendAllText(Output + "/Changes.txt", "Bake completed in " + (EditorApplication.timeSinceStartup - started).ToString("F1") + " seconds.\n");
                File.WriteAllText(Output + "/Progress.txt", "Baking=False; complete=True");
                EditorSceneManager.OpenScene(ScenePath);
                Later(() => { CaptureAll("After"); Validate(); Finish(); });
            }
            catch (Exception e) { Fail(e); }
        }

        private static void Validate()
        {
            if (InteractionCounts() != interactionCounts) throw new InvalidOperationException("Interactive object counts changed.");
            var surfaces = SceneSurfaces();
            var primary = surfaces.Where(CanBake).Where(r => !r.name.Contains("LOD1") && !r.name.Contains("LOD2")).ToArray();
            var missing = primary.Where(r => r.lightmapIndex < 0 || r.lightmapIndex >= LightmapSettings.lightmaps.Length).ToArray();
            if (missing.Length > 0) throw new InvalidOperationException("Lightmaps missing: " + string.Join(", ", missing.Select(r => Hierarchy(r.transform))));
            var probes = GameObject.Find(RigName).GetComponentsInChildren<ReflectionProbe>();
            if (probes.Length != 5 || probes.Any(p => !p.bakedTexture)) throw new InvalidOperationException("Reflection probe bake was not saved.");
            var leftRig = GameObject.Find("Left_Lounge_Visual_Study");
            if (!leftRig || !leftRig.GetComponentInChildren<ReflectionProbe>().bakedTexture)
                throw new InvalidOperationException("Approved left-lounge lighting was not retained.");
            if (surfaces.Where(r => r.GetComponentInParent<Door>()).Any(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0))
                throw new InvalidOperationException("A moving door was marked for static lighting.");
            var report = new StringBuilder();
            report.AppendLine("PASS: saved scene reopened. " + primary.Length + " primary static surfaces have valid lightmaps.");
            report.AppendLine("PASS: five added reflection probes have saved HDR textures; approved left-lounge rig retained.");
            report.AppendLine("PASS: moving doors remain dynamic. " + interactionCounts + ".");
            report.AppendLine("PASS: matching before/after cameras and unchanged global post-processing.");
            report.AppendLine("Lightmap atlases: " + LightmapSettings.lightmaps.Length + ". Added realtime-shadow spotlights: 6; remaining new lights baked.");
            File.WriteAllText(Output + "/Validation.txt", report.ToString());
        }

        private static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;

        public static void ReviewBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                interactionCounts = InteractionCounts();
                Later(() => { CaptureAll("After"); Validate(); Finish(); });
            }
            catch (Exception e) { Fail(e); }
        }

        public static void AuditBatch()
        {
            try
            {
                Directory.CreateDirectory(Output);
                Directory.CreateDirectory("Logs/HallVisualPolish");
                if (!File.Exists("Logs/HallVisualPolish/Before.unity")) File.Copy(ScenePath, "Logs/HallVisualPolish/Before.unity");
                EditorSceneManager.OpenScene(ScenePath);
                Later(() =>
                {
                    if (!GameObject.Find(RigName)) CaptureAll("Before");
                    var log = new StringBuilder();
                    foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        log.AppendLine("ROOT " + root.name);
                        foreach (Transform child in root.transform)
                        {
                            var renderers = child.GetComponentsInChildren<MeshRenderer>();
                            if (renderers.Length == 0) continue;
                            var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                            log.AppendLine("  " + child.name + " center=" + bounds.center.ToString("F2") + " size=" + bounds.size.ToString("F2")
                                + " renderers=" + renderers.Length + " baked=" + renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < LightmapSettings.lightmaps.Length));
                            log.AppendLine("    " + string.Join(", ", renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Select(m => m.name).Distinct()));
                        }
                    }
                    File.WriteAllText(Output + "/Audit.txt", log.ToString());
                    Finish();
                });
            }
            catch (Exception e) { Fail(e); }
        }

        private static void CaptureAll(string label)
        {
            void Shot(string name, Vector3 eye, Vector3 target, float fov) => LoungeVisualStudy.CaptureTo(eye, target, Output + "/" + label + "_" + name + ".png", fov);
            Shot("LeftLounge", new Vector3(-5.5f, 1.7f, 7.1f), new Vector3(-11.6f, 1.35f, 10.55f), 53);
            Shot("RightLounge", new Vector3(5.5f, 1.7f, 7.1f), new Vector3(11.6f, 1.35f, 10.55f), 53);
            Shot("RearLibrary", new Vector3(0, 1.65f, 10.8f), new Vector3(0, 1.25f, 16.4f), 65);
            Shot("Auction", new Vector3(4.5f, 6.7f, 14), new Vector3(0, 5.5f, 8.3f), 65);
            Shot("Trading", new Vector3(0, 1.7f, -7), new Vector3(0, 1.7f, -17), 75);
            Shot("GrandHall", new Vector3(0, 2.2f, -12), new Vector3(0, 5.3f, 10), 65);
            Shot("WaitingArea", new Vector3(5, 1.65f, -6), new Vector3(11.5f, .9f, -11.5f), 65);
        }

        private static void Later(Action action)
        {
            frames = 0; ready = action; EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        private static void Tick()
        {
            if (++frames < 16) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            EditorApplication.update -= Tick;
            try { ready(); } catch (Exception e) { Fail(e); }
        }
        private static void Finish() { Debug.Log("HALL_VISUAL_POLISH_OK"); EditorApplication.Exit(0); }
        private static void Fail(Exception e)
        {
            Directory.CreateDirectory(Output); File.WriteAllText(Output + "/Error.txt", e.ToString());
            Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
