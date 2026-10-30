using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Dev.Editor
{
    // Lobby room behind the hall's east "LOBBY" door. Generated from boxes with world-scaled UVs so the
    // hall's floor/wall/ceiling materials keep the hall's texel density. Not lightmapped: the approved hall
    // bake stays untouched and the room is lit by its own realtime sconces.
    public static class LobbyRoomBuilder
    {
        public const string Root = "Assets/Project/Content/Levels/LobbyRoom";
        const string PrefabPath = Root + "/LobbyRoom.prefab";
        const string Hall = "Assets/Project/Content/Levels/ExchangeFurnished";

        // Prefab origin: the outside face of the hall wall, centered on the LOBBY door. Local +X runs into the room.
        public static readonly Vector3 Origin = new Vector3(14.12f, 0, -5.4f);
        const float Depth = 13.5f, HalfWidth = 7.6f, Height = 5.5f, Wall = .24f;
        // The door frame on the hall wall spans about ±1.95 m around the origin.
        const float DoorHalfWidth = 2f;
        const float WainscotHeight = 1.1f, Panel = .05f;

        static Material floor, wall, ceiling, walnut, brass, glow, runner;

        [MenuItem("Tools/Buy Bid Blow/Build Lobby Room")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            floor = Load(Hall + "/Materials/Hall_Dark_Herringbone.mat");
            wall = Load(Hall + "/Materials/BBB_Wall_Soft_Teal_001.mat");
            ceiling = Load("Assets/Project/Content/Levels/Exchange/Materials/Hall_Ceiling.mat");
            walnut = Load(Hall + "/Materials/BBB_Column_Walnut.mat");
            brass = Load(Hall + "/Materials/BBB_Column_Brass.mat");
            glow = Load(Hall + "/Materials/A63_Wall_Sconce_ON.mat");
            runner = Load(Hall + "/Materials/A70_Royal_Runner_Woven.mat");
            Directory.CreateDirectory(Root + "/Meshes");

            var root = new GameObject("LobbyRoom");
            try
            {
                BuildShell(root.transform);
                BuildTrim(root.transform);
                BuildLights(root.transform);
                BuildFurniture(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Place();
            Debug.Log("Lobby room built: " + PrefabPath);
        }

        // Puts one instance of the prefab behind the LOBBY door in the hall scene, replacing an older one.
        static void Place()
        {
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "LobbyRoom")) UnityEngine.Object.DestroyImmediate(old);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            instance.transform.SetPositionAndRotation(Origin, Quaternion.identity);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildShell(Transform root)
        {
            float w = HalfWidth + Wall;
            // Floor and ceiling tuck .1 m under the hall wall so no seam shows at the threshold.
            Box(root, "Floor", new Vector3(-.1f, -.2f, -w), new Vector3(Depth + Wall, 0, w), floor, .167f);
            Box(root, "Ceiling", new Vector3(-.1f, Height, -w), new Vector3(Depth + Wall, Height + .2f, w), ceiling, .219f);
            Box(root, "Wall_East", new Vector3(Depth, 0, -w), new Vector3(Depth + Wall, Height, w), wall, .844f);
            Box(root, "Wall_North", new Vector3(-.2f, 0, HalfWidth), new Vector3(Depth, Height, w), wall, .844f);
            Box(root, "Wall_South", new Vector3(-.2f, 0, -w), new Vector3(Depth, Height, -HalfWidth), wall, .844f);
        }

        // Walnut wainscot with a chair rail, a cornice and pilasters, echoing the hall's wall dressing.
        static void BuildTrim(Transform root)
        {
            var trim = Group(root, "Trim");
            float rail = WainscotHeight, cap = .07f;
            void Wainscot(string name, Vector3 min, Vector3 max, Vector3 railOut)
            {
                Box(trim, name, min, max, walnut, .5f);
                var top = new Vector3(min.x, rail, min.z); var end = new Vector3(max.x, rail + cap, max.z);
                Box(trim, name + "_Rail", Vector3.Min(top, top + railOut), Vector3.Max(end, end + railOut), walnut, .5f);
            }
            Wainscot("Wainscot_North", new Vector3(0, 0, HalfWidth - Panel), new Vector3(Depth, rail, HalfWidth), new Vector3(0, 0, -.04f));
            Wainscot("Wainscot_South", new Vector3(0, 0, -HalfWidth), new Vector3(Depth, rail, -HalfWidth + Panel), new Vector3(0, 0, .04f));
            Wainscot("Wainscot_East", new Vector3(Depth - Panel, 0, -HalfWidth), new Vector3(Depth, rail, HalfWidth), new Vector3(-.04f, 0, 0));
            Wainscot("Wainscot_West_N", new Vector3(0, 0, DoorHalfWidth), new Vector3(Panel, rail, HalfWidth), new Vector3(.04f, 0, 0));
            Wainscot("Wainscot_West_S", new Vector3(0, 0, -HalfWidth), new Vector3(Panel, rail, -DoorHalfWidth), new Vector3(.04f, 0, 0));

            float c0 = Height - .3f;
            Box(trim, "Cornice_North", new Vector3(0, c0, HalfWidth - .12f), new Vector3(Depth, Height, HalfWidth), walnut, .5f);
            Box(trim, "Cornice_South", new Vector3(0, c0, -HalfWidth), new Vector3(Depth, Height, -HalfWidth + .12f), walnut, .5f);
            Box(trim, "Cornice_East", new Vector3(Depth - .12f, c0, -HalfWidth), new Vector3(Depth, Height, HalfWidth), walnut, .5f);
            Box(trim, "Cornice_West", new Vector3(0, c0, -HalfWidth), new Vector3(.12f, Height, HalfWidth), walnut, .5f);

            foreach (float x in PilasterX)
            {
                Pilaster(trim, "Pilaster_N_" + x, new Vector3(x, 0, HalfWidth), new Vector3(0, 0, -1));
                Pilaster(trim, "Pilaster_S_" + x, new Vector3(x, 0, -HalfWidth), new Vector3(0, 0, 1));
            }
            foreach (float z in PilasterZ) Pilaster(trim, "Pilaster_E_" + z, new Vector3(Depth, 0, z), new Vector3(-1, 0, 0));
        }

        static readonly float[] PilasterX = { 4.5f, 9f };
        static readonly float[] PilasterZ = { -3.8f, 3.8f };

        // A walnut pilaster standing on the wall at 'foot', protruding along 'inward', with brass base and capital bands.
        static void Pilaster(Transform parent, string name, Vector3 foot, Vector3 inward)
        {
            var side = Vector3.Cross(Vector3.up, inward).normalized;
            Vector3 Span(float halfWidth, float depth) => side * halfWidth + inward * depth;
            void Part(string part, float y0, float y1, float halfWidth, float depth, Material m)
            {
                var a = foot + Vector3.up * y0 - side * halfWidth;
                var b = foot + Vector3.up * y1 + side * halfWidth + inward * depth;
                Box(parent, name + part, Vector3.Min(a, b), Vector3.Max(a, b), m, .5f);
            }
            Part("", 0, Height - .3f, .25f, .14f, walnut);
            Part("_Base", 0, .18f, .29f, .18f, brass);
            Part("_Capital", Height - .5f, Height - .42f, .29f, .18f, brass);
        }

        // Warm wall sconces on every pilaster, as in the hall, plus two hidden ceiling fills.
        static void BuildLights(Transform root)
        {
            var lights = Group(root, "Lights");
            var warm = new Color(1f, .83f, .61f);
            var cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            void Sconce(string name, Vector3 face, Vector3 inward)
            {
                var g = Group(lights, name); g.localPosition = face + Vector3.up * 2.5f; g.localRotation = Quaternion.LookRotation(inward);
                var plate = Group(g, "Backplate"); plate.localPosition = new Vector3(0, 0, .02f); plate.localScale = new Vector3(.16f, .5f, .04f);
                plate.gameObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                plate.gameObject.AddComponent<MeshRenderer>().sharedMaterial = brass;
                var shade = Group(g, "Glow"); shade.localPosition = new Vector3(0, .05f, .12f); shade.localScale = new Vector3(.12f, .17f, .12f);
                shade.gameObject.AddComponent<MeshFilter>().sharedMesh = cylinder;
                var r = shade.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = glow; r.shadowCastingMode = ShadowCastingMode.Off;
                var light = Group(g, "Light").gameObject.AddComponent<Light>(); light.transform.localPosition = new Vector3(0, .1f, .35f);
                light.type = LightType.Point; light.lightmapBakeType = LightmapBakeType.Realtime; light.color = warm;
                light.intensity = 2.2f; light.range = 5.5f; light.shadows = LightShadows.None;
            }
            foreach (float x in PilasterX)
            {
                Sconce("Sconce_N_" + x, new Vector3(x, 0, HalfWidth - .14f), Vector3.back);
                Sconce("Sconce_S_" + x, new Vector3(x, 0, -HalfWidth + .14f), Vector3.forward);
            }
            foreach (float z in PilasterZ) Sconce("Sconce_E_" + z, new Vector3(Depth - .14f, 0, z), Vector3.left);
            foreach (float x in new[] { 3.5f, 9.5f })
            {
                // Hung low enough that the ceiling washes evenly instead of showing a hot spot.
                var fill = Group(lights, "Fill_" + x).gameObject.AddComponent<Light>(); fill.transform.localPosition = new Vector3(x, Height - 2.2f, 0);
                fill.type = LightType.Point; fill.lightmapBakeType = LightmapBakeType.Realtime; fill.color = warm;
                fill.intensity = 2.5f; fill.range = 10f; fill.shadows = LightShadows.None;
            }
        }

        static void BuildFurniture(Transform root)
        {
            var furniture = Group(root, "Furniture");
            // Runner from the door into the room; the texture's long side is V.
            var rug = Group(furniture, "Runner"); rug.localPosition = new Vector3(3.6f, .012f, 0);
            rug.gameObject.AddComponent<MeshFilter>().sharedMesh = SaveMesh("Runner", Quad(6.6f, 2.2f));
            rug.gameObject.AddComponent<MeshRenderer>().sharedMaterial = runner;
            Put(furniture, Hall + "/LoungeClusters/Prefabs/LoungeRug.prefab", new Vector3(9.7f, 0, 0), 0);
            // Corner planters stay clear of the waiting line (PlayerNet.WaitingCenter, local z ≈ +3).
            Put(furniture, Hall + "/LoungeClusters/Prefabs/LoungePlanter.prefab", new Vector3(12.3f, 0, 6.3f), 0);
            Put(furniture, Hall + "/LoungeClusters/Prefabs/LoungePlanter.prefab", new Vector3(12.3f, 0, -6.3f), 0);
            const string props = "Assets/Project/Content/Props";
            float north = HalfWidth - .3f, south = -HalfWidth + .3f;
            Put(furniture, props + "/LoungeAccessories/Prefabs/DisplayCabinet.prefab", new Vector3(6.75f, 0, north), 180);
            Put(furniture, props + "/FurnitureUtilityPack/Prefabs/Bookcase_A.prefab", new Vector3(2.25f, 0, north), 180);
            Put(furniture, props + "/FurnitureUtilityPack/Prefabs/InformationBoard_A.prefab", new Vector3(6.75f, 0, south), 0);
            Put(furniture, props + "/LoungeAccessories/Prefabs/LeafletRack.prefab", new Vector3(2.6f, 0, south), 0);
            Put(furniture, props + "/FurnitureUtilityPack/Prefabs/Table_B.prefab", new Vector3(Depth - .45f, 0, 0), 270);
            Put(furniture, props + "/LoungeAccessories/Prefabs/UmbrellaStand.prefab", new Vector3(.4f, 0, -DoorHalfWidth - .45f), 90);
        }

        static void Put(Transform parent, string path, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Missing prefab: " + path);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            g.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        }

        static Material Load(string path) => AssetDatabase.LoadAssetAtPath<Material>(path) ?? throw new InvalidOperationException("Missing material: " + path);

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }

        // An axis-aligned box in prefab space with a box collider. UVs are the face's own coordinates in
        // metres times uvPerMeter, so tiling matches the hall regardless of the piece's size.
        static void Box(Transform parent, string name, Vector3 min, Vector3 max, Material material, float uvPerMeter)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var center = (min + max) / 2; g.transform.localPosition = center;
            var size = max - min;
            g.AddComponent<MeshFilter>().sharedMesh = SaveMesh(name, BoxMesh(center, size, uvPerMeter));
            g.AddComponent<MeshRenderer>().sharedMaterial = material;
            g.AddComponent<BoxCollider>().size = size;
        }

        static Mesh BoxMesh(Vector3 center, Vector3 size, float uvPerMeter)
        {
            var h = size / 2;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                // u, v axes on the face; walls run V up the world Y axis.
                var v = Mathf.Abs(n.y) > 0 ? Vector3.forward : Vector3.up;
                // u = n × v keeps the quad clockwise seen from outside (Unity's front face).
                var u = Vector3.Cross(n, v);
                int start = vertices.Count;
                foreach (var (su, sv) in new[] { (-1, -1), (-1, 1), (1, 1), (1, -1) })
                {
                    var local = Vector3.Scale(n + u * su + v * sv, h);
                    vertices.Add(local); normals.Add(n);
                    var world = local + center;
                    uvs.Add(new Vector2(Vector3.Dot(world, u), Vector3.Dot(world, v)) * uvPerMeter);
                }
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "LobbyRoom_Box" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        // A floor quad, length along X (texture V), width along Z (texture U), facing up.
        static Mesh Quad(float length, float width)
        {
            float x = length / 2, z = width / 2;
            var mesh = new Mesh { name = "LobbyRoom_Runner" };
            mesh.vertices = new[] { new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(x, 0, z), new Vector3(-x, 0, z) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh SaveMesh(string name, Mesh mesh)
        {
            name = name.Replace('.', '_'); mesh.name = name;
            var path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
