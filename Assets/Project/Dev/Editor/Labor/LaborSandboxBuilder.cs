using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.Labor;
using BuyBidBlow.Systems.Labor;
using BuyBidBlow.Systems.Types;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    // 노동 Sandbox 도구.
    // 1) Create and Open Sandbox: SC_ExchangeFurnished를 복사한 씬을 만든다(원본·Build Settings 미변경).
    // 2) Build Work Site: 열린 Sandbox 씬에 일터(콘크리트 건물·자재 더미·하역장)를 다시 만든다.
    // 3) Set Selected Door As Entrance: 선택한 홀의 문을 일터 입구로 지정한다.
    public static class LaborSandboxBuilder
    {
        public const string Parent = "Assets/Project/Dev/Sandbox";
        public const string Root = Parent + "/Labor";
        public const string ScenePath = Root + "/LaborSandbox.unity";
        public const string SettingsPath = Root + "/LaborSettings.json";
        const string Source = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string FontPath = "Assets/Font/public/static/Pretendard-Regular.otf";
        const string SiteName = "Labor Work Site", SystemName = "Labor System", EntranceName = "Labor Entrance";

        [MenuItem("Tools/Buy Bid Blow/Labor/Create and Open Sandbox")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(Parent, "Labor");
            if (!AssetDatabase.CopyAsset(Source, ScenePath)) throw new Exception("Could not copy " + Source);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
            var config = new SerializedObject(manager);
            Set(config, "minPlayers", p => p.intValue = 1);       // 혼자서도 경기 시작
            Set(config, "prepareSeconds", p => p.floatValue = 2); // 시작 카운트다운 단축
            config.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("LABOR SANDBOX CREATED: " + ScenePath);
        }

        [MenuItem("Tools/Buy Bid Blow/Labor/Build Work Site")]
        public static void BuildWorkSite()
        {
            if (!InSandbox()) return;
            var old = GameObject.Find(SiteName);
            if (old) Undo.DestroyObjectImmediate(old);
            var site = new GameObject(SiteName);
            site.transform.position = LaborLayout.Origin;
            Undo.RegisterCreatedObjectUndo(site, "Build Labor Work Site");
            new SiteBuilder(site.transform, LoadSettings(), LoadFont()).Build();
            EnsureSystem();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = site;
            Debug.Log($"LABOR WORK SITE BUILT at {LaborLayout.Origin}. 다음: 홀의 문을 선택하고 Set Selected Door As Entrance.");
        }

        [MenuItem("Tools/Buy Bid Blow/Labor/Set Selected Door As Entrance")]
        public static void SetEntrance()
        {
            if (!InSandbox()) return;
            var door = Selection.activeGameObject;
            if (!door) { Dialog("Hierarchy나 Scene에서 일터로 연결할 문 오브젝트를 먼저 선택하세요."); return; }
            if (!TryGetBounds(door, out var bounds)) { Dialog("선택한 오브젝트에 Renderer나 Collider가 없어 크기를 알 수 없습니다."); return; }
            if (bounds.size.y > 6 || Mathf.Max(bounds.size.x, bounds.size.z) > 6)
                Debug.LogWarning("[Labor] 선택한 오브젝트가 문 한 짝보다 커 보입니다. 입구 판정이 넓어질 수 있습니다.", door);

            // 문 두께 방향(수평에서 더 얇은 축)을 법선으로 삼고, 씬 뷰 카메라가 있는 쪽(홀 쪽)으로 향하게 한다.
            bool thinX = bounds.size.x < bounds.size.z;
            var normal = thinX ? Vector3.right : Vector3.forward;
            var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera.transform.position : bounds.center - normal;
            if (Vector3.Dot(view - bounds.center, normal) < 0) normal = -normal;
            float thickness = thinX ? bounds.size.x : bounds.size.z;
            float width = thinX ? bounds.size.z : bounds.size.x;

            var old = GameObject.Find(EntranceName);
            if (old) Undo.DestroyObjectImmediate(old);
            var entrance = new GameObject(EntranceName);
            Undo.RegisterCreatedObjectUndo(entrance, "Set Labor Entrance");
            entrance.transform.SetPositionAndRotation(bounds.center + normal * (thickness / 2 + .05f), Quaternion.LookRotation(normal, Vector3.up));
            var box = entrance.AddComponent<BoxCollider>();
            box.size = new Vector3(Mathf.Max(width, .8f), Mathf.Max(bounds.size.y, 1.8f), .1f);
            entrance.AddComponent<LaborDoor>().kind = LaborDoor.Kind.ToWorkSite;

            var back = new GameObject("Hall Return");
            back.transform.SetParent(entrance.transform, false);
            var floor = new Vector3(entrance.transform.position.x, bounds.min.y + .1f, entrance.transform.position.z);
            back.transform.SetPositionAndRotation(floor + normal * 1.5f, Quaternion.LookRotation(normal, Vector3.up));

            var font = LoadFont();
            SiteBuilder.Text(entrance.transform, new Vector3(0, bounds.size.y / 2 + .45f, 0), "일터 WORK\n[E]", .03f, new Color(1f, .85f, .35f), font);

            var system = EnsureSystem();
            Undo.RecordObject(system, "Set Labor Entrance");
            system.hallReturn = back.transform;
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = entrance;
            Debug.Log($"LABOR ENTRANCE SET on '{door.name}'. 홀 복귀 지점: {back.transform.position}", entrance);
        }

        static bool InSandbox()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) return true;
            Dialog("LaborSandbox 씬에서만 실행할 수 있습니다.\nTools > Buy Bid Blow > Labor > Create and Open Sandbox로 먼저 여세요.");
            return false;
        }

        static void Dialog(string message) => EditorUtility.DisplayDialog("Labor Sandbox", message, "확인");

        static bool TryGetBounds(GameObject target, out Bounds bounds)
        {
            var renderers = target.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            var all = renderers.Length > 0 ? renderers.Select(r => r.bounds) : target.GetComponentsInChildren<Collider>().Select(c => c.bounds);
            bounds = default;
            bool any = false;
            foreach (var b in all) { if (!any) bounds = b; else bounds.Encapsulate(b); any = true; }
            return any;
        }

        static LaborSettings LoadSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SettingsPath);
            var settings = asset ? JsonUtility.FromJson<LaborSettings>(asset.text) : new LaborSettings();
            settings.Validate();
            return settings;
        }

        static Font LoadFont() => AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        static LaborSite EnsureSystem()
        {
            var system = UnityEngine.Object.FindAnyObjectByType<LaborSite>(FindObjectsInactive.Include);
            if (!system)
            {
                var go = new GameObject(SystemName);
                Undo.RegisterCreatedObjectUndo(go, "Create Labor System");
                system = go.AddComponent<LaborSite>();
            }
            system.settingsJson = AssetDatabase.LoadAssetAtPath<TextAsset>(SettingsPath);
            system.font = LoadFont();
            EditorUtility.SetDirty(system);
            return system;
        }

        static void Set(SerializedObject target, string name, Action<SerializedProperty> apply)
        {
            var property = target.FindProperty(name);
            if (property == null) { Debug.LogWarning($"[LaborSandbox] GameNetworkManager.{name} not found; left unchanged."); return; }
            apply(property);
        }

        // 일터 지오메트리. 모든 좌표는 일터 루트 기준(로컬)이다.
        // 건물: x -8..8, z -3..15, 층고 4m(2F=4, 3F=8, 4F=12), 지붕 16.
        // 경사로(폭 3m): 동쪽(x 8..11) 1→2층, 3→4층 / 서쪽(x -11..-8) 2→3층. 모두 z 0→12 방향으로 오른다.
        // 층마다 반대편 경사로로 건너가야 하므로 높은 층일수록 이동 거리가 길다.
        sealed class SiteBuilder
        {
            const int GroundLayer = 6; // PlayerMove.groundLayer
            const float H = LaborLayout.FloorHeight;
            readonly Transform root;
            readonly LaborSettings settings;
            readonly Font font;
            Material concrete, concreteDark, dirt, safety, pallet, door, marker;

            public SiteBuilder(Transform root, LaborSettings settings, Font font)
            {
                this.root = root; this.settings = settings; this.font = font;
            }

            public void Build()
            {
                if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
                concrete = Mat("Concrete", new Color(.62f, .62f, .6f));
                concreteDark = Mat("ConcreteDark", new Color(.42f, .42f, .41f));
                dirt = Mat("Dirt", new Color(.42f, .36f, .28f));
                safety = Mat("Safety", new Color(.95f, .75f, .1f));
                pallet = Mat("Pallet", new Color(.6f, .45f, .28f));
                door = Mat("Door", new Color(.22f, .28f, .34f));
                marker = Mat("DropMarker", new Color(.2f, .7f, .35f));

                Yard();
                Building();
                foreach (Company c in Enum.GetValues(typeof(Company))) Pile(c);
                for (int f = LaborSettings.FirstFloor; f <= LaborLayout.TopFloor; f++) Drop(f);
                ExitDoor();
                Lights();
            }

            void Yard()
            {
                Box("Ground", new Vector3(0, -.25f, 0), new Vector3(70, .5f, 70), dirt, true);
                Box("Fence N", new Vector3(0, 2, 35), new Vector3(70, 4, .4f), concreteDark, false);
                Box("Fence S", new Vector3(0, 2, -35), new Vector3(70, 4, .4f), concreteDark, false);
                Box("Fence E", new Vector3(35, 2, 0), new Vector3(.4f, 4, 70), concreteDark, false);
                Box("Fence W", new Vector3(-35, 2, 0), new Vector3(.4f, 4, 70), concreteDark, false);
                string pay = string.Join("  ", Enumerable.Range(LaborSettings.FirstFloor, LaborLayout.TopFloor - 1)
                    .Select(f => $"{f}층 ${settings.FloorPay[f - LaborSettings.FirstFloor]}"));
                Text(root, new Vector3(0, 4.2f, -16),
                    $"공사장 일당\n{pay}\n볏짚 ×{settings.MaterialPayPercent[0] / 100f:0.0#}  나무 ×{settings.MaterialPayPercent[1] / 100f:0.0#}  벽돌 ×{settings.MaterialPayPercent[2] / 100f:0.0#}\n경사로는 오른쪽(동쪽)에서 시작",
                    .05f, Color.white, font);
            }

            void Building()
            {
                var building = Group("Concrete Building");
                Box("1F Pad", new Vector3(0, .05f, 6), new Vector3(16, .1f, 18), concrete, true, building);
                for (int f = 2; f <= LaborLayout.TopFloor; f++)
                {
                    float y = (f - 1) * H;
                    Box($"{f}F Slab", new Vector3(0, y - .15f, 6), new Vector3(16, .3f, 18), concrete, true, building);
                    Text(building, new Vector3(0, y + 3f, -3.2f), $"{f}F", .08f, new Color(1f, .85f, .35f), font);
                }
                Box("Roof", new Vector3(0, 4 * H - .15f, 6), new Vector3(16.6f, .3f, 18.6f), concreteDark, false, building);
                foreach (var x in new[] { -7.7f, 7.7f })
                    foreach (var z in new[] { -2.7f, 6f, 14.7f })
                        Box("Column", new Vector3(x, 2 * H, z), new Vector3(.4f, 4 * H, .4f), concreteDark, false, building);
                foreach (var z in new[] { -2.7f, 14.7f })
                    Box("Column", new Vector3(0, 2 * H, z), new Vector3(.4f, 4 * H, .4f), concreteDark, false, building);

                // 층 가장자리 난간. 경사로와 이어지는 구간만 비운다.
                var east = new Dictionary<int, (float, float)[]>
                { { 2, new[] { (12f, 15f) } }, { 3, new[] { (-3f, 0f) } }, { 4, new[] { (12f, 15f) } } };
                var west = new Dictionary<int, (float, float)[]>
                { { 2, new[] { (-3f, 0f) } }, { 3, new[] { (12f, 15f) } }, { 4, new (float, float)[0] } };
                for (int f = 2; f <= LaborLayout.TopFloor; f++)
                {
                    float y = (f - 1) * H;
                    Box("Rail Front", new Vector3(0, y + .5f, -2.925f), new Vector3(16, 1, .15f), safety, false, building);
                    Box("Rail Back", new Vector3(0, y + .5f, 14.925f), new Vector3(16, 1, .15f), safety, false, building);
                    RailAlongZ(building, 7.925f, y, east[f]);
                    RailAlongZ(building, -7.925f, y, west[f]);
                }

                var ramps = Group("Ramps");
                Ramp(ramps, 9.5f, 1);
                Ramp(ramps, -9.5f, 2);
                Ramp(ramps, 9.5f, 3);
            }

            void RailAlongZ(Transform parent, float x, float y, (float z0, float z1)[] gaps)
            {
                float start = -3f;
                foreach (var gap in gaps.OrderBy(g => g.z0))
                {
                    if (gap.z0 > start) Rail(start, gap.z0);
                    start = Mathf.Max(start, gap.z1);
                }
                if (start < 15f) Rail(start, 15f);
                void Rail(float z0, float z1) =>
                    Box("Rail Side", new Vector3(x, y + .5f, (z0 + z1) / 2), new Vector3(.15f, 1, z1 - z0), safety, false, parent);
            }

            // fromFloor층에서 한 층 위로, z 0→12 방향으로 오르는 경사로와 양 끝 층계참.
            void Ramp(Transform parent, float x, int fromFloor)
            {
                float h0 = (fromFloor - 1) * H, run = 12f;
                float angle = Mathf.Atan2(H, run) * Mathf.Rad2Deg, length = Mathf.Sqrt(run * run + H * H);
                var rotation = Quaternion.Euler(-angle, 0, 0);
                var normal = rotation * Vector3.up;
                var mid = new Vector3(x, h0 + H / 2, run / 2);
                float outer = x + Mathf.Sign(x) * 1.575f;
                Box($"Ramp {fromFloor}F-{fromFloor + 1}F", mid - normal * .15f, new Vector3(3, .3f, length), concrete, true, parent, rotation);
                Box("Ramp Rail", new Vector3(outer, mid.y, mid.z) + normal * .5f, new Vector3(.15f, 1, length), safety, false, parent, rotation);
                if (fromFloor > 1) Landing(parent, x, h0, -1.5f, -3f);   // 아래쪽 입구
                Landing(parent, x, h0 + H, 13.5f, 15f);                     // 위쪽 출구
            }

            void Landing(Transform parent, float x, float y, float z, float endZ)
            {
                float outer = x + Mathf.Sign(x) * 1.575f;
                Box("Landing", new Vector3(x, y - .15f, z), new Vector3(3, .3f, 3), concrete, true, parent);
                Box("Landing Rail", new Vector3(outer, y + .5f, z), new Vector3(.15f, 1, 3), safety, false, parent);
                Box("Landing Rail", new Vector3(x, y + .5f, endZ), new Vector3(3, 1, .15f), safety, false, parent);
            }

            void Pile(Company material)
            {
                var pile = new GameObject("Pile " + material);
                pile.transform.SetParent(root, false);
                pile.transform.localPosition = LaborLayout.PileLocal(material);
                pile.AddComponent<LaborPile>().material = material;
                var collider = pile.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, .75f, 0);
                collider.size = new Vector3(3, 1.5f, 2.2f);
                var color = Mat(material.ToString(), LaborSite.LoadColor(material));
                switch (material)
                {
                    case Company.Straw:
                        for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++)
                            Visual(pile.transform, PrimitiveType.Cube, new Vector3((i - 1) * .95f, .3f + j * .6f, (j - .5f) * .2f), new Vector3(.9f, .58f, 1.8f), color);
                        break;
                    case Company.Timber:
                        for (int row = 0, count = 4; count > 0; row++, count--)
                            for (int i = 0; i < count; i++)
                                Visual(pile.transform, PrimitiveType.Cylinder, new Vector3(0, .18f + row * .32f, (i - (count - 1) / 2f) * .38f), new Vector3(.36f, 1.3f, .36f), color, Quaternion.Euler(0, 0, 90));
                        break;
                    default:
                        Visual(pile.transform, PrimitiveType.Cube, new Vector3(0, .07f, 0), new Vector3(2.6f, .14f, 1.8f), pallet);
                        for (int layer = 0; layer < 4; layer++) for (int i = 0; i < 3; i++)
                            Visual(pile.transform, PrimitiveType.Cube, new Vector3((i - 1) * .82f, .27f + layer * .27f, 0), new Vector3(.78f, .25f, 1.6f), color);
                        break;
                }
                Text(pile.transform, new Vector3(0, 2.4f, 0),
                    $"{LaborBoard.Name(material)}\n속도 {settings.CarrySpeed[(int)material] * 100:0}% · 보상 ×{settings.MaterialPayPercent[(int)material] / 100f:0.0#}",
                    .035f, Color.white, font);
            }

            void Drop(int floor)
            {
                var zone = new GameObject($"Drop {floor}F");
                zone.transform.SetParent(root, false);
                zone.transform.localPosition = LaborLayout.DropLocal(floor);
                zone.AddComponent<LaborDropZone>().floor = floor;
                var collider = zone.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, .6f, 0);
                collider.size = new Vector3(2.4f, 1.2f, 2.4f);
                Visual(zone.transform, PrimitiveType.Cube, new Vector3(0, .01f, 0), new Vector3(3.4f, .02f, 3.4f), marker);
                Visual(zone.transform, PrimitiveType.Cube, new Vector3(0, .075f, 0), new Vector3(2.4f, .15f, 2.4f), pallet);
                Visual(zone.transform, PrimitiveType.Cube, new Vector3(0, .6f, 0), new Vector3(1.6f, .9f, 1.6f), safety);
                int low = settings.Pay(floor, Company.Straw), high = settings.Pay(floor, Company.Brick);
                Text(zone.transform, new Vector3(0, 2.3f, 0), $"{floor}층 하역장\n${low} ~ ${high}", .04f, new Color(.6f, 1f, .7f), font);
            }

            void ExitDoor()
            {
                var exit = new GameObject("Exit To Hall");
                exit.transform.SetParent(root, false);
                exit.transform.localPosition = LaborLayout.ExitDoorLocal;
                Box("Back Wall", new Vector3(0, 2, -.35f), new Vector3(16, 4, .4f), concreteDark, false, exit.transform);
                Box("Post", new Vector3(-1.15f, 1.3f, 0), new Vector3(.3f, 2.6f, .3f), concreteDark, false, exit.transform);
                Box("Post", new Vector3(1.15f, 1.3f, 0), new Vector3(.3f, 2.6f, .3f), concreteDark, false, exit.transform);
                Box("Lintel", new Vector3(0, 2.75f, 0), new Vector3(2.6f, .3f, .3f), concreteDark, false, exit.transform);
                Visual(exit.transform, PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(2f, 2.4f, .1f), door);
                var collider = exit.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, 1.2f, .1f);
                collider.size = new Vector3(2f, 2.4f, .2f);
                exit.AddComponent<LaborDoor>().kind = LaborDoor.Kind.ToHall;
                Text(exit.transform, new Vector3(0, 3.4f, .2f), "홀로 돌아가기 [E]", .035f, new Color(1f, .85f, .35f), font);
            }

            void Lights()
            {
                var lights = Group("Lights");
                PointLight(lights, new Vector3(0, 10, -14), 34, 5);
                for (int f = 1; f <= LaborLayout.TopFloor; f++) PointLight(lights, new Vector3(0, (f - 1) * H + 3.3f, 6), 14, 3);
                PointLight(lights, new Vector3(13, 7, 6), 18, 3);
                PointLight(lights, new Vector3(-13, 7, 6), 18, 3);
            }

            void PointLight(Transform parent, Vector3 position, float range, float intensity)
            {
                var go = new GameObject("Point Light");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = range;
                light.intensity = intensity;
                light.color = new Color(1f, .95f, .85f);
                light.shadows = LightShadows.None;
                light.lightmapBakeType = LightmapBakeType.Realtime;
            }

            Transform Group(string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                return go.transform;
            }

            // 충돌하는 구조물. ground면 PlayerMove가 착지로 인식하는 Ground 레이어.
            GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool ground, Transform parent = null, Quaternion? rotation = null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent ? parent : root, false);
                go.transform.localPosition = position;
                go.transform.localRotation = rotation ?? Quaternion.identity;
                go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = material;
                go.layer = ground ? GroundLayer : 0;
                return go;
            }

            // 보이기만 하는 장식. 콜라이더를 지워 상호작용 판정은 부모 콜라이더가 맡는다.
            static void Visual(Transform parent, PrimitiveType type, Vector3 position, Vector3 size, Material material, Quaternion? rotation = null)
            {
                var go = GameObject.CreatePrimitive(type);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
                go.transform.localRotation = rotation ?? Quaternion.identity;
                go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = material;
            }

            public static void Text(Transform parent, Vector3 position, string text, float size, Color color, Font font)
            {
                var go = new GameObject("Sign");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
                var mesh = go.AddComponent<TextMesh>();
                mesh.text = text;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.fontSize = 64;
                mesh.characterSize = size;
                mesh.color = color;
                if (font)
                {
                    mesh.font = font;
                    go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
                go.AddComponent<LaborBillboard>();
            }

            static Material Mat(string name, Color color)
            {
                string path = Root + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material) { material.color = color; EditorUtility.SetDirty(material); return material; }
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                AssetDatabase.CreateAsset(material, path);
                return material;
            }
        }
    }
}
