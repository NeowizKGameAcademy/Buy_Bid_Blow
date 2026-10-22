using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // Black pig auctioneer standing behind the mezzanine rostrum, facing the auction chairs.
    public static class HallAuctioneer
    {
        const string Folder = "Assets/Project/Characters/NPC/Pig/Auctioneer";
        const string Source = "Assets/Project/Dev/Sandbox/Pigs/Models/Pig_Black_50k@Dwarf Idle.fbx";
        const string Name = "Auctioneer Pig";
        const string Rostrum = "01_Auction_Rostrum_ROOT";
        // Tall enough for the shoulders to clear the 1.18 m rostrum.
        const float Height = 2.2f, Behind = .35f;

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        // World bounds of a collision piece from its mesh (Collider.bounds is empty in prefab contents).
        static Bounds MeshBounds(Transform t)
        {
            var mesh = t.GetComponent<MeshCollider>().sharedMesh.bounds;
            return new Bounds(t.TransformPoint(mesh.center), Vector3.Scale(mesh.size, t.lossyScale));
        }

        static GameObject MakePrefab()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            string clipPath = Folder + "/Black_DwarfIdle.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (!clip) { clip = UnityEngine.Object.Instantiate(AssetDatabase.LoadAllAssetsAtPath(Source).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"))); AssetDatabase.CreateAsset(clip, clipPath); }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
            string controllerPath = Folder + "/Auctioneer.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) { controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath); var state = controller.layers[0].stateMachine.AddState("Dwarf Idle"); state.motion = clip; controller.layers[0].stateMachine.defaultState = state; }
            string avatarPath = Folder + "/Black_Avatar.asset";
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
            if (!avatar) { avatar = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source).GetComponent<Animator>().avatar); AssetDatabase.CreateAsset(avatar, avatarPath); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Characters/NPC/Pig/PF_Pig_Black_Model.prefab"));
            try
            {
                var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.avatar = avatar; animator.applyRootMotion = false;
                clip.SampleAnimation(go, 0);
                return PrefabUtility.SaveAsPrefabAsset(go, Folder + "/PF_Auctioneer.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        static void Place(GameObject hall, GameObject prefab)
        {
            var old = hall.transform.Find(Name); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var rostrum = MeshBounds(hall.GetComponentsInChildren<Transform>(true).Single(t => t.name == Rostrum));
            var chairs = hall.GetComponentsInChildren<Seat>(true).Where(s => s.auction).ToArray();
            // Face the auction chairs; they sit toward +Z of the rostrum if none are tagged.
            var audience = chairs.Length > 0 ? chairs.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / chairs.Length : rostrum.center + Vector3.forward;
            var toAudience = audience - rostrum.center; toAudience.y = 0; toAudience.Normalize();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hall.transform);
            go.name = Name;
            go.transform.rotation = Quaternion.LookRotation(toAudience);
            go.transform.localScale *= Height / BoundsOf(go).size.y;
            var bounds = BoundsOf(go);
            var target = rostrum.center - toAudience * (rostrum.extents.z + Behind);
            target.y = rostrum.min.y;
            go.transform.position += target - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Debug.Log($"AUCTIONEER at {go.transform.position} facing {toAudience} · {chairs.Length} auction chair(s)");
        }

        static void Run()
        {
            var prefab = MakePrefab();
            AuctioneerAnimationSetup.Apply();
            string path = HallLevelBuilder.Root + "/AuctionHall_Furnished.prefab";
            var hall = PrefabUtility.LoadPrefabContents(path);
            try { Place(hall, prefab); PrefabUtility.SaveAsPrefabAsset(hall, path); } finally { PrefabUtility.UnloadPrefabContents(hall); }
            var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
            hall = scene.GetRootGameObjects().Single(g => g.name == "AuctionHall_Furnished");
            Place(hall, prefab);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        [MenuItem("BuyBidBlow/Hall/Place Auctioneer Pig")]
        static void Menu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Run(); }

        // Batch: -executeMethod BuyBidBlow.Dev.Editor.HallAuctioneer.RunBatch, writes Logs/Auctioneer.png from the auction chairs.
        public static void RunBatch()
        {
            try
            {
                Run();
                var pig = GameObject.Find(Name).transform;
                var cameraGo = new GameObject("Auctioneer Preview"); var camera = cameraGo.AddComponent<Camera>();
                camera.transform.position = pig.position + pig.forward * 7 + Vector3.up * 2.8f; camera.transform.LookAt(pig.position + Vector3.up * 1.1f); camera.fieldOfView = 50;
                var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply(); File.WriteAllBytes("Logs/Auctioneer.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(cameraGo);
                File.WriteAllText("Logs/Auctioneer.result", "PASS");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { File.WriteAllText("Logs/Auctioneer.result", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
