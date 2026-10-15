using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Dev.Editor
{
    public static class GrandEntranceBuilder
    {
        private const string Root = "Assets/Project/Content/Props/GrandEntrance";
        private const string Preview = "Assets/Project/Dev/Sandbox/GrandEntrance";
        private static readonly string[] Names = { "Wood", "Teal", "Brass" };

        [MenuItem("Tools/Buy Bid Blow/Build Grand Entrance Prefab")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Preview);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in Directory.GetFiles(Root + "/Textures", "*.png"))
            {
                var path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal");
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit not available");
            var materials = Names.Select((name, i) =>
            {
                var path = Root + "/Materials/GrandEntrance_" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;
                mat.SetColor("_BaseColor", Color.white);
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/BBB_Door_" + name + "_BaseColor.png"));
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/BBB_Door_" + name + "_Normal.png"));
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_Metallic", i == 2 ? 1f : 0f);
                mat.SetFloat("_Smoothness", new[] { .52f, .51f, .62f }[i]);
                EditorUtility.SetDirty(mat);
                return mat;
            }).ToArray();
            string modelPath = Root + "/Models/GrandEntrance.fbx";
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.importAnimation = false;
            modelImporter.isReadable = false;
            modelImporter.globalScale = 1f;
            modelImporter.bakeAxisConversion = true;
            modelImporter.generateSecondaryUV = true;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.SaveAndReimport();
            foreach (var material in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
            {
                int index = Array.FindIndex(Names, n => material.name == "BBB_Door_" + n);
                if (index < 0) throw new InvalidOperationException("Unknown FBX material " + material.name);
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), materials[index]);
            }
            modelImporter.SaveAndReimport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("GrandEntrance");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            var meshes = model.GetComponentsInChildren<MeshFilter>();
            if (meshes.Length != 3) throw new InvalidOperationException("Expected frame and two door meshes");
            foreach (var mesh in meshes.Where(m => m.name != "Frame"))
            {
                var box = mesh.gameObject.AddComponent<BoxCollider>();
                var bounds = mesh.sharedMesh.bounds;
                box.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
                box.size = new Vector3(1.94f, 3.66f, .26f);
            }
            var collision = new GameObject("FrameCollision");
            collision.transform.SetParent(root.transform, false);
            for (int side = -1; side <= 1; side += 2)
                AddBox(collision.transform, "Jamb", new Vector3(side * 2.16f, 1.925f, 0), new Vector3(.76f, 3.85f, .88f), 0);
            AddBox(collision.transform, "Threshold", new Vector3(0, .055f, 0), new Vector3(4.08f, .11f, .8f), 0);
            // The opaque transom closes the upper opening; segmented arch follows its silhouette.
            AddBox(collision.transform, "Transom", new Vector3(0, 4.36f, 0), new Vector3(3.0f, 1.10f, .28f), 0);
            for (int i = 0; i < 9; i++)
            {
                float angle = (i + .5f) * Mathf.PI / 9f;
                var center = new Vector3(2.23f * Mathf.Cos(angle), 3.85f + 1.82f * Mathf.Sin(angle), 0);
                float tangent = Mathf.Atan2(1.82f * Mathf.Cos(angle), -2.23f * Mathf.Sin(angle)) * Mathf.Rad2Deg;
                AddBox(collision.transform, "Arch_" + i, center, new Vector3(.80f, .42f, .62f), tangent);
            }
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/GrandEntrance.prefab");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .58f, .62f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .35f, .36f);
            RenderSettings.ambientGroundColor = new Color(.2f, .18f, .15f);
            AddLight("Preview Key", new Vector3(40, -35, 0), 1.4f);
            AddLight("Preview Rear Fill", new Vector3(35, 145, 0), 1.2f);
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(1.8f, 3.5f, -11);
            camera.transform.LookAt(new Vector3(0, 3, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.18f, .19f, .20f);
            camera.fieldOfView = 38;
            camera.nearClipPlane = .1f;
            EditorSceneManager.SaveScene(scene, Preview + "/GrandEntrancePreview.unity");
            AssetDatabase.SaveAssets();
            ValidateAssets();
            RenderPreview(camera, -1, "ViewA");
            RenderPreview(camera, 1, "ViewB");
            Debug.Log("GRAND_ENTRANCE_BUILD_SUCCESS");
        }

        private static void AddBox(Transform parent, string name, Vector3 center, Vector3 size, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void AddLight(string name, Vector3 rotation, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.transform.rotation = Quaternion.Euler(rotation);
            light.shadows = LightShadows.Soft;
        }

        private static void RenderPreview(Camera camera, int side, string name)
        {
            camera.transform.position = new Vector3(1.8f, 3.5f, side * 11);
            camera.transform.LookAt(new Vector3(0, 3, 0));
            var rt = new RenderTexture(1000, 1000, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = rt;
            try
            {
                // Allow the render pipeline to initialize before reading the first frame.
                camera.Render();
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(1000, 1000, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0);
                image.Apply();
                File.WriteAllBytes("Docs/Arts/Props/Models/GrandEntrance_Unity_" + name + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        [MenuItem("Tools/Buy Bid Blow/Capture Grand Entrance Preview")]
        public static void CapturePreview()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Preview + "/GrandEntrancePreview.unity")
                throw new InvalidOperationException("Open GrandEntrancePreview.unity first");
            var camera = GameObject.Find("Preview Camera").GetComponent<Camera>();
            var position = camera.transform.position;
            var rotation = camera.transform.rotation;
            try
            {
                RenderPreview(camera, -1, "ViewA");
                RenderPreview(camera, 1, "ViewB");
                ValidateAssets();
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); }
            Debug.Log("GRAND_ENTRANCE_PREVIEW_SUCCESS");
        }

        public static void ValidateAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/GrandEntrance.prefab");
            if (prefab == null) throw new InvalidOperationException("Missing prefab");
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length != 3) throw new InvalidOperationException("Expected three renderers");
            var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            if (materials.Length != 3) throw new InvalidOperationException("Expected three materials");
            foreach (var mat in materials)
                if (mat == null || mat.shader.name != "Universal Render Pipeline/Lit" || mat.GetTexture("_BaseMap") == null || mat.GetTexture("_BumpMap") == null)
                    throw new InvalidOperationException("Invalid URP material or texture");
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    throw new InvalidOperationException("Missing script");
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh == null) throw new InvalidOperationException("Missing mesh");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var bounds = instance.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a,b) => { a.Encapsulate(b); return a; });
                if (Mathf.Abs(bounds.size.y - 5.99f) > .12f || Mathf.Abs(bounds.size.x - 5.08f) > .2f)
                    throw new InvalidOperationException("Incorrect scale or axis: " + bounds.size);
                foreach (var name in new[] { "LeftDoor", "RightDoor" })
                {
                    var leaf = instance.GetComponentsInChildren<Transform>().Single(t => t.name == name);
                    if (Mathf.Abs(Mathf.Abs(leaf.position.x) - 1.94f) > .02f || Mathf.Abs(leaf.position.y - .14f) > .02f)
                        throw new InvalidOperationException("Incorrect hinge origin: " + leaf.position);
                    var before = leaf.GetComponent<Renderer>().bounds.center;
                    leaf.localRotation *= Quaternion.Euler(0, 60, 0);
                    if (Vector3.Distance(before, leaf.GetComponent<Renderer>().bounds.center) < .5f)
                        throw new InvalidOperationException("Door does not rotate around hinge");
                    if (leaf.GetComponent<BoxCollider>() == null) throw new InvalidOperationException("Missing leaf collision");
                }
                if (EditorBuildSettings.scenes.Any(s => s.path == Preview + "/GrandEntrancePreview.unity"))
                    throw new InvalidOperationException("Preview scene must not enter build settings");
                File.WriteAllText(Preview + "/Validation.txt", "PASS: 3 renderers, 3 URP materials, 6 textures, valid meshes, no missing scripts, hinge rotation, leaf colliders, preview excluded from build.\nBounds: " + bounds.size + "\n");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
