using System;
using System.Linq;
using BuyBidBlow.UI.HUD;
using UnityEditor;
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
                var iconTemplate = icons.arraySize > 0 ? (Image)icons.GetArrayElementAtIndex(0).objectReferenceValue : null;
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
                    var icon = iconTransform ? iconTransform.GetComponent<Image>()
                        : iconTemplate ? Object.Instantiate(iconTemplate, root) : NewIcon(root);
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
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
                // The wolf (local body and avatars alike) plays the new emotes from its own Animator.
                WolfAnimatorBuilder.EnsureEmoteStates();
                AssetDatabase.SaveAssets();
                Debug.Log("Eight emotion menu and wolf emote states saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }

        static Image NewIcon(Transform root)
        {
            var go = new GameObject("Emotion Icon", typeof(RectTransform), typeof(Image)) { layer = root.gameObject.layer };
            go.transform.SetParent(root, false);
            return go.GetComponent<Image>();
        }
    }
}
