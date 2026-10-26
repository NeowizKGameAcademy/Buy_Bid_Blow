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
    // Builds the chair-seated wolf pose (a single static frame, no sit-down motion) and adds a
    // "Seated" state to the local and network wolf animators. Gameplay switches to it with Animator.Play.
    public static class WolfSeatedPoseBuilder
    {
        const string Wolf = "Assets/Project/Content/Characters/Wolf";
        public const string StateName = "Seated";
        // Hips above the character root in the seated pose; PlayerSeating.hipHeight on Player.prefab uses it.
        public const float SeatedHipHeight = .75f;
        // The local player model and the network avatar have different bone rest rotations, so each gets its own clip.
        static readonly (string prefab, string idle, string controller, string clip)[] Rigs =
        {
            ("Assets/Project/Characters/Player/Player.prefab", Wolf + "/Animation/Player_Idle.anim", Wolf + "/Animation/Player Animation.controller", Wolf + "/WolfActions/Player_Seated.anim"),
            ("Assets/Project/Characters/Player/PlayerNet.prefab", Wolf + "/Animation/Wolf_Idle.anim", Wolf + "/Animation/WolfCharacter.controller", Wolf + "/WolfActions/Wolf_Seated.anim"),
        };

        [MenuItem("Tools/BuyBidBlow/Wolf/Build Seated Pose")]
        public static void Build()
        {
            foreach (var rig in Rigs)
                AddState(AssetDatabase.LoadAssetAtPath<AnimatorController>(rig.controller), BuildClip(rig.prefab, rig.idle, rig.clip));
            AssetDatabase.SaveAssets();
        }

        public static void BuildBatch()
        {
            try { Build(); Capture(); File.WriteAllText("Logs/WolfSeated.result", "PASS"); }
            catch (Exception e) { File.WriteAllText("Logs/WolfSeated.result", e.ToString()); Debug.LogException(e); }
        }

        static AnimationClip BuildClip(string prefab, string idle, string clipPath)
        {
            var wolf = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
            try
            {
                var animator = ModelAnimator(wolf);
                AssetDatabase.LoadAssetAtPath<AnimationClip>(idle).SampleAnimation(animator.gameObject, 0);
                Pose(animator.transform);

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (!clip) { clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(clipPath) }; AssetDatabase.CreateAsset(clip, clipPath); }
                clip.ClearCurves();
                foreach (var bone in animator.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("mixamorig:")))
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, animator.transform);
                    var q = bone.localRotation;
                    Key(clip, path, "m_LocalRotation.x", q.x); Key(clip, path, "m_LocalRotation.y", q.y);
                    Key(clip, path, "m_LocalRotation.z", q.z); Key(clip, path, "m_LocalRotation.w", q.w);
                    if (bone.name == "mixamorig:Hips")
                    {
                        var p = bone.localPosition;
                        Key(clip, path, "m_LocalPosition.x", p.x); Key(clip, path, "m_LocalPosition.y", p.y); Key(clip, path, "m_LocalPosition.z", p.z);
                    }
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                return clip;
            }
            finally { UnityEngine.Object.DestroyImmediate(wolf); }
        }

        static Animator ModelAnimator(GameObject root) => root.GetComponentsInChildren<Animator>(true).First(a => a.runtimeAnimatorController);

        // From the idle pose: thighs forward, shins straight down, feet flat, hands resting on the lap.
        static void Pose(Transform root)
        {
            Transform B(string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:" + name);
            // The animator object faces the same way as the wolf model on both rigs.
            var forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            foreach (var side in new[] { "Left", "Right" })
            {
                Aim(B(side + "UpLeg"), B(side + "Leg"), forward - Vector3.up * .08f);
                Aim(B(side + "Leg"), B(side + "Foot"), Vector3.down);
                Aim(B(side + "Foot"), B(side + "ToeBase"), forward);
                Aim(B(side + "Arm"), B(side + "ForeArm"), Vector3.down + forward * .35f);
                Aim(B(side + "ForeArm"), B(side + "Hand"), forward - Vector3.up * .25f);
            }
        }

        static void Aim(Transform bone, Transform child, Vector3 direction) =>
            bone.rotation = Quaternion.FromToRotation(child.position - bone.position, direction) * bone.rotation;

        static void Key(AnimationClip clip, string path, string property, float value) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), AnimationCurve.Constant(0, 1 / 30f, value));

        static void AddState(AnimatorController controller, AnimationClip clip)
        {
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == StateName) ?? machine.AddState(StateName, new Vector3(260, -60));
            state.motion = clip;
            state.writeDefaultValues = false;
            EditorUtility.SetDirty(controller);
        }

        // Seats the local Player and a network avatar on a mezzanine chair the way PlayerSeating does,
        // and renders them for review.
        static void Capture()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity");
            var seat = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Seat>(true)).First(s => s.name == "A41_Front_Seat_2");
            var anchor = seat.sittingPoints[0];
            var log = new System.Text.StringBuilder();
            log.AppendLine($"anchor {anchor.position} rot {anchor.eulerAngles}");
            foreach (var rig in Rigs)
            {
                string label = Path.GetFileNameWithoutExtension(rig.clip);
                var root = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rig.prefab));
                float hipHeight = SeatedHipHeight;
                // The network avatar follows the local player's root (PlayerNet.FollowInMatch).
                root.transform.SetPositionAndRotation(anchor.position - Vector3.up * hipHeight, anchor.rotation * (root.GetComponent<BuyBidBlow.Characters.PlayerNet>() ? BuyBidBlow.Characters.PlayerNet.ModelYaw : Quaternion.identity));
                var animator = ModelAnimator(root);
                AssetDatabase.LoadAssetAtPath<AnimationClip>(rig.clip).SampleAnimation(animator.gameObject, 0);
                var hips = animator.GetComponentsInChildren<Transform>(true).First(t => t.name == "mixamorig:Hips");
                log.AppendLine($"{label}: animator {animator.name} hips above root {hips.position.y - root.transform.position.y:0.000} (hipHeight {hipHeight}) facing {Vector3.SignedAngle(anchor.forward, Vector3.ProjectOnPlane(animator.transform.forward, Vector3.up), Vector3.up):0}");
                var baked = Bake(root);
                root.SetActive(false);
                foreach (var t in baked.GetComponentsInChildren<Transform>().Concat(seat.GetComponentsInChildren<Transform>())) t.gameObject.layer = 31;
                Render(anchor, label);
                UnityEngine.Object.DestroyImmediate(baked); UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText("Logs/WolfSeated.txt", log.ToString());
        }

        static GameObject Bake(GameObject root)
        {
            var baked = new GameObject("Baked Wolf");
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh(); skin.BakeMesh(mesh);
                var part = new GameObject(skin.name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                part.transform.SetParent(baked.transform, true);
            }
            return baked;
        }

        // Only the seat and the wolf, so the railing and hall don't block the view.
        static void Render(Transform anchor, string label)
        {
            var cam = new GameObject("Seat Camera").AddComponent<Camera>();
            cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray;
            var rt = new RenderTexture(800, 600, 24); cam.targetTexture = rt; cam.fieldOfView = 50;
            var target = anchor.position + Vector3.up * .1f;
            foreach (var (name, offset) in new[] { ("side", anchor.right * 2.4f), ("front", anchor.forward * 2.4f) })
            {
                cam.transform.position = target + offset; cam.transform.LookAt(target);
                cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(800, 600); tex.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); tex.Apply();
                File.WriteAllBytes($"Logs/WolfSeated_{label}_{name}.png", tex.EncodeToPNG());
            }
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }
    }
}
