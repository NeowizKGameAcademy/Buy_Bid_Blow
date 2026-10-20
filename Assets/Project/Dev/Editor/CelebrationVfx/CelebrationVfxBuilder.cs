using System.IO;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev
{
    public static class CelebrationVfxBuilder
    {
        private const string Root = "Assets/Project/Content/Props/CelebrationVfx";
        private const string Demo = "Assets/Project/Dev/Sandbox/CelebrationVfx/CelebrationVfxDemo.unity";
        private static readonly Color Gold = new Color(1f, .72f, .18f);
        private static readonly Color Mint = new Color(.22f, 1f, .73f);
        private static readonly Color Coral = new Color(1f, .28f, .38f);
        private static Material spark, paper, bill, coin;
        private static Mesh rectangle, star, disc;

        [MenuItem("Tools/BuyBidBlow/VFX/Build Celebration Pack")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Path.GetDirectoryName(Demo));
            AssetDatabase.Refresh();
            spark = MakeMaterial("Spark", MakeTexture("Spark", false), Color.white, true);
            paper = MakeMaterial("Confetti", null, Color.white, false);
            bill = MakeMaterial("Banknote", MakeTexture("Banknote", true), Color.white, false);
            coin = MakeMaterial("Coin", null, Gold, false);
            rectangle = MakeMesh("Rectangle", 4, false, true);
            star = MakeMesh("Star", 10, true, false);
            disc = MakeMesh("CoinDisc", 24, false, false);
            SaveEffect("Fireworks", 0);
            SaveEffect("Fanfare", 1);
            SaveEffect("Money", 2);
            BuildDemo();
            AssetDatabase.SaveAssets();
            Debug.Log("Celebration VFX: built three prefabs and demo scene.");
        }

        [MenuItem("Tools/BuyBidBlow/VFX/Open Demo")]
        private static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Demo)) Build();
            EditorSceneManager.OpenScene(Demo);
        }

        private static void SaveEffect(string name, int kind)
        {
            var root = new GameObject(name);
            root.AddComponent<CelebrationVfx>();
            if (kind == 0)
            {
                Color[] colors = { Gold, Mint, Coral };
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = new Vector3((i - 1) * 1.35f, 2.8f + (i % 2) * 1.2f, 0);
                    var p = Emitter(root, "Bloom " + i, pos, 110, 2.1f, 3.3f, .085f, colors[i], spark, null, .25f + i * .5f);
                    var shape = p.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .08f;
                    var velocity = p.velocityOverLifetime; velocity.enabled = true;
                    velocity.speedModifier = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .12f));
                    var main = p.main; main.gravityModifier = .12f;
                    var trails = p.trails; trails.enabled = true; trails.ratio = .65f;
                    trails.lifetime = .18f; trails.dieWithParticles = true;
                    trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
                    p.GetComponent<ParticleSystemRenderer>().trailMaterial = spark;
                    Emitter(root, "Flash " + i, pos, 1, .3f, 0, 1.4f, colors[i], spark, null, .25f + i * .5f);
                }
            }
            else if (kind == 1)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var p = Emitter(root, "Confetti " + side, new Vector3(side * 1.3f, .1f, 0), 85, 3.5f, 6f, .13f, Gold, paper, rectangle);
                    p.transform.rotation = Quaternion.Euler(-90, side * -22, 0);
                    DecoratePaper(p, .48f, true);
                    var stars = Emitter(root, "Stars " + side, new Vector3(side, .15f, 0), 18, 2.5f, 5, .23f, Gold, paper, star, .1f);
                    stars.transform.rotation = p.transform.rotation;
                    DecoratePaper(stars, .4f, false);
                }
                Emitter(root, "Golden flash", Vector3.up, 1, .45f, 0, 2.8f, Gold, spark, null);
            }
            else
            {
                var notes = Emitter(root, "Flying banknotes", Vector3.up * .3f, 65, 4.7f, 6, .46f, Color.white, bill, rectangle);
                notes.transform.rotation = Quaternion.Euler(-90, 0, 0);
                DecoratePaper(notes, .3f, false);
                var coins = Emitter(root, "Spinning coins", Vector3.up * .3f, 38, 3.3f, 5.5f, .18f, Color.white, coin, disc, .12f);
                coins.transform.rotation = notes.transform.rotation;
                DecoratePaper(coins, .5f, false);
                var glints = Emitter(root, "Money glints", Vector3.up, 30, 2.8f, 2, .11f, Gold, spark, null, .2f);
                var shape = glints.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static ParticleSystem Emitter(GameObject root, string name, Vector3 position, int count,
            float lifetime, float speed, float size, Color color, Material material, Mesh mesh, float delay = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.loop = false; main.playOnAwake = false; main.duration = .15f;
            main.startDelay = delay; main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .75f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .65f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .7f, size);
            main.startColor = color; main.maxParticles = count + 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = p.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var shape = p.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 27; shape.radius = .2f;
            var fade = p.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .05f), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.maxParticleSize = 1;
            if (mesh != null) { renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh; }
            return p;
        }

        private static void DecoratePaper(ParticleSystem p, float gravity, bool colorful)
        {
            var main = p.main; main.gravityModifier = gravity; main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startRotationY = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            if (colorful)
            {
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Gold, 0), new GradientColorKey(Mint, .5f), new GradientColorKey(Coral, 1) },
                    new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            }
            var rotation = p.rotationOverLifetime; rotation.enabled = true; rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-3, 3); rotation.y = new ParticleSystem.MinMaxCurve(-2, 2);
            rotation.z = new ParticleSystem.MinMaxCurve(-4, 4);
            var noise = p.noise; noise.enabled = true; noise.strength = .45f; noise.frequency = .65f; noise.scrollSpeed = .6f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        private static Texture2D MakeTexture(string name, bool banknote)
        {
            const int width = 128, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + .5f) / width, v = (y + .5f) / height;
                    Color c;
                    if (!banknote)
                    {
                        float radius = new Vector2((u - .5f) * 2, (v - .5f) * 2).magnitude;
                        c = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 2));
                    }
                    else
                    {
                        c = new Color(.59f, .91f, .67f);
                        bool border = x < 5 || x > 122 || y < 5 || y > 58;
                        bool frame = ((x == 10 || x == 117) && y > 9 && y < 54) || ((y == 10 || y == 53) && x > 9 && x < 118);
                        float oval = Mathf.Pow((x - 64) / 19f, 2) + Mathf.Pow((y - 32) / 24f, 2);
                        if (border || frame || (oval > .8f && oval < 1)) c = new Color(.08f, .35f, .24f);
                        // Stylized B currency mark, readable on either side of the two-sided mesh.
                        bool mark = (x >= 56 && x <= 60 && y >= 18 && y <= 46)
                            || (x >= 60 && x <= 70 && (Mathf.Abs(y - 18) < 2 || Mathf.Abs(y - 32) < 2 || Mathf.Abs(y - 46) < 2))
                            || (x >= 70 && x <= 74 && ((y > 19 && y < 31) || (y > 33 && y < 45)));
                        bool lines = (x > 16 && x < 39 || x > 89 && x < 111) && y > 20 && y < 44 && y % 6 < 2;
                        if (mark || lines) c = new Color(.08f, .35f, .24f);
                    }
                    texture.SetPixel(x, y, c);
                }
            texture.Apply();
            string path = Root + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material MakeMaterial(string name, Texture texture, Color color, bool additive)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color); material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh MakeMesh(string name, int segments, bool pointed, bool rectangular)
        {
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[vertices.Length]; uv[0] = Vector2.one * .5f;
            var indices = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2 * i / segments + Mathf.PI * .5f;
                float radius = pointed && i % 2 == 1 ? .22f : .5f;
                Vector3 v = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (rectangular) v = new Vector3(i == 0 || i == 3 ? -.65f : .65f, i < 2 ? -.32f : .32f);
                vertices[i + 1] = v;
                uv[i + 1] = rectangular ? new Vector2(v.x / 1.3f + .5f, v.y / .64f + .5f) : (Vector2)v + Vector2.one * .5f;
                indices[i * 3] = 0; indices[i * 3 + 1] = i + 1; indices[i * 3 + 2] = (i + 1) % segments + 1;
            }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Root + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        private static void BuildDemo()
        {
            // Batch Unity starts with an unsaved, untitled scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode || (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                    ? NewSceneMode.Single : NewSceneMode.Additive);
            var cameraObject = new GameObject("Preview Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 4.5f, -18);
            cameraObject.transform.LookAt(new Vector3(0, 2, 0));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.022f, .035f, .065f);
            camera.orthographic = true; camera.orthographicSize = 5.8f;
            var effects = new CelebrationVfx[3];
            string[] names = { "Fireworks", "Fanfare", "Money" };
            for (int i = 0; i < 3; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + names[i] + ".prefab"), scene);
                instance.transform.position = new Vector3((i - 1) * 6, 0, 0);
                effects[i] = instance.GetComponent<CelebrationVfx>();
            }
            var controller = new GameObject("Demo Controls", typeof(CelebrationVfxDemo));
            SceneManager.MoveGameObjectToScene(controller, scene);
            controller.GetComponent<CelebrationVfxDemo>().Configure(effects);
            EditorSceneManager.SaveScene(scene, Demo);
            EditorSceneManager.CloseScene(scene, true);
        }

        public static void BuildAndVerify()
        {
            Build();
            Verify();
        }

        public static void Verify()
        {
            var scene = EditorSceneManager.OpenScene(Demo);
            foreach (var effect in Object.FindObjectsByType<CelebrationVfx>(FindObjectsSortMode.None))
            {
                effect.Play();
                int count = 0;
                foreach (var p in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    p.Simulate(1.2f, false, true);
                    count += p.particleCount;
                    var renderer = p.GetComponent<ParticleSystemRenderer>();
                    if (!renderer.sharedMaterial || !renderer.sharedMaterial.shader.isSupported)
                        throw new System.Exception("Missing/unsupported material: " + p.name);
                }
                if (count == 0) throw new System.Exception("No particles: " + effect.name);
                effect.Stop();
                foreach (var p in effect.GetComponentsInChildren<ParticleSystem>())
                    if (p.particleCount != 0) throw new System.Exception("Stop failed: " + effect.name);
                effect.Play();
                foreach (var p in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    p.Simulate(8, false, true);
                    // Editor simulation pauses the system; check exhausted particles and finite emission.
                    if (p.particleCount != 0 || p.main.loop)
                        throw new System.Exception("Effect does not finish: " + p.name + ", particles=" + p.particleCount);
                    p.Simulate(1.2f, false, true);
                }
                Debug.Log("VFX verified: " + effect.name + ", visible particles=" + count);
            }
            var camera = Camera.main;
            var target = new RenderTexture(1600, 900, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes("Library/CelebrationVfxPreview.png", image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            Debug.Log("Celebration VFX verification PASS.");
        }
    }
}
