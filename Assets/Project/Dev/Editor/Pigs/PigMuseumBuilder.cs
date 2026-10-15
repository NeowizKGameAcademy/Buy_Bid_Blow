using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    public static class PigMuseumBuilder
    {
        public const string Root = "Assets/Project/Dev/Sandbox/Pigs";
        public const string ScenePath = Root + "/SC_Pig_AnimationMuseum.unity";
        private static readonly string[] Files = {
            "Pig_Green_50k@Talking (1)", "Pig_Yellow_50k@Thankful",
            "Pig_Red_50k@Quick Formal Bow", "Pig_Black_50k@Quick Formal Bow",
            "Pig_Black_50k@Dwarf Idle", "Pig_Black_50k@Counting",
            "Pig_Yellow_50k@Standing Idle", "Pig_Green_50k@Standing Idle", "Pig_Red_50k@Standing Idle"
        };
        private static readonly string[] Labels = {
            "GREEN / TALKING", "YELLOW / THANKFUL", "RED / FORMAL BOW",
            "BLACK / FORMAL BOW", "BLACK / DWARF IDLE", "BLACK / COUNTING",
            "YELLOW / STANDING IDLE", "GREEN / STANDING IDLE", "RED / STANDING IDLE"
        };
        private static readonly Color[] Accents = {
            new Color(.15f,.58f,.35f), new Color(.95f,.68f,.17f), new Color(.76f,.19f,.2f),
            new Color(.24f,.28f,.34f), new Color(.24f,.28f,.34f), new Color(.24f,.28f,.34f),
            new Color(.95f,.68f,.17f), new Color(.15f,.58f,.35f), new Color(.76f,.19f,.2f)
        };

        [MenuItem("Tools/BuyBidBlow/Pigs/Open Animation Museum")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/BuyBidBlow/Pigs/Rebuild Animation Museum")]
        public static void Build()
        {
            BuildScene(ScenePath);
        }

        [MenuItem("Tools/BuyBidBlow/Pigs/Add Standing Idle Exhibits")]
        public static void AddStandingIdleExhibits()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
            var target = SceneManager.GetSceneByPath(ScenePath);
            if (!target.IsValid()) target = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var museum = target.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PigAnimationMuseum>(true)).Single();
            const string stagePath = Root + "/__StandingIdleStage.unity";
            if (File.Exists(stagePath)) throw new InvalidOperationException("Temporary staging scene already exists: " + stagePath);
            BuildScene(stagePath);
            var stage = EditorSceneManager.OpenScene(stagePath, OpenSceneMode.Additive);
            try
            {
                var source = stage.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PigAnimationMuseum>(true)).Single();
                var exhibits = museum.exhibits.ToList();
                var labels = museum.labels.ToList();
                var views = museum.viewpoints.ToList();
                for (int i = 6; i < Files.Length; i++)
                {
                    if (exhibits.Any(a => a && a.gameObject.name == Files[i])) continue;
                    SceneManager.MoveGameObjectToScene(source.exhibits[i].transform.root.gameObject, target);
                    SceneManager.MoveGameObjectToScene(source.viewpoints[i].gameObject, target);
                    exhibits.Add(source.exhibits[i]);
                    labels.Add(source.labels[i]);
                    views.Add(source.viewpoints[i]);
                }
                museum.exhibits = exhibits.ToArray();
                museum.labels = labels.ToArray();
                museum.viewpoints = views.ToArray();
                // Extend the gallery around the original six exhibits; retain their objects and references.
                foreach (string name in new[] { "Gallery floor", "Back wall", "Left wall", "Right wall", "PIG ANIMATION MUSEUM", "Overview" })
                {
                    var oldObject = target.GetRootGameObjects().FirstOrDefault(g => g.name == name);
                    var newObject = stage.GetRootGameObjects().FirstOrDefault(g => g.name == name);
                    if (!oldObject || !newObject) continue;
                    oldObject.transform.SetPositionAndRotation(newObject.transform.position, newObject.transform.rotation);
                    oldObject.transform.localScale = newObject.transform.localScale;
                }
                var subtitle = target.GetRootGameObjects().Select(g => g.GetComponent<TextMesh>())
                    .FirstOrDefault(t => t && t.text.Contains("MOTIONS /"));
                var newSubtitle = stage.GetRootGameObjects().Select(g => g.GetComponent<TextMesh>())
                    .First(t => t && t.text.Contains("MOTIONS /"));
                if (subtitle)
                {
                    subtitle.text = newSubtitle.text;
                    subtitle.name = newSubtitle.name;
                    subtitle.transform.position = newSubtitle.transform.position;
                }
                museum.museumCamera.transform.SetPositionAndRotation(museum.overview.position, museum.overview.rotation);
                if (museum.exhibits.Length != 9 || museum.viewpoints.Length != 9 || museum.labels.Length != 9)
                    throw new InvalidOperationException("Expected nine complete exhibits.");
                EditorSceneManager.MarkSceneDirty(target);
                if (!EditorSceneManager.SaveScene(target)) throw new IOException("Museum update save failed.");
                File.AppendAllText("Library/PigMuseumBuild.result", "PASS: three Standing Idle exhibits appended; original six retained; nine camera/controller entries.\n");
            }
            finally
            {
                EditorSceneManager.CloseScene(stage, true);
                AssetDatabase.DeleteAsset(stagePath);
            }
        }

        private static void BuildScene(string destination)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
            var loaded = SceneManager.GetSceneByPath(destination);
            if (loaded.IsValid()) throw new InvalidOperationException("Close the museum scene before rebuilding.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Controllers");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new StringBuilder();
            var models = new GameObject[Files.Length];
            var clips = new AnimationClip[Files.Length];
            var controllers = new AnimatorController[Files.Length];
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!lit) throw new InvalidOperationException("URP Lit shader not available.");

            for (int i = 0; i < Files.Length; i++)
            {
                string path = Root + "/Models/" + Files[i] + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Missing FBX: " + path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.isReadable = false;
                var settings = importer.defaultClipAnimations;
                foreach (var setting in settings)
                {
                    setting.loopTime = true;
                    setting.lockRootPositionXZ = true;
                    setting.lockRootHeightY = true;
                    setting.lockRootRotation = true;
                }
                importer.clipAnimations = settings;
                importer.SaveAndReimport();

                string textureFolder = Root + "/Materials/Textures_" + i;
                Directory.CreateDirectory(textureFolder);
                importer.ExtractTextures(textureFolder);
                NormalizeTextureNames(textureFolder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Texture2D baseMap = FindTexture(textureFolder, "basecolor");
                Texture2D normalMap = FindTexture(textureFolder, "normal");
                if (normalMap)
                {
                    var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normalMap)) as TextureImporter;
                    textureImporter.textureType = TextureImporterType.NormalMap;
                    textureImporter.SaveAndReimport();
                }
                // Remapped materials may no longer appear among the FBX's embedded subassets.
                foreach (var external in importer.GetExternalObjectMap().Values.OfType<Material>())
                {
                    external.shader = lit;
                    external.SetTexture("_BaseMap", baseMap);
                    external.SetColor("_BaseColor", baseMap ? Color.white : Accents[i]);
                    if (normalMap) { external.SetTexture("_BumpMap", normalMap); external.EnableKeyword("_NORMALMAP"); }
                    EditorUtility.SetDirty(external);
                }
                foreach (var original in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    string matPath = Root + "/Materials/Pig_" + i + "_" + SafeName(original.name) + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (!material) { material = new Material(lit); AssetDatabase.CreateAsset(material, matPath); }
                    material.shader = lit;
                    var texture = baseMap ? baseMap : original.mainTexture;
                    material.SetTexture("_BaseMap", texture);
                    material.SetColor("_BaseColor", texture ? Color.white : Accents[i]);
                    material.SetFloat("_Metallic", 0);
                    material.SetFloat("_Smoothness", .25f);
                    if (normalMap) { material.SetTexture("_BumpMap", normalMap); material.EnableKeyword("_NORMALMAP"); }
                    EditorUtility.SetDirty(material);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
                }
                importer.SaveAndReimport();
                models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                clips[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (!models[i] || !clips[i] || clips[i].length <= 0) throw new InvalidOperationException("Model or animation missing: " + path);
                string controllerPath = Root + "/Controllers/Pig_" + i + ".controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (!controller)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    var layers = controller.layers;
                    layers[0].name = "Exhibit";
                    controller.layers = layers;
                    var state = controller.layers[0].stateMachine.AddState("Loop");
                    state.motion = clips[i];
                    controller.layers[0].stateMachine.defaultState = state;
                }
                else controller.layers[0].stateMachine.defaultState.motion = clips[i];
                controllers[i] = controller;
                report.AppendLine($"{i+1}: {Files[i]} | clip={clips[i].name} length={clips[i].length:0.00}s | texture={(baseMap ? baseMap.name : "embedded/fallback")}");
            }
            var previous = SceneManager.GetActiveScene();
            bool replaceEmpty = string.IsNullOrEmpty(previous.path) && !previous.isDirty && previous.GetRootGameObjects().Length == 0;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.58f,.62f,.7f);
                RenderSettings.skybox = null;
                RenderSettings.fog = false;
                Material floor = CreateMaterial("Museum_Floor", new Color(.12f,.16f,.21f));
                Material wall = CreateMaterial("Museum_Wall", new Color(.22f,.28f,.34f));
                Material plinth = CreateMaterial("Museum_Plinth", new Color(.38f,.43f,.48f));
                float extraDepth = ((Files.Length + 2) / 3 - 2) * 5.5f;
                Cube("Gallery floor", new Vector3(0,-.15f,4 + extraDepth/2), new Vector3(19,.3f,19 + extraDepth), floor);
                Cube("Back wall", new Vector3(0,2.5f,12 + extraDepth), new Vector3(19,5,.3f), wall);
                Cube("Left wall", new Vector3(-9.5f,2.5f,4 + extraDepth/2), new Vector3(.3f,5,16 + extraDepth), wall);
                Cube("Right wall", new Vector3(9.5f,2.5f,4 + extraDepth/2), new Vector3(.3f,5,16 + extraDepth), wall);
                Text("PIG ANIMATION MUSEUM", new Vector3(-7.8f,4.1f,11.8f + extraDepth), .075f, Color.white);
                Text($"{Files.Length} MOTIONS / {Files.Length} EXHIBITS", new Vector3(-7.8f,3.45f,11.8f + extraDepth), .035f, new Color(.8f,.85f,.9f));
                var key = new GameObject("Gallery key light").AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.8f;
                key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(45,-30,0);
                var fill = new GameObject("Gallery fill light").AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = .6f;
                fill.color = new Color(.75f,.85f,1);
                fill.transform.rotation = Quaternion.Euler(35,140,0);
                var camera = new GameObject("Museum Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.gameObject.AddComponent<AudioListener>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f,.11f,.15f);
                camera.nearClipPlane = .05f;
                camera.farClipPlane = 80;
                camera.fieldOfView = 55;
                var cameraData = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (cameraData != null) camera.gameObject.AddComponent(cameraData);

                var museum = new GameObject("Museum Controls").AddComponent<PigAnimationMuseum>();
                museum.museumCamera = camera;
                museum.exhibits = new Animator[Files.Length];
                museum.labels = Labels;
                museum.viewpoints = new Transform[Files.Length];
                museum.overview = Viewpoint("Overview", new Vector3(13,9,-14), new Vector3(0,1.3f,4 + extraDepth/2));
                camera.transform.SetPositionAndRotation(museum.overview.position, museum.overview.rotation);
                for (int i = 0; i < Files.Length; i++)
                {
                    Vector3 position = new Vector3((i % 3 - 1) * 5, 0, 1 + i / 3 * 5.5f);
                    var station = new GameObject($"{i+1:00} {Labels[i]}");
                    station.transform.position = position;
                    Material accent = CreateMaterial("Station_" + i, Accents[i]);
                    var pedestal = Cube("Exhibit base", position + Vector3.up*.18f, new Vector3(3.5f,.36f,3), plinth);
                    pedestal.transform.SetParent(station.transform, true);
                    var trim = Cube("Identity strip", position + new Vector3(0,.2f,-1.51f), new Vector3(3.5f,.18f,.03f), accent);
                    trim.transform.SetParent(station.transform, true);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(models[i], scene);
                    model.name = Files[i];
                    model.transform.SetParent(station.transform, false);
                    model.transform.localRotation = Quaternion.Euler(0,180,0);
                    var renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) throw new InvalidOperationException("No model renderers: " + Files[i]);
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    if (bounds.size.y <= .01f) throw new InvalidOperationException("Invalid model bounds: " + Files[i]);
                    model.transform.localScale *= 2.25f / bounds.size.y;
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    model.transform.position += new Vector3(position.x - bounds.center.x, .37f - bounds.min.y, position.z - bounds.center.z);
                    var animator = model.GetComponent<Animator>();
                    if (!animator) animator = model.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controllers[i];
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    museum.exhibits[i] = animator;
                    museum.viewpoints[i] = Viewpoint("View " + (i+1), position + new Vector3(0,1.65f,-4.1f), position + new Vector3(0,1.5f,0));
                    var plaque = Text($"{i+1:00}   {Labels[i]}\n{clips[i].length:0.00}s   /   LOOP", position + new Vector3(-1.65f,.86f,-1.65f), .023f, Color.white);
                    plaque.transform.SetParent(station.transform, true);

                    // Sample imported clips on the actual rig in the temporary scene to verify motion.
                    clips[i].SampleAnimation(model, clips[i].length * .15f);
                    var bones = model.GetComponentsInChildren<Transform>(true);
                    var before = bones.Select(t => t.localRotation).ToArray();
                    var beforePositions = bones.Select(t => t.localPosition).ToArray();
                    clips[i].SampleAnimation(model, clips[i].length * .65f);
                    int moved = bones.Where((t,n) => Quaternion.Angle(t.localRotation,before[n]) > .01f || Vector3.Distance(t.localPosition,beforePositions[n]) > .0001f).Count();
                    if (moved == 0) throw new InvalidOperationException("Clip does not animate model: " + Files[i]);
                    clips[i].SampleAnimation(model, 0);
                    report.AppendLine($"   PASS: renderers={renderers.Length}, animated transforms={moved}, URP materials={renderers.All(r => r.sharedMaterials.All(m => m && m.shader && m.shader.name.StartsWith("Universal Render Pipeline/")))}");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, destination)) throw new IOException("Museum scene save failed.");
                AssetDatabase.SaveAssets();
                report.AppendLine($"PASS: {Files.Length} models, controllers and working clips; museum scene saved.");
                File.WriteAllText("Library/PigMuseumBuild.result", report.ToString());
                Debug.Log(report.ToString());
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (!replaceEmpty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string SafeName(string name) => string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
        private static void NormalizeTextureNames(string folder)
        {
            // Some FBX exporters store images as *_jpg / *_png without an extension.
            foreach (string file in Directory.GetFiles(folder))
            {
                if (Path.HasExtension(file)) continue;
                string extension = file.EndsWith("_jpg", StringComparison.OrdinalIgnoreCase) ? ".jpg" :
                    file.EndsWith("_png", StringComparison.OrdinalIgnoreCase) ? ".png" : null;
                if (extension == null) continue;
                string target = file + extension;
                if (!File.Exists(target))
                {
                    File.Move(file, target);
                    // A DefaultImporter meta from an extensionless file cannot import an image.
                    if (File.Exists(file + ".meta")) File.Delete(file + ".meta");
                }
                else
                {
                    File.Delete(file);
                    if (File.Exists(file + ".meta")) File.Delete(file + ".meta");
                }
            }
            foreach (string meta in Directory.GetFiles(folder, "*.meta"))
            {
                if ((meta.EndsWith(".jpg.meta") || meta.EndsWith(".png.meta")) && File.ReadAllText(meta).Contains("DefaultImporter:"))
                    File.Delete(meta);
            }
        }
        private static Texture2D FindTexture(string folder, string part)
        {
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta") || Path.GetFileName(file).IndexOf(part, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string path = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture) return texture;
            }
            return null;
        }
        private static Material CreateMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .15f);
            EditorUtility.SetDirty(material);
            return material;
        }
        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
        private static GameObject Text(string text, Vector3 position, float size, Color color)
        {
            var label = new GameObject(text.Split('\n')[0]);
            label.transform.position = position;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.GetComponent<Renderer>().sharedMaterial = mesh.font.material;
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.color = color;
            mesh.anchor = TextAnchor.UpperLeft;
            label.AddComponent<PigMuseumLabel>().textShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/MuseumText.shader");
            return label;
        }
        private static Transform Viewpoint(string name, Vector3 position, Vector3 target)
        {
            var point = new GameObject(name).transform;
            point.position = position;
            point.LookAt(target);
            return point;
        }
    }
}
