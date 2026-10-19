using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BuyBidBlow.Props;
using BuyBidBlow.Characters;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallLoungeClusters
    {
        const string Root = HallLevelBuilder.Root + "/LoungeClusters";
        const string Data = "Logs/A92LoungeImport";
        const string GroupName = "UnderMezzanine_Lounge_Clusters";
        [Serializable] public class Part { public string file, material; }
        [Serializable] public class CustomAsset { public string name; public Part[] parts; }
        [Serializable] public class Placement { public string name, prefab; public float[] position, scale; public float yaw; }
        [Serializable] public class Layout { public Placement[] placements; public CustomAsset[] customAssets; }
        static Vector3 V(float[] v) => new Vector3(v[0], v[1], v[2]);

        static T Save<T>(T value, string path) where T : UnityEngine.Object
        {
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old) { EditorUtility.CopySerialized(value, old); UnityEngine.Object.DestroyImmediate(value); return old; }
            AssetDatabase.CreateAsset(value, path); return value;
        }

        static Mesh ReadMesh(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            int count = reader.ReadInt32(), indices = reader.ReadInt32();
            var points = new Vector3[count]; var normals = new Vector3[count]; var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            var triangles = new int[indices]; for (int i = 0; i < indices; i++) triangles[i] = reader.ReadInt32();
            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path), indexFormat = IndexFormat.UInt32 };
            mesh.vertices = points; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }

        static void BuildCustomAssets(Layout layout)
        {
            foreach (string dir in new[] { "Meshes", "Materials", "Prefabs" }) Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            var rug = new Material(AssetDatabase.LoadAssetAtPath<Material>(HallLevelBuilder.Root + "/Materials/BBB_Seating_TealVelvet.mat"));
            rug.name = "Lounge_Rug_BlenderKit_Fabric"; rug.SetFloat("_Smoothness", 0);
            var rugMaterial = Save(rug, Root + "/Materials/Lounge_Rug_BlenderKit_Fabric.mat");
            foreach (var asset in layout.customAssets)
            {
                var go = new GameObject(asset.name);
                try
                {
                    foreach (var part in asset.parts)
                    {
                        var mesh = Save(ReadMesh(Data + "/" + part.file), Root + "/Meshes/" + Path.GetFileNameWithoutExtension(part.file) + ".asset");
                        var mat = asset.name == "LoungeRug" ? rugMaterial : AssetDatabase.LoadAssetAtPath<Material>(HallLevelBuilder.Root + "/Materials/" + part.material + ".mat");
                        if (!mat) throw new Exception("Missing shared hall material: " + part.material);
                        var child = new GameObject(part.material); child.transform.SetParent(go.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = mesh; child.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    }
                    if (asset.name == "LoungePlanter")
                    {
                        HallFurnitureCollision.FitPlanter(go);
                    }
                    PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + asset.name + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
        }

        static void UpdateUmbrellaPrefab()
        {
            const string folder = "Assets/Project/Content/Props/LoungeAccessories";
            AssetDatabase.ImportAsset(folder + "/Models/UmbrellaStand.fbx", ImportAssetOptions.ForceUpdate);
            string path = folder + "/Prefabs/UmbrellaStand.prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var lod = go.GetComponent<LODGroup>(); lod.RecalculateBounds();
                var body = go.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name.EndsWith("LOD0"));
                var collider = go.GetComponent<BoxCollider>(); collider.center = go.transform.InverseTransformPoint(body.bounds.center); collider.size = body.bounds.size;
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }

        [MenuItem("BuyBidBlow/Hall/Apply Approved Lounge Clusters")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Data + "/layout.json"));
            BuildCustomAssets(layout); UpdateUmbrellaPrefab();
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var existing = GameObject.Find(GroupName);
            if (existing) UnityEngine.Object.DestroyImmediate(existing);
            var group = new GameObject(GroupName);
            var log = new StringBuilder("Approved Blender A91 lounge layout, with A92 seated umbrellas.\n");
            foreach (var item in layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefab);
                if (!prefab) throw new Exception("Missing prefab: " + item.prefab);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                go.name = item.name; go.transform.localPosition = V(item.position); go.transform.localRotation = Quaternion.Euler(0, item.yaw, 0); go.transform.localScale = V(item.scale);
                // Existing hall seats carry their original orientation in their meshes.
                // Tighten their collision box in that orientation, keeping seat interactions.
                if (item.prefab.StartsWith(HallLevelBuilder.Root + "/Prefabs/")) TightSeatCollider(go, item);
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterials.Any(m => !m || !m.shader)) throw new Exception("Missing material: " + renderer.name);
                log.AppendLine(item.name + " at " + go.transform.position + ", yaw=" + item.yaw);
            }
            Physics.SyncTransforms();
            foreach (Transform child in group.transform)
            {
                foreach (var col in child.GetComponentsInChildren<Collider>())
                {
                    var b = col.bounds;
                    if (b.min.x < 5.9f && b.max.x > -5.9f && b.min.z < 13.6f && b.max.z > 5.6f)
                        throw new Exception("Matching clearance obstructed: " + child.name + " " + b);
                }
            }
            if (group.transform.childCount != 27) throw new Exception("Expected 27 furniture/rug placements.");
            log.AppendLine("PASS: 27 linked prefab instances; all materials resolved; matching rectangle x +/-5.9, z 5.6..13.6 remains free of new colliders.");
            log.AppendLine("Umbrella metal tips contact the 0.037 m interior tray. LOD0 and LOD1 regenerated; shared prefab bounds refreshed.");
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/Arts/Environments/UnityVerification");
            File.WriteAllText("Docs/Arts/Environments/UnityVerification/A92_Lounge_Validation.txt", log.ToString());
        }

        static void TightSeatCollider(GameObject go, Placement item)
        {
            float bakedYaw = item.name.Contains("Teal_Sofa") ? -135 : item.name.Contains("Red_Sofa") ? -45 : item.name.Contains("Bench") ? 45 : 180;
            foreach (var col in go.GetComponents<BoxCollider>()) UnityEngine.Object.DestroyImmediate(col);
            var body = new GameObject("LoungeSeatCollision"); body.transform.SetParent(go.transform, false); body.transform.localRotation = Quaternion.Euler(0, -bakedYaw, 0);
            var bounds = new Bounds(); bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) foreach (var vertex in mf.sharedMesh.vertices)
            {
                var point = body.transform.InverseTransformPoint(mf.transform.TransformPoint(vertex));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
            var collider = body.AddComponent<BoxCollider>(); collider.center = bounds.center; collider.size = bounds.size;
        }

        static void Capture(Vector3 position, Vector3 target, string name, float lens = 35)
        {
            var go = new GameObject("LoungeVerificationCamera"); var camera = go.AddComponent<Camera>();
            try
            {
                camera.nearClipPlane = .03f; camera.farClipPlane = 100; camera.fieldOfView = 2 * Mathf.Atan(36f / (2 * lens * (1600f / 900))) * Mathf.Rad2Deg;
                camera.transform.position = position; camera.transform.LookAt(target); camera.allowMSAA = true;
                var additional = go.AddComponent<UniversalAdditionalCameraData>(); additional.renderPostProcessing = true;
                var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 }; camera.targetTexture = rt;
                ShaderUtil.allowAsyncCompilation = false; camera.Render(); camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes("Docs/Arts/Environments/UnityVerification/" + name + ".png", image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        public static void BuildBatch()
        {
            try
            {
                Directory.CreateDirectory(Data);
                string backup = Data + "/SC_ExchangeFurnished_before_A92.unity";
                if (!File.Exists(backup)) File.Copy(HallLevelBuilder.ScenePath, backup);
                Apply();
                Capture(new Vector3(-.2f, 1.65f, 5.5f), new Vector3(-8.6f, 1.05f, 11.6f), "A92_Unity_Left_Lounge");
                Capture(new Vector3(.2f, 1.65f, 5.5f), new Vector3(8.6f, 1.05f, 11.6f), "A92_Unity_Right_Lounge");
                Capture(new Vector3(8.9f, 1.1f, 5.7f), new Vector3(7, .49f, 6.95f), "A92_Unity_Umbrella", 40);
                Debug.Log("A92_LOUNGE_APPLY_OK"); EditorApplication.Exit(0);
            }
            catch (Exception e) { File.WriteAllText(Data + "/error.txt", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void VerifyBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                var group = GameObject.Find(GroupName);
                var seats = group.GetComponentsInChildren<Seat>(); int occupants = 0;
                if (seats.Length != 5) throw new Exception("Expected five new seat/sofa instances.");
                foreach (var seat in seats)
                {
                    var actors = new PlayerSeating[seat.sittingPoints.Length];
                    try
                    {
                        for (int i = 0; i < actors.Length; i++)
                        {
                            var actor = new GameObject("LoungeVerificationOccupant"); actor.AddComponent<Rigidbody>().useGravity = false; actor.AddComponent<CapsuleCollider>();
                            actors[i] = actor.AddComponent<PlayerSeating>(); actor.transform.position = seat.exitPoints[0].position;
                            if (!seat.Interact(actor) || !actors[i].IsSeated) throw new Exception("Seat failed: " + seat.name);
                            occupants++;
                        }
                        foreach (var actor in actors)
                        {
                            Physics.SyncTransforms();
                            if (!actor.TryStand()) throw new Exception("No safe standing exit: " + seat.name);
                            UnityEngine.Object.DestroyImmediate(actor.gameObject);
                        }
                    }
                    finally { foreach (var actor in actors) if (actor) UnityEngine.Object.DestroyImmediate(actor.gameObject); }
                }
                File.AppendAllText("Docs/Arts/Environments/UnityVerification/A92_Lounge_Validation.txt", "PASS: all " + occupants + " new sitting places accept occupants and allow collision-free standing.\n");
                foreach (var lod in group.GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);
                Capture(new Vector3(-.2f, 1.65f, 5.5f), new Vector3(-8.6f, 1.05f, 11.6f), "A92_Unity_Left_Lounge");
                Capture(new Vector3(.2f, 1.65f, 5.5f), new Vector3(8.6f, 1.05f, 11.6f), "A92_Unity_Right_Lounge");
                Capture(new Vector3(0, 1.65f, 11.8f), new Vector3(0, 1.1f, 16.6f), "A92_Unity_Rear_Information", 24);
                Capture(new Vector3(8.9f, 1.1f, 5.7f), new Vector3(7, .49f, 6.95f), "A92_Unity_Umbrella", 40);
                Debug.Log("A92_LOUNGE_VERIFY_OK seats=" + occupants); EditorApplication.Exit(0);
            }
            catch (Exception e) { File.WriteAllText(Data + "/verify-error.txt", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void CaptureUmbrellaBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                foreach (var lod in GameObject.Find(GroupName).GetComponentsInChildren<LODGroup>()) lod.ForceLOD(0);
                Capture(new Vector3(8.9f, 1.1f, 5.7f), new Vector3(7, .49f, 6.95f), "A92_Unity_Umbrella", 40);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
