using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuyBidBlow.Dev
{
    // Explicit, reproducible asset setup for the Wolf character.
    public static class WolfAnimatorBuilder
    {
        private const string Wolf = "Assets/Project/Characters/Wolf";
        private const string Output = Wolf + "/Animation";
        private const string ControllerPath = Output + "/WolfCharacter.controller";
        private const string PrefabPath = Wolf + "/WolfExpressions/PF_Wolf_Character.prefab";
        private const string ScenePath = "Assets/Project/Dev/Sandbox/Wolf/SC_Wolf_Character.unity";
        private static readonly string[] BodyClips = {
            "Wolf_Walk", "Wolf_RaiseArms", "Wolf_Wave", "Wolf_Sit", "Wolf_Bid",
            "Mixamo_Walking", "Mixamo_Defeat", "Mixamo_HandRaising", "Mixamo_Flair", "Mixamo_HipHopDancing"
        };
        private static readonly string[] Faces = { "Happy", "Suspicious", "Shocked", "Blank" };

        [MenuItem("Tools/Buy Bid Blow/Wolf/Build Animator and Wire Character")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isDirty)
                throw new InvalidOperationException("Save the Wolf sandbox scene before rebuilding its Animator.");
            if (!AssetDatabase.IsValidFolder(Output)) AssetDatabase.CreateFolder(Wolf, "Animation");

            var clips = BodyClips.Select(name =>
                AssetDatabase.LoadAssetAtPath<AnimationClip>(Wolf + "/WolfActions/" + name + ".anim")
                ?? throw new InvalidOperationException("Missing body clip: " + name)).ToArray();
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].legacy = false;
                var settings = AnimationUtility.GetAnimationClipSettings(clips[i]);
                settings.loopTime = i == 0 || i == 3 || i == 5 || i == 9;
                AnimationUtility.SetAnimationClipSettings(clips[i], settings);
                EditorUtility.SetDirty(clips[i]);
            }

            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var character = prefab.GetComponentInChildren<BBBWolfCharacter>(true);
                if (!character || !character.model) throw new InvalidOperationException("Prefab has no character/model.");
                var idle = CreateIdle(clips[5]);
                var faceClips = CreateFaces(character.model);

                // Rebuilding is explicit; preserve the controller GUID and replace its owned subassets.
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                {
                    controller.layers = Array.Empty<AnimatorControllerLayer>();
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
                        if (asset != controller) UnityEngine.Object.DestroyImmediate(asset, true);
                    controller.parameters = Array.Empty<AnimatorControllerParameter>();
                }
                controller.AddParameter(BBBWolfCharacter.ActionParameter, AnimatorControllerParameterType.Int);
                controller.AddParameter(BBBWolfCharacter.ExpressionParameter, AnimatorControllerParameterType.Int);
                controller.AddParameter(BBBWolfCharacter.MovementParameter, AnimatorControllerParameterType.Float);
                var parameters = controller.parameters;
                parameters[0].defaultInt = -1;
                parameters[1].defaultInt = (int)BBBWolfCharacter.Face.BlankBluff;
                controller.parameters = parameters;

                controller.AddLayer("Body");
                var body = controller.layers[0].stateMachine;
                var locomotion = body.AddState("Locomotion", new Vector3(260, 80));
                locomotion.writeDefaultValues = false;
                var blend = new BlendTree { name = "Idle Walk", blendType = BlendTreeType.Simple1D,
                    blendParameter = BBBWolfCharacter.MovementParameter, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(blend, controller);
                blend.AddChild(idle, 0);
                blend.AddChild(clips[5], 1);
                locomotion.motion = blend;
                locomotion.AddStateMachineBehaviour<BBBWolfActionState>();
                body.defaultState = locomotion;
                AddRequestTransition(body, locomotion, 0);
                for (int i = 0; i < clips.Length; i++)
                {
                    var state = body.AddState(BodyClips[i], new Vector3(520 + (i % 2) * 280, 80 + (i / 2) * 90));
                    state.motion = clips[i];
                    state.writeDefaultValues = false;
                    state.AddStateMachineBehaviour<BBBWolfActionState>().showPaddle = i == 4;
                    AddRequestTransition(body, state, i + 1);
                    bool loop = AnimationUtility.GetAnimationClipSettings(clips[i]).loopTime;
                    if (!loop)
                    {
                        var transition = state.AddTransition(locomotion);
                        transition.hasExitTime = true;
                        transition.exitTime = 1;
                        transition.hasFixedDuration = true;
                        transition.duration = .12f;
                    }
                }

                controller.AddLayer("Expression");
                var layers = controller.layers;
                layers[1].defaultWeight = 1;
                controller.layers = layers;
                var expressions = controller.layers[1].stateMachine;
                for (int i = 0; i < faceClips.Length; i++)
                {
                    var state = expressions.AddState(Faces[i], new Vector3(260, 80 + 90 * i));
                    state.motion = faceClips[i];
                    state.writeDefaultValues = false;
                    if (i == 3) expressions.defaultState = state;
                    var transition = expressions.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = 0;
                    transition.canTransitionToSelf = false;
                    transition.AddCondition(AnimatorConditionMode.Equals, i, BBBWolfCharacter.ExpressionParameter);
                }
                EditorUtility.SetDirty(controller);
                Configure(character, controller);
                // Production prefab must never contain the sandbox GUI.
                foreach (var review in prefab.GetComponentsInChildren<BBBWolfCharacterReview>(true))
                    UnityEngine.Object.DestroyImmediate(review);
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            bool opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var character in root.GetComponentsInChildren<BBBWolfCharacter>(true))
                        Configure(character, controller);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Wolf sandbox.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            AssetDatabase.SaveAssets();
            Validate();
            File.WriteAllText("Library/WolfAnimatorBuild.result", "PASS: Animator, 10 body clips, idle pose, 4 expression clips, prefab and sandbox wired.");
            Debug.Log("Wolf Animator setup complete. Use Tools > Buy Bid Blow > Wolf > Run Playback Checks.");
        }

        private static void AddRequestTransition(AnimatorStateMachine machine, AnimatorState state, int value)
        {
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = .12f;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.Equals, value, BBBWolfCharacter.ActionParameter);
        }

        private static AnimationClip CreateIdle(AnimationClip source)
        {
            var idle = new AnimationClip { name = "Wolf_Idle", frameRate = 30 };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.type != typeof(Transform)) continue;
                float value = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0);
                AnimationUtility.SetEditorCurve(idle, binding, AnimationCurve.Constant(0, 1, value));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(idle);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            return SaveClip(idle, Output + "/Wolf_Idle.anim");
        }

        private static AnimationClip[] CreateFaces(Transform model)
        {
            var objects = Faces.Select(name => Find(model, name)).ToArray();
            var clips = new AnimationClip[Faces.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = new AnimationClip { name = "Face_" + Faces[i], frameRate = 30 };
                for (int j = 0; j < objects.Length; j++)
                {
                    string path = AnimationUtility.CalculateTransformPath(objects[j], model);
                    AnimationUtility.SetEditorCurve(clip,
                        EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"),
                        AnimationCurve.Constant(0, 1, i == j ? 1 : 0));
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clips[i] = SaveClip(clip, Output + "/Face_" + Faces[i] + ".anim");
            }
            return clips;
        }

        private static AnimationClip SaveClip(AnimationClip clip, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(clip, existing);
                UnityEngine.Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Transform Find(Transform model, string name)
        {
            var matches = model.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected unique transform: " + name);
            return matches[0];
        }

        private static void Configure(BBBWolfCharacter character, AnimatorController controller)
        {
            Transform model = character.model;
            if (!model) throw new InvalidOperationException("Character model is missing.");
            var appearance = character.GetComponent<BBBWolfAppearance>();
            bool newAppearance = !appearance;
            if (newAppearance) appearance = character.gameObject.AddComponent<BBBWolfAppearance>();
            var serialized = new SerializedObject(appearance);
            SetRenderers(serialized.FindProperty("uppers"), model,
                new[] { "Wolf_Upper_Suit", "Wolf_Upper_RolledShirt", "Wolf_Upper_Vest" });
            SetRenderers(serialized.FindProperty("lowers"), model,
                new[] { "Wolf_Lower_Long", "Wolf_Lower_Shorts" });
            serialized.FindProperty("tie").objectReferenceValue = Find(model, "Wolf_Tie").GetComponent<SkinnedMeshRenderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            appearance.ApplyAppearance();

            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            character.animator = animator;
            character.appearance = appearance;
            character.paddle = Find(model, "Bid_Paddle_Prototype").gameObject;
            character.paddle.SetActive(false);
            for (int i = 0; i < Faces.Length; i++) Find(model, Faces[i]).gameObject.SetActive(i == (int)character.expression);
            EditorUtility.SetDirty(character);
            EditorUtility.SetDirty(appearance);
            EditorUtility.SetDirty(animator);
        }

        private static void SetRenderers(SerializedProperty property, Transform model, string[] names)
        {
            property.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = Find(model, names[i]).GetComponent<SkinnedMeshRenderer>();
        }

        [MenuItem("Tools/Buy Bid Blow/Wolf/Validate Animator Assets")]
        public static void Validate()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (!controller || controller.layers.Length != 2) throw new InvalidOperationException("Expected Body and Expression layers.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var character = prefab.GetComponentInChildren<BBBWolfCharacter>(true);
            if (!character.animator || !character.appearance || !character.paddle) throw new InvalidOperationException("Prefab references missing.");
            foreach (var clip in controller.animationClips.Distinct())
            {
                if (clip.legacy) throw new InvalidOperationException("Legacy clip: " + clip.name);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.path.Length > 0 && !character.model.Find(binding.path))
                        throw new InvalidOperationException("Unresolved animation binding: " + clip.name + " / " + binding.path);
            }
            Debug.Log("Wolf Animator asset validation passed.");
        }
    }
}




