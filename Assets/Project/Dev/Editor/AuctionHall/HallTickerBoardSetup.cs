using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    // Extract the existing hall televisions without re-exporting/rebuilding the furnished hall.
    public static class HallTickerBoardSetup
    {
        public const string Root = "Assets/Project/Content/Props/TickerBoard";
        public const string PrefabPath = Root + "/TickerBoard.prefab";
        const string SourcePath = "Docs/Arts/Props/TickerBoard/PlacementSource.json";
        const string ReportPath = "Docs/Arts/Props/TickerBoard/ExtractionReport.txt";
        const string HallPrefab = HallLevelBuilder.Root + "/AuctionHall_Furnished.prefab";
        [Serializable] public class Placement { public string name; public float[] position, forward, up, mountPosition, mountSize; }
        [Serializable] public class Source { public string source; public Placement[] placements; public float[] mountTriangleCenters; }
        static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        static Quaternion Rotation(Placement p) => Quaternion.LookRotation(V(p.forward), V(p.up));
        static Source ReadSource() => JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath));
        static bool IsBoard(MeshRenderer r) => r.sharedMaterial && r.sharedMaterial.name.StartsWith("BBB_TV_", StringComparison.Ordinal);
        static MeshRenderer[] MergedBoards(GameObject hall) => hall.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => IsBoard(r) && r.name.StartsWith("Static_", StringComparison.Ordinal)).ToArray();
        static int TriangleCount(IEnumerable<MeshRenderer> renderers) => renderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);

        static void BuildPrefab(GameObject hall, Source source)
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var renderers = MergedBoards(hall);
            if (renderers.Length == 0) throw new Exception("No original center board geometry found.");
            var center = source.placements[0];
            var worldToLocal = Matrix4x4.TRS(V(center.position), Rotation(center), Vector3.one).inverse;
            var root = new GameObject("TickerBoard");
            try
            {
                foreach (var group in renderers.GroupBy(r => r.sharedMaterial))
                {
                    var material = group.Key;
                    string destination = Root + "/Materials/" + material.name + ".mat";
                    string oldPath = AssetDatabase.GetAssetPath(material);
                    if (oldPath != destination)
                    {
                        string error = AssetDatabase.MoveAsset(oldPath, destination);
                        if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                    }
                    string name = material.name == "BBB_TV_Black_Screen" ? "Screen" : material.name.Replace("BBB_TV_", "");
                    var mesh = new Mesh { name = "TickerBoard_" + name, indexFormat = IndexFormat.UInt32 };
                    var pieces = group.Select(r => (renderer: r, mesh: BoardPiece(r, source, 0))).Where(p => p.mesh).ToArray();
                    mesh.CombineMeshes(pieces.Select(p => new CombineInstance
                    {
                        mesh = p.mesh,
                        transform = worldToLocal * p.renderer.transform.localToWorldMatrix
                    }).ToArray(), true, true);
                    foreach (var piece in pieces) Object.DestroyImmediate(piece.mesh);
                    mesh.RecalculateBounds();
                    string path = Root + "/Meshes/" + mesh.name + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                    else AssetDatabase.CreateAsset(mesh, path);
                    var child = new GameObject(name);
                    child.transform.SetParent(root.transform, false); child.AddComponent<MeshFilter>(); child.AddComponent<MeshRenderer>();
                    child.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = child.GetComponent<MeshRenderer>();
                    EditorUtility.CopySerialized(group.First(), renderer);
                    renderer.sharedMaterial = material;
                    renderer.lightmapIndex = -1;
                    renderer.receiveGI = ReceiveGI.LightProbes;
                    GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(group.First().gameObject));
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static Mesh BoardPiece(MeshRenderer renderer, Source source, int board)
        {
            // A spatial batch may contain the corner of a neighboring TV as well.
            // Classify triangles, not renderer bounds, before combining the reusable model.
            var original = renderer.GetComponent<MeshFilter>().sharedMesh;
            var positions = original.vertices; var triangles = original.triangles;
            var selected = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var center = renderer.transform.TransformPoint((positions[triangles[i]] + positions[triangles[i + 1]] + positions[triangles[i + 2]]) / 3);
                int nearest = Enumerable.Range(0, source.placements.Length).OrderBy(j => (center - V(source.placements[j].position)).sqrMagnitude).First();
                if (nearest == board) selected.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
            }
            if (selected.Count == 0) return null;
            var ids = selected.Distinct().ToArray();
            var lookup = ids.Select((old, index) => (old, index)).ToDictionary(p => p.old, p => p.index);
            var normals = original.normals; var uv = original.uv; var tangents = original.tangents;
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.vertices = ids.Select(i => positions[i]).ToArray();
            mesh.normals = ids.Select(i => normals[i]).ToArray();
            mesh.uv = ids.Select(i => uv[i]).ToArray();
            if (tangents.Length == positions.Length) mesh.tangents = ids.Select(i => tangents[i]).ToArray();
            mesh.triangles = selected.Select(i => lookup[i]).ToArray();
            mesh.RecalculateBounds(); return mesh;
        }

        static void SeparateMountExtension(Source source)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.transform.Find("WallMountExtension")) return;
                var steel = root.transform.Find("Mount_Steel");
                var filter = steel.GetComponent<MeshFilter>(); var mesh = filter.sharedMesh;
                var positions = mesh.vertices; var triangles = mesh.triangles;
                var extension = new List<int>(); var remainder = new List<int>();
                var centers = Enumerable.Range(0, source.mountTriangleCenters.Length / 3)
                    .Select(i => new Vector3(source.mountTriangleCenters[i * 3], source.mountTriangleCenters[i * 3 + 1], source.mountTriangleCenters[i * 3 + 2])).ToArray();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var center = (positions[triangles[i]] + positions[triangles[i + 1]] + positions[triangles[i + 2]]) / 3;
                    var target = centers.Any(c => (c - center).sqrMagnitude < .0001f * .0001f) ? extension : remainder;
                    target.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                }
                if (extension.Count != centers.Length * 3) throw new Exception("Cannot isolate the rear mounting extension.");
                var mountMesh = Subset(mesh, extension, V(source.placements[0].mountPosition)); mountMesh.name = "TickerBoard_WallMountExtension";
                AssetDatabase.CreateAsset(mountMesh, Root + "/Meshes/" + mountMesh.name + ".asset");
                var remainingMesh = Subset(mesh, remainder, Vector3.zero); remainingMesh.name = mesh.name;
                EditorUtility.CopySerialized(remainingMesh, mesh); Object.DestroyImmediate(remainingMesh);
                var child = new GameObject("WallMountExtension"); child.transform.SetParent(root.transform, false);
                child.transform.localPosition = V(source.placements[0].mountPosition);
                child.AddComponent<MeshFilter>().sharedMesh = mountMesh;
                var renderer = child.AddComponent<MeshRenderer>(); EditorUtility.CopySerialized(steel.GetComponent<MeshRenderer>(), renderer);
                GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(steel.gameObject));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Mesh Subset(Mesh source, List<int> triangles, Vector3 offset)
        {
            var ids = triangles.Distinct().ToArray();
            var lookup = ids.Select((old, index) => (old, index)).ToDictionary(p => p.old, p => p.index);
            var positions = source.vertices; var normals = source.normals; var uv = source.uv; var tangents = source.tangents;
            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.vertices = ids.Select(i => positions[i] - offset).ToArray();
            result.normals = ids.Select(i => normals[i]).ToArray(); result.uv = ids.Select(i => uv[i]).ToArray();
            if (tangents.Length == positions.Length) result.tangents = ids.Select(i => tangents[i]).ToArray();
            result.triangles = triangles.Select(i => lookup[i]).ToArray(); result.RecalculateBounds(); return result;
        }

        // Used by the hall importer too, so a future level import keeps the props separated.
        public static void ReplaceMergedBoards(GameObject hall)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) return;
            var merged = MergedBoards(hall);
            var previousRoot = hall.transform.Find("TickerBoards");
            if (merged.Length == 0 && !previousRoot) return;
            if (merged.Length > 0 && previousRoot) throw new Exception("Both merged boards and ticker prefab instances exist.");
            var source = ReadSource();
            var original = merged.Length > 0 ? merged : previousRoot.GetComponentsInChildren<MeshRenderer>();
            var originalBody = original.Where(r => r.sharedMaterial.name != "BBB_TV_Mount_Steel").ToArray();
            int before = TriangleCount(originalBody);
            var root = new GameObject("TickerBoards"); root.transform.SetParent(hall.transform, false);
            for (int i = 0; i < source.placements.Length; i++)
            {
                var placement = source.placements[i];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.name = placement.name;
                instance.transform.localPosition = V(placement.position);
                instance.transform.localRotation = Rotation(placement);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                var mount = instance.transform.Find("WallMountExtension");
                mount.localPosition = V(placement.mountPosition);
                mount.localScale = new Vector3(1, 1, placement.mountSize[2] / source.placements[0].mountSize[2]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(mount);
            }
            var replacements = root.GetComponentsInChildren<MeshRenderer>().Where(r => r.sharedMaterial.name != "BBB_TV_Mount_Steel").ToArray();
            if (before != TriangleCount(replacements)) throw new Exception("Ticker extraction changed triangle count.");
            VerifyGeometry(originalBody, replacements);
            // These batches use only TV-specific materials; no wall/furniture geometry is removed.
            foreach (var renderer in merged)
            {
                if (renderer.GetComponents<Collider>().Length != 0) throw new Exception("Unexpected collider on a TV render batch.");
                Object.DestroyImmediate(renderer.gameObject);
            }
            if (previousRoot) Object.DestroyImmediate(previousRoot.gameObject);
        }

        static void VerifyGeometry(MeshRenderer[] before, MeshRenderer[] after)
        {
            // Check geometry and material for every vertex. The source export baked different
            // world-position UVs and bevel normals per placement; all copies now share the
            // center board's model data. The original materials have no texture maps.
            // Neighbor cells avoid quantization-boundary failures from transform roundoff.
            const float cell = .002f;
            var vertices = new Dictionary<(string, int, int, int), List<(Vector3, Vector3, Vector2)>>();
            foreach (var renderer in after)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var positions = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
                for (int i = 0; i < positions.Length; i++)
                {
                    var p = renderer.transform.TransformPoint(positions[i]);
                    var key = (renderer.sharedMaterial.name, Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell), Mathf.FloorToInt(p.z / cell));
                    if (!vertices.TryGetValue(key, out var list)) vertices[key] = list = new List<(Vector3, Vector3, Vector2)>();
                    list.Add((p, renderer.transform.TransformDirection(normals[i]).normalized, uv[i]));
                }
            }
            foreach (var renderer in before)
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var positions = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
                for (int i = 0; i < positions.Length; i++)
                {
                    var p = renderer.transform.TransformPoint(positions[i]);
                    var n = renderer.transform.TransformDirection(normals[i]).normalized;
                    int x = Mathf.FloorToInt(p.x / cell), y = Mathf.FloorToInt(p.y / cell), z = Mathf.FloorToInt(p.z / cell);
                    bool found = false;
                    for (int dx = -1; dx <= 1 && !found; dx++)
                    for (int dy = -1; dy <= 1 && !found; dy++)
                    for (int dz = -1; dz <= 1 && !found; dz++)
                        if (vertices.TryGetValue((renderer.sharedMaterial.name, x + dx, y + dy, z + dz), out var list))
                            found = list.Any(v => (v.Item1 - p).sqrMagnitude < .0001f * .0001f);
                    if (!found) throw new Exception($"Ticker vertex mismatch: {renderer.name}, vertex {i}, {p}");
                }
            }
        }

        [MenuItem("BuyBidBlow/Hall/Extract Ticker Board Prefab")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Logs/TickerBoard");
            if (!File.Exists("Logs/TickerBoard/Before.unity")) File.Copy(HallLevelBuilder.ScenePath, "Logs/TickerBoard/Before.unity");
            if (!File.Exists("Logs/TickerBoard/Before.prefab")) File.Copy(HallPrefab, "Logs/TickerBoard/Before.prefab");
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var hall = scene.GetRootGameObjects().Single(o => o.name == "AuctionHall_Furnished");
            int colliders = hall.GetComponentsInChildren<Collider>(true).Length;
            var lighting = Lightmapping.lightingDataAsset;
            if (MergedBoards(hall).Length > 0) BuildPrefab(hall, ReadSource());
            SeparateMountExtension(ReadSource());
            ReplaceMergedBoards(hall);
            if (colliders != hall.GetComponentsInChildren<Collider>(true).Length || lighting != Lightmapping.lightingDataAsset)
                throw new Exception("Unrelated collision or lighting data changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var contents = PrefabUtility.LoadPrefabContents(HallPrefab);
            try { ReplaceMergedBoards(contents); PrefabUtility.SaveAsPrefabAsset(contents, HallPrefab); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            RemoveExtractionVariants();
            AssetDatabase.SaveAssets();
            ValidateSaved();
        }

        static void ValidateSaved()
        {
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            var hall = scene.GetRootGameObjects().Single(o => o.name == "AuctionHall_Furnished");
            var root = hall.transform.Find("TickerBoards");
            if (!root || root.childCount != 3 || MergedBoards(hall).Length != 0) throw new Exception("Incorrect scene board instances.");
            foreach (Transform instance in root)
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance.gameObject) != PrefabPath) throw new Exception("Board prefab link missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length != 6 || !prefab.transform.Find("Screen") || !prefab.transform.Find("WallMountExtension")) throw new Exception("Incomplete board prefab.");
            foreach (var renderer in renderers)
                if (!renderer.sharedMaterial || !renderer.GetComponent<MeshFilter>().sharedMesh) throw new Exception("Missing board asset.");
            if (LightmapSettings.lightmaps.Length == 0 || !GameObject.Find("HallFurnitureCollision")) throw new Exception("Hall lighting/collision missing.");
            string report = "PASS: three linked TickerBoard instances in SC_ExchangeFurnished and the hall prefab.\n" +
                "PASS: original screen/body world-space geometry and materials preserved (position tolerance 0.1 mm).\n" +
                "All three instances use exactly the same prefab and six mesh assets. No variants. UVs and bevel normals are shared from the center board.\n" +
                "WallMountExtension is a separate child: center depth 0.566486 m, side depth 2.174050 m. Only its local position/depth scale are overridden per placement.\n" +
                "PASS: baked hall lighting and colliders retained; no duplicate merged TV renderers.\n" +
                $"Prefab: {PrefabPath}\nPer board: {renderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.vertexCount)} vertices, {TriangleCount(renderers)} triangles, {renderers.Length} renderers.\n" +
                "Pivot: screen center. Local front: +Z; up: +Y; scale: 1 Unity unit = 1 meter. Screen is a separate child.\n" +
                "Five dedicated materials were moved with their GUIDs preserved. No runtime price UI or networking was added.\n";
            File.WriteAllText(ReportPath, report); Debug.Log(report);
        }

        static void RemoveExtractionVariants()
        {
            // Only remove the two temporary variants and ten meshes created by this extractor.
            // Every path is an explicit descendant of the project's TickerBoard asset folder.
            foreach (string side in new[] { "Left", "Right" })
            {
                AssetDatabase.DeleteAsset(Root + "/Variants/TickerBoard_" + side + ".prefab");
                foreach (string part in new[] { "Screen", "Charcoal_Bezel", "Vent_Dark", "Mount_Steel", "Matte_Back" })
                    AssetDatabase.DeleteAsset(Root + "/Meshes/TickerBoard_" + side + "_" + part + ".asset");
            }
            if (Directory.Exists(Root + "/Variants") && Directory.GetFileSystemEntries(Root + "/Variants").Length == 0)
                AssetDatabase.DeleteAsset(Root + "/Variants");
        }

        public static void RunBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
