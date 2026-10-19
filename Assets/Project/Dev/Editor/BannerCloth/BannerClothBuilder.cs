using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BuyBidBlow.Dev.BannerCloth;

namespace BuyBidBlow.Dev.Editor
{
    public static class BannerClothBuilder
    {
        const string Root = "Assets/Project/Content/Props/ChampionshipBanner";
        public const string ScenePath = "Assets/Project/Dev/Sandbox/BannerCloth/SC_BannerCloth.unity";

        [MenuItem("BuyBidBlow/Banner Cloth/Build Test Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/ChampionshipBanner.fbx");
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAllAssetsAtPath(Root + "/ChampionshipBanner.fbx").OfType<Mesh>().Single();
            // Bake the imported axis conversion into a standalone Unity mesh.
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/ChampionshipBanner.fbx");
            var mf = model.GetComponentInChildren<MeshFilter>();
            var matrix = mf.transform.localToWorldMatrix;
            var mesh = UnityEngine.Object.Instantiate(imported);
            mesh.name = "ChampionshipBanner_ClothMesh";
            mesh.vertices = mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            // FBX axis conversion can mirror X: map the print in Unity's front view.
            mesh.uv = mesh.vertices.Select(v => new Vector2(
                Mathf.InverseLerp(mesh.bounds.min.x, mesh.bounds.max.x, v.x),
                Mathf.InverseLerp(mesh.bounds.min.y, mesh.bounds.max.y, v.y))).ToArray();
            if (mesh.normals.Sum(n => n.z) > 0)
            {
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                mesh.triangles = triangles;
            }
            mesh.RecalculateNormals(); mesh.RecalculateTangents();
            if (Mathf.Abs(mesh.bounds.size.x - 6) > .05f || Mathf.Abs(mesh.bounds.size.y - 2) > .05f)
                throw new Exception("Unexpected FBX dimensions: " + mesh.bounds.size);
            SaveAsset(mesh, Root + "/ChampionshipBanner_Mesh.asset");
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/ChampionshipBanner_Mesh.asset");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "ChampionshipBanner_Fabric";
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/ChampionshipBanner.png"));
            material.SetFloat("_Cull", 0); material.SetFloat("_Smoothness", .08f);
            SaveAsset(material, Root + "/ChampionshipBanner_Fabric.mat");
            material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/ChampionshipBanner_Fabric.mat");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var banner = new GameObject("ChampionshipBanner");
            var renderer = banner.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh; renderer.sharedMaterial = material;
            renderer.updateWhenOffscreen = true;
            renderer.localBounds = new Bounds(new Vector3(0, -1, 0), new Vector3(10, 8, 8));
            var cloth = banner.AddComponent<Cloth>();
            cloth.useGravity = true; cloth.stretchingStiffness = 1; cloth.bendingStiffness = .08f;
            cloth.damping = .25f; cloth.friction = .4f; cloth.clothSolverFrequency = 120;
            cloth.useTethers = true; cloth.enableContinuousCollision = true;
            cloth.useVirtualParticles = 1; cloth.sleepThreshold = 0;
            var vertices = cloth.vertices;
            var bounds = mesh.bounds;
            var coefficients = new ClothSkinningCoefficient[vertices.Length];
            int pins = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                bool pin = vertices[i].y > bounds.max.y - .025f && Mathf.Abs(vertices[i].x) > 2.85f;
                coefficients[i].maxDistance = pin ? 0 : .7f + (bounds.max.y - vertices[i].y) * .4f;
                coefficients[i].collisionSphereDistance = 0;
                if (pin) pins++;
            }
            if (pins != 4) throw new Exception("Expected four pinned vertices, got " + pins);
            cloth.coefficients = coefficients;
            PrefabUtility.SaveAsPrefabAsset(banner, Root + "/ChampionshipBanner.prefab");
            banner.transform.position = new Vector3(0, 3.2f, 0);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "ContactCapsule"; capsule.transform.position = new Vector3(0, 1.6f, -1.2f);
            var collider = capsule.GetComponent<CapsuleCollider>();
            cloth.capsuleColliders = new[] { collider };
            var demo = new GameObject("ClothTestControls").AddComponent<BannerClothDemo>();
            demo.banner = cloth; demo.contact = collider;
            for (int side = -1; side <= 1; side += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Support"; post.transform.position = new Vector3(side * 3.1f, 1.65f, 0);
                post.transform.localScale = new Vector3(.12f, 3.3f, .12f);
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Floor";
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(4, 3.3f, -9);
            camera.transform.LookAt(new Vector3(0, 1.9f, 0)); camera.fieldOfView = 45;
            camera.backgroundColor = new Color(.075f, .09f, .11f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Key Light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 2; light.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientLight = new Color(.45f, .45f, .45f);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("BANNER BUILD OK: " + mesh.vertexCount + " vertices; pins=" + pins);
        }

        static void SaveAsset(UnityEngine.Object asset, string path)
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing) { EditorUtility.CopySerialized(asset, existing); UnityEngine.Object.DestroyImmediate(asset); }
            else AssetDatabase.CreateAsset(asset, path);
        }
    }
}
