using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Dev.Editor
{
    public static class FurnitureUtilityPackBuilder
    {
        const string Root = "Assets/Project/Content/Props/FurnitureUtilityPack";
        const string Shared = "Assets/Project/Content/Props/LoungeAccessories/Materials";
        const string Preview = "Assets/Project/Dev/Sandbox/FurnitureUtilityPack";
        const string Docs = "Docs/Arts/Props/Models/FurnitureUtilityPack";
        [Serializable] public class ColliderSpec { public float[] center; public float[] size; }
        [Serializable] public class AssetSpec { public string name; public ColliderSpec[] colliders; }
        [Serializable] public class Manifest { public AssetSpec[] assets; }
        [Serializable] public class Budget { public string name; public int lod0Vertices, lod0Triangles, lod1Vertices, lod1Triangles, colliders; public Vector3 sizeMetres; }
        [Serializable] public class BudgetList { public List<Budget> assets = new List<Budget>(); }

        static Material Material(string name, string texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", .28f);
            mat.SetFloat("_Metallic", 0);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + texture + ".png"));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Bounds BoundsOf(IEnumerable<Renderer> renderers)
        {
            var list = renderers.ToArray();
            var bounds = list[0].bounds;
            foreach (var renderer in list.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static (int vertices, int triangles) Counts(IEnumerable<Renderer> renderers)
        {
            int vertices = 0, triangles = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                vertices += mesh.vertexCount;
                for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
                if (!renderer.sharedMaterial || !renderer.sharedMaterial.GetTexture("_BaseMap") && !renderer.name.Contains("Glass"))
                    throw new Exception("Missing material or base map: " + renderer.name);
            }
            return (vertices, triangles);
        }

        [MenuItem("BuyBidBlow/Props/Build Furniture Utility Pack")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Preview);
            Directory.CreateDirectory(Docs);
            AssetDatabase.Refresh();
            foreach (var file in Directory.GetFiles(Root + "/Textures", "*.png"))
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.maxTextureSize = 1024;
                imp.alphaSource = TextureImporterAlphaSource.None;
                imp.wrapMode = TextureWrapMode.Clamp; imp.anisoLevel = 4;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
            }
            var atlas = AssetDatabase.LoadAssetAtPath<Material>(Shared + "/Lounge_BK_Atlas.mat");
            var glass = AssetDatabase.LoadAssetAtPath<Material>(Shared + "/Lounge_BK_Glass.mat");
            if (!atlas || !glass) throw new Exception("Shared BlenderKit materials are required.");
            var accents = Material("Furniture_Accents", "Furniture_Accents_BaseColor");
            var signA = Material("InformationBoard_A", "InformationBoard_A_Text");
            var signB = Material("InformationBoard_B", "InformationBoard_B_Text");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/FurnitureUtilityManifest.json"));
            var budgets = new BudgetList();
            var report = new StringBuilder("Unity imported mesh counts include UV and normal splits.\nLOD0 / LOD1; signs and glass included in both.\n\n");
            foreach (var entry in manifest.assets)
            {
                string path = Root + "/Models/" + entry.name + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
                importer.isReadable = false; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.globalScale = 1; importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                go.name = entry.name;
                try
                {
                    foreach (var old in go.GetComponentsInChildren<LODGroup>()) UnityEngine.Object.DestroyImmediate(old);
                    var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.gameObject.SetActive(true);
                        bool isGlass = renderer.name.Contains("Glass");
                        bool isSign = renderer.name.Contains("SignFace");
                        renderer.sharedMaterial = isGlass ? glass : isSign ? (entry.name.EndsWith("_A") ? signA : signB) : renderer.name.Contains("Accents") ? accents : atlas;
                        renderer.shadowCastingMode = isGlass ? ShadowCastingMode.Off : ShadowCastingMode.On;
                        if (isSign && renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount != 4)
                            throw new Exception("Sign should be a four-vertex textured quad: " + entry.name);
                    }
                    var high = renderers.Where(r => r.name.EndsWith("LOD0") || r.name.Contains("SignFace") || r.name.Contains("Glass")).ToArray();
                    var low = renderers.Where(r => r.name.EndsWith("LOD1") || r.name.Contains("SignFace") || r.name.Contains("Glass")).ToArray();
                    if (high.Length == 0 || low.Length == 0) throw new Exception("Missing LOD: " + entry.name);
                    var group = go.AddComponent<LODGroup>();
                    group.SetLODs(new[] { new LOD(.14f, high), new LOD(.018f, low) });
                    group.RecalculateBounds();
                    var bounds = BoundsOf(high);
                    if (bounds.size.y < .3f || bounds.size.y > 2.1f || entry.name != "Shelf_B" && Mathf.Abs(bounds.min.y) > .035f)
                        throw new Exception("Invalid metre scale or floor contact: " + entry.name + " " + bounds);
                    // FBX maps Blender (x,y,z) to Unity (-x,z,-y).
                    foreach (var spec in entry.colliders)
                    {
                        var collider = go.AddComponent<BoxCollider>();
                        collider.center = new Vector3(-spec.center[0], spec.center[1], -spec.center[2]);
                        collider.size = new Vector3(spec.size[0], spec.size[1], spec.size[2]);
                    }
                    var a = Counts(high); var b = Counts(low);
                    if (b.triangles >= a.triangles) throw new Exception("LOD does not reduce geometry: " + entry.name);
                    budgets.assets.Add(new Budget { name = entry.name, lod0Vertices = a.vertices, lod0Triangles = a.triangles,
                        lod1Vertices = b.vertices, lod1Triangles = b.triangles, colliders = entry.colliders.Length, sizeMetres = bounds.size });
                    report.AppendLine($"{entry.name}: LOD0 {a.vertices} vertices / {a.triangles} triangles; LOD1 {b.vertices} vertices / {b.triangles} triangles");
                    report.AppendLine($"  Size {bounds.size} m; bounds centre {bounds.center}; {entry.colliders.Length} box colliders; {high.Length}/{low.Length} renderers");
                    PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + entry.name + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Docs + "/Unity_Budget.json", JsonUtility.ToJson(budgets, true));
            File.WriteAllText(Docs + "/Unity_Budget.txt", report.ToString());
            File.WriteAllText("Logs/furniture-utility-result.txt", report.ToString());
        }

        static void Capture(Camera camera, string name)
        {
            const int width = 1400, height = 1000;
            var rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render(); camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Docs + "/" + name + "_Unity.png", image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        }

        static void PreviewReflections()
        {
            string path = Preview + "/StudioReflection.asset";
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (!cube)
            {
                const int size = 64;
                cube = new Cubemap(size, TextureFormat.RGBAHalf, true) { name = "StudioReflection" };
                for (int face = 0; face < 6; face++)
                {
                    var pixels = new Color[size * size];
                    for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    {
                        float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1;
                        Vector3 direction = face == 0 ? new Vector3(1, -v, -u) : face == 1 ? new Vector3(-1, -v, u)
                            : face == 2 ? new Vector3(u, 1, v) : face == 3 ? new Vector3(u, -1, -v)
                            : face == 4 ? new Vector3(u, -v, 1) : new Vector3(-u, -v, -1);
                        direction.Normalize();
                        var color = Color.Lerp(new Color(.13f, .12f, .10f), new Color(.72f, .69f, .61f), direction.y * .5f + .5f);
                        float softbox = Mathf.Pow(Mathf.Max(0, Vector3.Dot(direction, new Vector3(-.5f, .65f, -.57f).normalized)), 14);
                        color += new Color(1.2f, 1.12f, .94f) * softbox;
                        pixels[y * size + x] = color;
                    }
                    cube.SetPixels(pixels, (CubemapFace)face);
                }
                cube.Apply(true, false); AssetDatabase.CreateAsset(cube, path);
            }
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 1;
        }

        public static void BuildBatch()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Build();
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.67f, .70f, .74f);
                RenderSettings.ambientEquatorColor = new Color(.45f, .46f, .44f);
                RenderSettings.ambientGroundColor = new Color(.29f, .27f, .24f);
                PreviewReflections();
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Preview Floor"; floor.transform.localScale = Vector3.one * 2;
                floor.transform.position = new Vector3(0, -.008f, 0);
                floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Shared + "/PreviewFloor.mat");
                var key = new GameObject("Preview Key").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 1.2f; key.transform.rotation = Quaternion.Euler(42, -35, 0);
                key.shadows = LightShadows.Soft;
                var fill = new GameObject("Preview Fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = .65f; fill.transform.rotation = Quaternion.Euler(30, 145, 0);
                var camera = new GameObject("Preview Camera").AddComponent<Camera>();
                camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                camera.allowMSAA = true;
                camera.orthographic = true; camera.nearClipPlane = .05f; camera.farClipPlane = 60;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.19f, .22f, .20f);
                ShaderUtil.allowAsyncCompilation = false;
                string[] types = { "InformationBoard", "ServiceTrolley", "Table", "Bookcase", "Shelf" };
                var instances = new List<GameObject>();
                for (int i = 0; i < types.Length; i++)
                {
                    var pair = new List<GameObject>();
                    foreach (string variant in new[] { "A", "B" })
                    {
                        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + types[i] + "_" + variant + ".prefab"));
                        model.transform.rotation = Quaternion.Euler(0, 180, 0);
                        model.GetComponent<LODGroup>().ForceLOD(0);
                        pair.Add(model); instances.Add(model);
                    }
                    var a = BoundsOf(pair[0].GetComponentsInChildren<MeshRenderer>().Where(r => !r.name.EndsWith("LOD1")));
                    var b = BoundsOf(pair[1].GetComponentsInChildren<MeshRenderer>().Where(r => !r.name.EndsWith("LOD1")));
                    float gap = .32f;
                    pair[0].transform.position = new Vector3(-(b.size.x + gap) / 2, 0, 0);
                    pair[1].transform.position = new Vector3((a.size.x + gap) / 2, 0, 0);
                    var total = BoundsOf(pair.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>()).Where(r => !r.name.EndsWith("LOD1")));
                    Vector3 target = total.center;
                    camera.transform.position = target + new Vector3(1.8f, 1.9f, -7);
                    camera.transform.LookAt(target);
                    float halfHeight = 0, halfWidth = 0;
                    for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                    {
                        var corner = total.center + Vector3.Scale(total.extents, new Vector3(x, y, z));
                        var local = camera.transform.InverseTransformPoint(corner);
                        halfHeight = Mathf.Max(halfHeight, Mathf.Abs(local.y)); halfWidth = Mathf.Max(halfWidth, Mathf.Abs(local.x));
                    }
                    camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / 1.4f) * 1.16f;
                    Capture(camera, types[i]);
                    foreach (var model in pair) model.SetActive(false);
                }
                for (int i = 0; i < instances.Count; i++)
                {
                    var model = instances[i]; model.SetActive(true);
                    model.transform.position = new Vector3((i / 2 - 2) * 2.2f, 0, (i % 2) * 2.2f);
                    model.GetComponent<LODGroup>().ForceLOD(-1);
                }
                camera.transform.position = new Vector3(3.5f, 6, -10);
                camera.transform.LookAt(new Vector3(0, .6f, 1)); camera.orthographicSize = 4.3f;
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Preview + "/SC_FurnitureUtilityPack.unity");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e); File.WriteAllText("Logs/furniture-utility-result.txt", e.ToString()); EditorApplication.Exit(1);
            }
        }
    }
}
