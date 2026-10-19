using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Dev.Editor
{
    public static class LoungeAccessoriesBuilder
    {
        const string Root = "Assets/Project/Content/Props/LoungeAccessories";
        const string Preview = "Assets/Project/Dev/Sandbox/LoungeAccessories";
        static readonly string[] Names = { "DisplayCabinet", "LeafletRack", "UmbrellaStand" };

        [MenuItem("BuyBidBlow/Props/Build Lounge Accessories")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Preview);
            AssetDatabase.Refresh();
            var report = new StringBuilder("Unity imported mesh counts (UV and normal splits included). Cabinet includes 10 glass triangles in each LOD.\n");
            foreach (var name in new[] { "Lounge_BK_Atlas_BaseColor", "Lounge_BK_Atlas_MetallicSmoothness", "Lounge_BK_Atlas_Normal", "Lounge_BK_Glass_MetallicSmoothness" })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(Root + "/Textures/" + name + ".png");
                imp.textureType = name.EndsWith("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                imp.sRGBTexture = name.EndsWith("BaseColor");
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = true; imp.maxTextureSize = 1024; imp.anisoLevel = 2;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
            }
            var opaque = LoadMaterial("Lounge_BK_Atlas");
            opaque.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Lounge_BK_Atlas_BaseColor.png"));
            opaque.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Lounge_BK_Atlas_MetallicSmoothness.png"));
            opaque.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Lounge_BK_Atlas_Normal.png"));
            opaque.SetFloat("_BumpScale", 1); opaque.EnableKeyword("_NORMALMAP");
            opaque.SetColor("_BaseColor", Color.white);
            opaque.SetFloat("_Metallic", 1); opaque.SetFloat("_Smoothness", 1);
            opaque.EnableKeyword("_METALLICSPECGLOSSMAP");
            var glass = LoadMaterial("Lounge_BK_Glass");
            glass.SetColor("_BaseColor", new Color(.93f, .98f, .97f, .075f));
            glass.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Lounge_BK_Glass_MetallicSmoothness.png"));
            glass.EnableKeyword("_METALLICSPECGLOSSMAP");
            glass.SetFloat("_Smoothness", 1); glass.SetFloat("_Surface", 1);
            glass.SetFloat("_Blend", 0); glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            glass.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0); glass.SetFloat("_Cull", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.SetOverrideTag("RenderType", "Transparent"); glass.renderQueue = 3000;
            glass.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(opaque); EditorUtility.SetDirty(glass);
            foreach (var name in Names)
            {
                string path = Root + "/Models/" + name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importCameras = false; importer.importLights = false;
                importer.importAnimation = false; importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.globalScale = 1;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                go.name = name;
                try
                {
                    foreach (var old in go.GetComponentsInChildren<LODGroup>()) UnityEngine.Object.DestroyImmediate(old);
                    var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.gameObject.SetActive(true);
                        bool transparent = renderer.name.Contains("Glass");
                        renderer.sharedMaterial = transparent ? glass : opaque;
                        renderer.shadowCastingMode = transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    }
                    var high = renderers.Where(r => r.name.Contains("LOD0") || r.name.Contains("Glass")).ToArray();
                    var low = renderers.Where(r => r.name.Contains("LOD1") || r.name.Contains("Glass")).ToArray();
                    if (high.Length == 0 || low.Length == 0) throw new Exception("LOD meshes missing: " + name);
                    var group = go.AddComponent<LODGroup>();
                    group.SetLODs(new[] { new LOD(.14f, high), new LOD(.018f, low) });
                    group.RecalculateBounds();
                    var bounds = high[0].bounds;
                    foreach (var renderer in high.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    if (bounds.size.y < .7f || bounds.size.y > 1.4f || Mathf.Abs(bounds.min.y) > .035f)
                        throw new Exception("Invalid metre scale or floor pivot: " + name + " " + bounds);
                    var collider = go.AddComponent<BoxCollider>();
                    collider.center = go.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size;
                    foreach (var entry in new[] { (label: "LOD0", meshes: high), (label: "LOD1", meshes: low) })
                    {
                        int vertices = 0, triangles = 0;
                        foreach (var renderer in entry.meshes)
                        {
                            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                            vertices += mesh.vertexCount;
                            for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
                            if (renderer.sharedMaterial == null) throw new Exception("Missing material");
                        }
                        report.AppendLine($"{name} {entry.label}: {vertices} vertices, {triangles} triangles, {entry.meshes.Length} renderers");
                    }
                    report.AppendLine($"  Bounds (metres): {bounds.size}; floor pivot; one box collider; LOD screen heights .14/.018");
                    PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + name + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/lounge-accessories-result.txt", report.ToString());
        }

        static Material LoadMaterial(string name)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            return mat;
        }

        public static void BuildBatch()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Build();
                var scene = EditorSceneManager.GetActiveScene();
                float[] positions = { -1.25f, .22f, 1.21f };
                for (int i = 0; i < Names.Length; i++)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + Names[i] + ".prefab"));
                    model.transform.position = new Vector3(positions[i], 0, 0);
                    model.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.6f, .66f, .69f);
                RenderSettings.ambientEquatorColor = new Color(.4f, .43f, .42f);
                RenderSettings.ambientGroundColor = new Color(.25f, .26f, .24f);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Preview Floor";
                floor.transform.position = new Vector3(0, -.005f, 0);
                var floorMat = LoadMaterial("PreviewFloor");
                floorMat.SetColor("_BaseColor", new Color(.19f, .22f, .20f));
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                EditorUtility.SetDirty(floorMat); AssetDatabase.SaveAssets();
                var light = new GameObject("Preview Key Light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(40, -35, 0);
                var camera = new GameObject("Preview Camera").AddComponent<Camera>();
                camera.transform.position = new Vector3(2.3f, 2.65f, -5.2f);
                camera.transform.LookAt(new Vector3(-.18f, .57f, 0));
                camera.orthographic = true; camera.orthographicSize = 1.1f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.16f, .2f, .19f);
                camera.nearClipPlane = .05f;
                EditorSceneManager.SaveScene(scene, Preview + "/SC_LoungeAccessories.unity");
                ShaderUtil.allowAsyncCompilation = false;
                var rt = new RenderTexture(1600, 1000, 24); camera.targetTexture = rt;
                camera.Render(); camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes("Docs/Arts/Props/Models/BBB_Lounge_Accessories_BlenderKit_Unity.png", image.EncodeToPNG());
                camera.targetTexture = null; RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e); File.WriteAllText("Logs/lounge-accessories-result.txt", e.ToString()); EditorApplication.Exit(1);
            }
        }
    }
}
