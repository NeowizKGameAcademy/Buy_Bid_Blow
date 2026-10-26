using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using BuyBidBlow.Characters;
using BuyBidBlow.Systems.Types;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallDeskStaff
    {
        const string Root = "Assets/Project/Content/Characters/Pig";
        const string GroupName = "Desk Pig Staff";
        static readonly string[] Colors = { "Yellow", "Green", "Red" };
        static readonly string[] Desks = { "STRAW", "WOOD", "BRICK" };
        static readonly Company[] Companies = { Company.Straw, Company.Timber, Company.Brick };
        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        static GameObject MakeStaff(string color)
        {
            string folder = Root + "/StandingStaff";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            string source = "Assets/Project/Dev/Sandbox/Pigs/Models/Pig_" + color + "_50k@Standing Idle.fbx";
            var sourceClip = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            string clipPath = folder + "/" + color + "_StandingIdle.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (!clip) { clip = UnityEngine.Object.Instantiate(sourceClip); AssetDatabase.CreateAsset(clip, clipPath); }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
            string controllerPath = folder + "/" + color + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) { controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath); var state = controller.layers[0].stateMachine.AddState("Standing Idle"); state.motion = clip; controller.layers[0].stateMachine.defaultState = state; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PF_Pig_" + color + "_Model.prefab"));
            try
            {
                var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                var sourceAnimator = AssetDatabase.LoadAssetAtPath<GameObject>(source).GetComponent<Animator>();
                if (sourceAnimator && sourceAnimator.avatar) {
                    string avatarPath = folder + "/" + color + "_Avatar.asset";
                    var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
                    if (!avatar) { avatar = UnityEngine.Object.Instantiate(sourceAnimator.avatar); AssetDatabase.CreateAsset(avatar, avatarPath); }
                    animator.avatar = avatar;
                }
                clip.SampleAnimation(go, 0);
                MakeTradeTarget(go, Companies[Array.IndexOf(Colors, color)]);
                return PrefabUtility.SaveAsPrefabAsset(go, folder + "/PF_Staff_" + color + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        // E on a desk clerk opens that company's trade panel: Interactable layer + body collider + NPCInfo.
        static void MakeTradeTarget(GameObject go, Company company)
        {
            go.layer = LayerMask.NameToLayer("Interactable");
            var bounds = BoundsOf(go);
            var scale = go.transform.lossyScale;
            var capsule = go.GetComponent<CapsuleCollider>(); if (!capsule) capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = go.transform.InverseTransformPoint(bounds.center);
            capsule.height = bounds.size.y / scale.y;
            capsule.radius = Mathf.Min(bounds.extents.x, bounds.extents.z) / Mathf.Max(scale.x, scale.z);
            var info = go.GetComponent<NPCInfo>(); if (!info) info = go.AddComponent<NPCInfo>();
            var so = new SerializedObject(info); so.FindProperty("company").enumValueIndex = (int)company; so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Adds the trade target to the existing staff prefabs without rebuilding or re-placing them.
        public static void AddTradeTargetsBatch()
        {
            try {
                for (int i = 0; i < Colors.Length; i++)
                {
                    string path = Root + "/StandingStaff/PF_Staff_" + Colors[i] + ".prefab";
                    var go = PrefabUtility.LoadPrefabContents(path);
                    try { MakeTradeTarget(go, Companies[i]); PrefabUtility.SaveAsPrefabAsset(go, path); } finally { PrefabUtility.UnloadPrefabContents(go); }
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/DeskStaffTrade.result", "PASS");
            } catch(Exception e) { File.WriteAllText("Logs/DeskStaffTrade.result", e.ToString()); Debug.LogException(e); }
        }
        static void Place(GameObject hall, GameObject[] staff)
        {
            var old = hall.transform.Find(GroupName); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var group = new GameObject(GroupName); group.transform.SetParent(hall.transform, false);
            for (int i = 0; i < Colors.Length; i++)
            {
                var chair = hall.GetComponentsInChildren<Transform>(true).Single(t => t.name.StartsWith("A24_" + Desks[i] + "_05_Staff_Chair") && !t.name.EndsWith("_35") && !t.name.EndsWith("_38") && !t.name.EndsWith("_39") && !t.name.EndsWith("_44") && !t.name.EndsWith("_46"));
                var seat = chair.Find("Seat_0");
                Vector3 forward = seat ? seat.forward : new Vector3(i == 0 ? .2660525f : i == 2 ? -.2660525f : 0, 0, .9639586f).normalized;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(staff[i], group.transform);
                go.name = "Staff_" + Desks[i] + "_" + Colors[i];
                go.transform.rotation = Quaternion.LookRotation(forward);
                go.transform.localScale *= 1.7f / BoundsOf(go).size.y;
                var bounds = BoundsOf(go);
                var target = chair.position + forward * .65f;
                target.y = BoundsOf(chair.gameObject).min.y;
                go.transform.position += target - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                Debug.Log("STAFF " + go.name + " bounds=" + BoundsOf(go));
            }
        }
        public static void RunBatch()
        {
            try {
                var staff = Colors.Select(MakeStaff).ToArray();
                DeskStaffAnimationSetup.Apply();
                string path = HallLevelBuilder.Root + "/AuctionHall_Furnished.prefab";
                var hall = PrefabUtility.LoadPrefabContents(path);
                try { Place(hall, staff); PrefabUtility.SaveAsPrefabAsset(hall, path); } finally { PrefabUtility.UnloadPrefabContents(hall); }
                var scene = EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                hall = scene.GetRootGameObjects().Single(g => g.name == "AuctionHall_Furnished");
                Place(hall, staff);
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                var cameraGo = new GameObject("Staff Preview"); var camera = cameraGo.AddComponent<Camera>();
                camera.transform.position = new Vector3(0, 3.3f, -7.5f); camera.transform.LookAt(new Vector3(0, 1.1f, -16)); camera.fieldOfView = 75;
                var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply(); File.WriteAllBytes("Logs/DeskStaff.png", image.EncodeToPNG());
                File.WriteAllText("Logs/DeskStaff.result", "PASS: three color-matched standing staff saved in scene and hall prefab.");
                EditorApplication.Exit(0);
            } catch(Exception e) { File.WriteAllText("Logs/DeskStaff.result", e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
