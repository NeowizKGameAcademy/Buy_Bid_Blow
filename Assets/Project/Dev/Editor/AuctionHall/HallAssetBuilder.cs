using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallAssetBuilder
    {
        const string Root = "Assets/Project/Content/Levels/Exchange";
        const string PreviewRoot = "Assets/Project/Dev/Sandbox/AuctionHall";
        public static void ValidateAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/AuctionHall.prefab");
            if (prefab == null) throw new InvalidOperationException("Missing hall prefab");
            foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (mesh.sharedMesh == null) throw new InvalidOperationException("Missing mesh: " + mesh.name);
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.GetTexture("_BaseMap") == null || material.GetTexture("_BumpMap") == null)
                        throw new InvalidOperationException("Missing material or texture: " + renderer.name);
            var scene = EditorSceneManager.OpenPreviewScene(PreviewRoot + "/AuctionHallPreview.unity");
            try
            {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new InvalidOperationException("Missing script: " + transform.name);
            var walker = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HallWalkthrough>(true)).FirstOrDefault();
            if (walker == null || walker.view == null) throw new InvalidOperationException("Missing walkthrough camera reference");
            if (EditorBuildSettings.scenes.Any(s => s.path == scene.path))
                throw new InvalidOperationException("Preview scene must not be in build settings");
            Debug.Log("HALL_RELOCATION_SUCCESS: meshes, materials, textures, scripts, camera and build exclusion verified.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [MenuItem("Tools/Buy Bid Blow/Build Hall Preview")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(PreviewRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in Directory.GetFiles(Root + "/Textures", "Hall_*.png"))
            {
                var path = file.Replace('\\', '/');
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal;
                ti.mipmapEnabled = true;
                ti.maxTextureSize = 1024;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.anisoLevel = 4;
                ti.SaveAndReimport();
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader not found");
            string[] names = { "Floor", "Wood", "Teal", "Ceiling", "Brass" };
            string[] normals = { "Floor", "Wood", "Plaster", "Plaster", "Brass" };
            float[] smooth = { .35f, .28f, .10f, .10f, .52f };
            float[] bumps = { .12f, .10f, .05f, .05f, .08f };
            foreach (var i in Enumerable.Range(0, names.Length))
            {
                var path = Root + "/Materials/Hall_" + names[i] + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Hall_" + names[i] + ".png"));
                mat.SetColor("_BaseColor", Color.white);
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Hall_" + normals[i] + "_Normal.png"));
                mat.SetFloat("_BumpScale", bumps[i]); mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_Metallic", i == 4 ? .8f : 0); mat.SetFloat("_Smoothness", smooth[i]);
                EditorUtility.SetDirty(mat);
            }
            var modelPath = Root + "/Models/AuctionHall.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importCameras = false; importer.importLights = false;
            importer.importAnimation = false; importer.isReadable = true;
            importer.generateSecondaryUV = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            instance.name = "AuctionHall";
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.StartsWith("COL_")) { renderer.enabled = false; continue; }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    if (m == null) throw new InvalidOperationException("Missing imported material: " + renderer.name);
                    var key = names.FirstOrDefault(n => m.name.StartsWith("Hall_" + n));
                    if (key == null) throw new InvalidOperationException("Unexpected material: " + m.name);
                    return AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Hall_" + key + ".mat");
                }).ToArray();
            }
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
            {
                bool collider = mf.name.StartsWith("COL_") || (!mf.name.StartsWith("Hall_Steps") && !mf.name.StartsWith("Hall_Roof"));
                if (collider) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                if (!mf.name.Contains("_Leaf_")) GameObjectUtility.SetStaticEditorFlags(mf.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            var columns = instance.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("Hall_Columns")).ToArray();
            var north = columns.Aggregate(Vector3.zero, (v, r) => v + r.bounds.center) / columns.Length;
            north.y = 0; north.Normalize();
            var player = new GameObject("Walkthrough - RMB to capture, WASD move, Shift run, Esc release");
            player.transform.position = -north * 12 + Vector3.up * .05f;
            player.transform.rotation = Quaternion.LookRotation(north);
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .3f; cc.center = Vector3.up * .9f; cc.stepOffset = .25f; cc.slopeLimit = 45;
            var cameraObject = new GameObject("PlayerCamera"); cameraObject.transform.SetParent(player.transform, false); cameraObject.transform.localPosition = Vector3.up * 1.65f;
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 100; camera.fieldOfView = 75;
            cameraObject.tag = "MainCamera"; cameraObject.AddComponent<AudioListener>();
            player.AddComponent<HallWalkthrough>().view = camera;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.3f, .34f, .36f);
            foreach (var p in new[] { new Vector3(-7,7,-7),new Vector3(7,7,-7),new Vector3(-7,10,7),new Vector3(7,10,7),new Vector3(0,12,0),new Vector3(0,7,12),new Vector3(0,2.4f,8) })
            {
                var go = new GameObject("Preview warm fill"); go.transform.position = Quaternion.LookRotation(north) * p;
                var light = go.AddComponent<Light>(); light.type = LightType.Point; light.range = p.y > 5 ? 22 : 12; light.intensity = p.y > 5 ? 5 : 2; light.color = new Color(1,.78f,.52f); light.shadows = LightShadows.None;
            }
            Physics.SyncTransforms();
            int hits = 0;
            foreach (var local in new[] { new Vector3(0,2,-10),new Vector3(0,2,0),new Vector3(0,6,8),new Vector3(-8.7f,6,4.5f),new Vector3(8.7f,6,4.5f) })
            {
                Vector3 point = Quaternion.LookRotation(north) * local;
                if (!Physics.Raycast(point, Vector3.down, out var hit, 10)) throw new InvalidOperationException("Walkable floor missing at " + point);
                float expected = local.z == 8 ? 4 : local.z == 4.5f ? 2 : 0;
                if (Mathf.Abs(hit.point.y - expected) > .08f) throw new InvalidOperationException("Unexpected collision height " + hit.point.y + " at " + point);
                hits++;
            }
            PrefabUtility.SaveAsPrefabAsset(instance, Root + "/AuctionHall.prefab");
            EditorSceneManager.SaveScene(scene, PreviewRoot + "/AuctionHallPreview.unity");
            AssetDatabase.SaveAssets();
            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
            File.WriteAllText(PreviewRoot + "/Validation.txt", "URP materials: 5\nMesh renderers: " + renderers.Count(r => r.enabled) + "\nColliders: " + instance.GetComponentsInChildren<Collider>().Length + "\nFloor/mezzanine/stair raycasts: " + hits + " passed\nFootprint nominal: 28 x 35 m\nCeiling nominal: 16 m\nPlay: RMB capture, WASD move, Shift run, Esc release.\nStatic doors; restroom interiors not modeled.\n");
            Debug.Log("HALL_BUILD_SUCCESS: prefab, scene, materials and " + hits + " collision checks completed.");
        }
    }
}
