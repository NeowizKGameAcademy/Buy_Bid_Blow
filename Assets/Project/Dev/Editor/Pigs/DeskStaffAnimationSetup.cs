using System;
using System.Linq;
using BuyBidBlow.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class DeskStaffAnimationSetup
    {
        const string Root = "Assets/Project/Content/Characters/Pig/StandingStaff/";
        [MenuItem("Tools/Buy Bid Blow/Pigs/Connect Desk Staff Animations")]
        public static void Apply()
        {
            Configure("Yellow", "Thankful");
            Configure("Green", "Talking (1)");
            Configure("Red", "Quick Formal Bow");
            AssetDatabase.SaveAssets();
        }

        static void Configure(string color, string action)
        {
            string path = Root + "PF_Staff_" + color + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var source = AssetDatabase.LoadAllAssetsAtPath("Assets/Project/Dev/Sandbox/Pigs/Models/Pig_" + color + "_50k@" + action + ".fbx")
                    .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                var missing = AnimationUtility.GetCurveBindings(source).Where(b => b.type == typeof(Transform) && !string.IsNullOrEmpty(b.path) && !go.transform.Find(b.path)).ToArray();
                if (missing.Length > 0) throw new InvalidOperationException(color + " greeting has incompatible bone paths.");
                string clipPath = Root + color + "_Greeting.anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (!clip) { clip = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(clip, clipPath); }
                else EditorUtility.CopySerialized(source, clip);
                clip.name = color + "_Greeting";
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                var animator = go.GetComponent<Animator>();
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                var machine = controller.layers[0].stateMachine;
                var idle = machine.states.Single(s => s.state.name == "Standing Idle").state;
                var state = machine.states.FirstOrDefault(s => s.state.name == "Greeting").state;
                if (!state) state = machine.AddState("Greeting");
                state.motion = clip;
                if (!state.transitions.Any(t => t.destinationState == idle))
                {
                    var back = state.AddTransition(idle);
                    back.hasExitTime = true; back.exitTime = 1;
                    back.hasFixedDuration = true; back.duration = .12f;
                }
                machine.defaultState = idle;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var client = go.GetComponent<DeskStaffAnimationClient>();
                if (!client) client = go.AddComponent<DeskStaffAnimationClient>();
                client.animator = animator; client.greeting = clip;
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
    }
}
