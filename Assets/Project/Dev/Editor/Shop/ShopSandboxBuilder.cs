using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using BuyBidBlow.Sandbox.Shop;
using BuyBidBlow.Systems.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev.Editor
{
    // 상점 Sandbox 도구.
    // 1) Create and Open Sandbox: SC_ExchangeFurnished를 복사한 씬을 만든다(원본·Build Settings 미변경).
    // 2) Build Shop On Selected: 선택한 바닥(2층 원형 장판) 위에 아치형 상점을 만든다. 만든 뒤 루트를 옮기거나 돌려도 된다.
    public static class ShopSandboxBuilder
    {
        public const string Parent = "Assets/Project/Dev/Sandbox";
        public const string Root = Parent + "/Shop";
        public const string ScenePath = Root + "/ShopSandbox.unity";
        public const string SettingsPath = Root + "/ShopSettings.json";
        const string Source = "Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string FontPath = "Assets/Font/public/static/Pretendard-Regular.otf";
        const string ShopName = "Shop Arch", SystemName = "Shop System";

        [MenuItem("Tools/Buy Bid Blow/Shop/Create and Open Sandbox")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder(Parent, "Shop");
            if (!AssetDatabase.CopyAsset(Source, ScenePath)) throw new Exception("Could not copy " + Source);

            var scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
            var config = new SerializedObject(manager);
            Set(config, "minPlayers", p => p.intValue = 1);       // 혼자서도 경기 시작
            Set(config, "prepareSeconds", p => p.floatValue = 2); // 시작 카운트다운 단축
            config.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("SHOP SANDBOX CREATED: " + ScenePath);
        }

        [MenuItem("Tools/Buy Bid Blow/Shop/Build Shop On Selected")]
        public static void BuildShop()
        {
            if (!InSandbox()) return;
            var floor = Selection.activeGameObject;
            if (!floor) { Dialog("Hierarchy나 Scene에서 상점을 올릴 바닥(2층 원형 장판)을 먼저 선택하세요."); return; }
            if (!TryGetBounds(floor, out var bounds)) { Dialog("선택한 오브젝트에 Renderer나 Collider가 없어 위치를 알 수 없습니다."); return; }
            if (Mathf.Max(bounds.size.x, bounds.size.z) > 20)
                Debug.LogWarning("[Shop] 선택한 오브젝트가 장판보다 커 보입니다. 위치가 어긋나면 'Shop Arch' 루트를 직접 옮기세요.", floor);

            // 앞면(손님이 서는 쪽)은 씬 뷰 카메라가 있는 방향.
            var position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera.transform.position : position - Vector3.forward;
            var front = view - position;
            front.y = 0;
            if (front.sqrMagnitude < .01f) front = Vector3.back;

            var old = GameObject.Find(ShopName);
            if (old) Undo.DestroyObjectImmediate(old);
            var shop = new GameObject(ShopName);
            Undo.RegisterCreatedObjectUndo(shop, "Build Shop");
            shop.transform.SetPositionAndRotation(position, Quaternion.LookRotation(front.normalized, Vector3.up));
            var counter = new ArchBuilder(shop.transform, LoadFont()).Build();

            var system = EnsureSystem();
            Undo.RecordObject(system, "Build Shop");
            system.counter = counter;
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = shop;
            Debug.Log($"SHOP BUILT on '{floor.name}' at {position}. 위치·방향은 'Shop Arch' 루트를 옮겨 조정하세요.", shop);
        }

        static bool InSandbox()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) return true;
            Dialog("ShopSandbox 씬에서만 실행할 수 있습니다.\nTools > Buy Bid Blow > Shop > Create and Open Sandbox로 먼저 여세요.");
            return false;
        }

        static void Dialog(string message) => EditorUtility.DisplayDialog("Shop Sandbox", message, "확인");

        static bool TryGetBounds(GameObject target, out Bounds bounds)
        {
            var renderers = target.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            var all = renderers.Length > 0 ? renderers.Select(r => r.bounds) : target.GetComponentsInChildren<Collider>().Select(c => c.bounds);
            bounds = default;
            bool any = false;
            foreach (var b in all) { if (!any) bounds = b; else bounds.Encapsulate(b); any = true; }
            return any;
        }

        static Font LoadFont() => AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        static ShopSite EnsureSystem()
        {
            var system = UnityEngine.Object.FindAnyObjectByType<ShopSite>(FindObjectsInactive.Include);
            if (!system)
            {
                var go = new GameObject(SystemName);
                Undo.RegisterCreatedObjectUndo(go, "Create Shop System");
                system = go.AddComponent<ShopSite>();
            }
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(SettingsPath);
            if (json) JsonUtility.FromJson<ShopSettings>(json.text).Validate();
            else Debug.LogWarning("[Shop] " + SettingsPath + " not found; built-in defaults are used.");
            system.settingsJson = json;
            system.font = LoadFont();
            EditorUtility.SetDirty(system);
            return system;
        }

        static void Set(SerializedObject target, string name, Action<SerializedProperty> apply)
        {
            var property = target.FindProperty(name);
            if (property == null) { Debug.LogWarning($"[ShopSandbox] GameNetworkManager.{name} not found; left unchanged."); return; }
            apply(property);
        }

        // 아치형 상점. 좌표는 루트 기준(로컬)이며 +z가 손님 쪽이다.
        // 기둥 x ±1.9, 높이 2.6, 그 위로 반지름 1.9 반원 아치(꼭대기 4.5). 판매대는 아치 안, 진열장은 판매대 뒤.
        sealed class ArchBuilder
        {
            const float HalfWidth = 1.9f, PillarHeight = 2.6f;
            readonly Transform root;
            readonly Font font;
            Material wood, dark, gold, cloth;

            public ArchBuilder(Transform root, Font font) { this.root = root; this.font = font; }

            public Transform Build()
            {
                if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
                wood = Mat("ShopWood", new Color(.55f, .33f, .17f));
                dark = Mat("ShopDarkWood", new Color(.3f, .18f, .1f));
                gold = Mat("ShopGold", new Color(.85f, .68f, .25f));
                cloth = Mat("ShopCloth", new Color(.55f, .1f, .12f));

                foreach (var x in new[] { -HalfWidth, HalfWidth })
                {
                    Box("Pillar", new Vector3(x, PillarHeight / 2, 0), new Vector3(.4f, PillarHeight, .4f), wood, true);
                    Box("Pillar Base", new Vector3(x, .1f, 0), new Vector3(.55f, .2f, .55f), gold, false);
                }
                const int segments = 11;
                float segment = Mathf.PI * HalfWidth / segments * 1.12f;
                for (int i = 0; i < segments; i++)
                {
                    float angle = Mathf.PI * (i + .5f) / segments;
                    var at = new Vector3(Mathf.Cos(angle) * HalfWidth, PillarHeight + Mathf.Sin(angle) * HalfWidth, 0);
                    Box("Arch", at, new Vector3(segment, .38f, .42f), i % 2 == 0 ? wood : gold, false, Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90));
                }

                var counter = Box("Shop Counter", new Vector3(0, .5f, .15f), new Vector3(2.8f, 1f, .7f), dark, true);
                counter.AddComponent<ShopCounter>();
                Box("Counter Top", new Vector3(0, 1.03f, .15f), new Vector3(3f, .06f, .85f), gold, false);
                Box("Counter Cloth", new Vector3(0, .55f, .51f), new Vector3(2.2f, .7f, .02f), cloth, false);

                Box("Shelf", new Vector3(0, 1.2f, -1.1f), new Vector3(3.2f, 2.4f, .4f), dark, true);
                for (int row = 0; row < 3; row++)
                    Box("Shelf Board", new Vector3(0, .6f + row * .65f, -.88f), new Vector3(3f, .05f, .1f), gold, false);

                Text(new Vector3(0, PillarHeight + HalfWidth + .55f, 0), "상점 SHOP", .05f, new Color(1f, .85f, .35f));
                Text(new Vector3(0, 1.5f, .5f), "[E] 상점 · [I] 아이템", .022f, Color.white);
                return counter.transform;
            }

            GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool collide, Quaternion? rotation = null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(root, false);
                go.transform.localPosition = position;
                go.transform.localRotation = rotation ?? Quaternion.identity;
                go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = material;
                if (!collide) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                return go;
            }

            void Text(Vector3 position, string text, float size, Color color)
            {
                var go = new GameObject("Sign");
                go.transform.SetParent(root, false);
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
                go.AddComponent<ShopBillboard>();
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
