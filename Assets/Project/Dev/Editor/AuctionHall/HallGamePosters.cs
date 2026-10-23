using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallGamePosters
    {
        const string Root = "Assets/Project/Content/Props/GamePosters";
        public static void BuildBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                Directory.CreateDirectory(Root + "/Materials");
                Directory.CreateDirectory(Root + "/Prefabs");
                AssetDatabase.Refresh();
                if (GameObject.Find("UnderMezzanine_GamePosters")) throw new Exception("Posters already exist.");
                var group = new GameObject("UnderMezzanine_GamePosters");
                Undo.RegisterCreatedObjectUndo(group, "Place game posters");
                string[] names = { "Sandguard", "FourWinds", "OrapaMagic" };
                var frame = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                frame.SetColor("_BaseColor", new Color(.12f, .075f, .035f));
                frame.SetFloat("_Metallic", .45f);
                frame.SetFloat("_Smoothness", .3f);
                AssetDatabase.CreateAsset(frame, Root + "/Materials/PosterFrame.mat");
                Physics.SyncTransforms();
                var report = "";
                for (int i = 0; i < names.Length; i++)
                {
                    string texturePath = Root + "/Textures/" + names[i] + ".png";
                    var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                    importer.wrapMode = TextureWrapMode.Clamp; importer.anisoLevel = 4;
                    importer.SaveAndReimport();
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                    material.SetFloat("_Smoothness", .12f);
                    AssetDatabase.CreateAsset(material, Root + "/Materials/" + names[i] + ".mat");
                    float x = 2.2f + i * 1.35f;
                    if (!Physics.Raycast(new Vector3(x, 2.15f, 15.7f), Vector3.forward, out var hit, 3f))
                        throw new Exception("Rear wall not found at " + x);
                    var normal = hit.normal; normal.y = 0; normal.Normalize();
                    var poster = new GameObject("Poster_" + names[i]);
                    poster.transform.SetParent(group.transform);
                    poster.transform.position = hit.point + normal * .055f;
                    poster.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
                    var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    backing.name = "ThinFrame"; backing.transform.SetParent(poster.transform, false);
                    backing.transform.localScale = new Vector3(.95f, 1.4f, .035f);
                    backing.GetComponent<Renderer>().sharedMaterial = frame;
                    UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
                    var art = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    art.name = "Artwork"; art.transform.SetParent(poster.transform, false);
                    art.transform.localPosition = new Vector3(0, 0, -.02f);
                    art.transform.localScale = new Vector3(.9f, 1.35f, 1);
                    art.GetComponent<Renderer>().sharedMaterial = material;
                    art.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    UnityEngine.Object.DestroyImmediate(art.GetComponent<Collider>());
                    PrefabUtility.SaveAsPrefabAssetAndConnect(poster, Root + "/Prefabs/" + names[i] + ".prefab", InteractionMode.AutomatedAction);
                    report += names[i] + ": " + hit.point + " wall=" + hit.collider.name + "\n";
                }
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
                var camera = new GameObject("PosterPreviewCamera").AddComponent<Camera>();
                camera.nearClipPlane = .05f;
                HallLevelVerification.Capture(camera, new Vector3(3.5f, 2.2f, 12.9f), new Vector3(3.5f, 2.15f, 17.1f), "GamePosters");
                File.WriteAllText("Logs/game-posters-result.txt", "PASS: three 2:3 framed posters saved.\n" + report);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText("Logs/game-posters-result.txt", e.ToString()); EditorApplication.Exit(1); }
        }
    }
}
