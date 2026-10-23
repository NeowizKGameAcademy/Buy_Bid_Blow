using System;
using System.Linq;
using BuyBidBlow.UI.HUD;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class EmotionMenuSetup
    {
        const string PlayerPath = "Assets/Project/Characters/Player/Player.prefab";
        const string AnimationRoot = "Assets/Project/Characters/Player/Wolf/Animation/";
        static readonly string[] Names =
            { "Frustration", "Windmill", "Wave", "JoyDance", "GolfDrive", "JumpingJacks", "LayingPose", "HurricaneKick" };

        [MenuItem("Tools/Buy Bid Blow/UI/Build Eight Emotion Menu")]
        public static void Build()
        {
            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var radial = player.GetComponentInChildren<RadialMenuController>(true);
                var serialized = new SerializedObject(radial);
                var root = ((GameObject)serialized.FindProperty("menuRoot").objectReferenceValue).transform;
                var slices = serialized.FindProperty("menu");
                var icons = serialized.FindProperty("emotionIcons");
                var template = (Image)slices.GetArrayElementAtIndex(0).objectReferenceValue;
                var iconTemplate = (Image)icons.GetArrayElementAtIndex(0).objectReferenceValue;
                var materialPath = "Assets/Project/UI/HUD/Radial Menu Eight.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(template.material);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.SetFloat("_Angle", 45f);
                EditorUtility.SetDirty(material);
                slices.arraySize = icons.arraySize = 8;
                for (var i = 0; i < 8; i++)
                {
                    var sliceTransform = root.Find($"Radial Menu {i + 1}");
                    var slice = sliceTransform ? sliceTransform.GetComponent<Image>() : Object.Instantiate(template, root);
                    slice.name = $"Radial Menu {i + 1}";
                    slice.material = material;
                    slice.raycastTarget = false;
                    slice.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f * i);
                    slice.transform.SetSiblingIndex(i);
                    slices.GetArrayElementAtIndex(i).objectReferenceValue = slice;

                    var iconName = $"Emotion Icon {i + 1:00}_{Names[i]}";
                    var iconTransform = root.Find(iconName);
                    var icon = iconTransform ? iconTransform.GetComponent<Image>() : Object.Instantiate(iconTemplate, root);
                    icon.name = iconName;
                    icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/UI/HUD/EmotionIcons/{i + 1:00}_{Names[i]}.png");
                    if (!icon.sprite) throw new Exception("Missing icon: " + iconName);
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    icon.color = Color.white;
                    icon.rectTransform.localScale = Vector3.one;
                    icon.rectTransform.localRotation = Quaternion.identity;
                    var angle = (202.5f - 45f * i) * Mathf.Deg2Rad;
                    icon.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 270f;
                    icon.rectTransform.sizeDelta = new Vector2(128f, 128f);
                    icon.transform.SetAsLastSibling();
                    icons.GetArrayElementAtIndex(i).objectReferenceValue = icon;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var animator = player.GetComponentInChildren<Animator>(true);
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                var machine = controller.layers[0].stateMachine;
                for (var i = 4; i < 8; i++)
                {
                    var clip = ImportClip(Names[i], animator.transform, i == 5 || i == 7);
                    if (!controller.parameters.Any(p => p.name == Names[i]))
                        controller.AddParameter(Names[i], AnimatorControllerParameterType.Trigger);
                    var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Player_" + Names[i])
                        ?? machine.AddState("Player_" + Names[i], new Vector3(600, i * 70));
                    state.motion = clip;
                    state.writeDefaultValues = true;
                    var transition = machine.anyStateTransitions.FirstOrDefault(t => t.destinationState == state)
                        ?? machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = 0.1f;
                    transition.canTransitionToSelf = true;
                    transition.conditions = new[] { new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = Names[i] } };
                }
                animator.applyRootMotion = false;
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Eight emotion menu, clips and Animator transitions saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }

        static AnimationClip ImportClip(string name, Transform skeleton, bool loop)
        {
            var path = AnimationRoot + "Emotions/Source/" + name + ".fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            var clip = Object.Instantiate(source);
            clip.name = "Player_" + name;
            clip.legacy = false;
            clip.hideFlags = HideFlags.None;
            var bones = skeleton.GetComponentsInChildren<Transform>(true);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform)) continue;
                if (string.IsNullOrEmpty(binding.path))
                {
                    // Never animate the Animator object's transform or move the network player.
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    continue;
                }
                if (skeleton.Find(binding.path)) continue;
                var boneName = binding.path.Split('/').Last();
                var matches = bones.Where(t => t.name == boneName).ToArray();
                if (matches.Length != 1) throw new Exception("Unmatched animation bone: " + binding.path);
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null);
                var mapped = binding;
                mapped.path = AnimationUtility.CalculateTransformPath(matches[0], skeleton);
                AnimationUtility.SetEditorCurve(clip, mapped, curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var clipPath = AnimationRoot + "Emotions/" + clip.name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (!existing) AssetDatabase.CreateAsset(clip, clipPath);
            else
            {
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                clip = existing;
            }
            return clip;
        }
    }
}
