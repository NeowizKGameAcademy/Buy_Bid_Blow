using System.Collections;
using System.Linq;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.UI.HUD;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Tests.PlayMode
{
    public class EmotionMenuTests
    {
        const string PlayerPath = "Assets/Project/Characters/Player/Player.prefab";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Triggers =
            { "Defeat", "Flair", "Hadraise", "HipHopDance", "GolfDrive", "JumpingJacks", "LayingPose", "HurricaneKick" };

        [Test]
        public void PrefabContainsEightAlignedSlicesAndIcons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var radial = prefab.GetComponentInChildren<RadialMenuController>(true);
            var slices = (Image[])typeof(RadialMenuController).GetField("menu", Private).GetValue(radial);
            var icons = (Image[])typeof(RadialMenuController).GetField("emotionIcons", Private).GetValue(radial);
            Assert.AreEqual(8, slices.Length);
            Assert.AreEqual(8, icons.Length);
            for (var i = 0; i < 8; i++)
            {
                Assert.IsNotNull(slices[i]);
                Assert.IsNotNull(icons[i].sprite);
                Assert.IsTrue(icons[i].sprite.name.StartsWith($"{i + 1:00}_"));
                Assert.AreEqual(45f, slices[i].material.GetFloat("_Angle"));
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 0, -45 * i), slices[i].transform.localRotation), .01f);
                Assert.AreEqual(i, radial.GetSelectedIndexFromLocalPoint(icons[i].rectTransform.anchoredPosition));
                Assert.IsFalse(icons[i].raycastTarget);
            }
            Assert.AreEqual(-1, radial.GetSelectedIndexFromLocalPoint(Vector2.zero));
            Assert.AreEqual(-1, radial.GetSelectedIndexFromLocalPoint(Vector2.right * 159));
            // Sweep the full circle, including both sides of each boundary and beyond the ring.
            for (var degrees = 0.25f; degrees < 360; degrees += .5f)
            {
                var point = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                var index = radial.GetSelectedIndexFromLocalPoint(point * 270);
                Assert.That(index, Is.InRange(0, 7));
                Assert.AreEqual(index, radial.GetSelectedIndexFromLocalPoint(point * 1500));
                var center = icons[index].rectTransform.anchoredPosition.normalized;
                Assert.LessOrEqual(Vector2.Angle(center, point), 22.51f);
            }
        }

        [UnityTest]
        public IEnumerator OpenConfirmCancelAndReopenAllEight()
        {
            var prefabRadial = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath).GetComponentInChildren<RadialMenuController>(true);
            var sourceRoot = (GameObject)typeof(RadialMenuController).GetField("menuRoot", Private).GetValue(prefabRadial);
            var host = new GameObject("Emotion test canvas", typeof(RectTransform), typeof(Canvas));
            host.SetActive(false);
            var root = Object.Instantiate(sourceRoot, host.transform);
            root.name = "Emotion Menu";
            var radial = host.AddComponent<RadialMenuController>();
            var icons = root.GetComponentsInChildren<Image>(true).Where(i => i.name.StartsWith("Emotion Icon")).ToArray();
            typeof(RadialMenuController).GetField("emotionIcons", Private).SetValue(radial, icons);
            host.SetActive(true);
            try
            {
                Assert.IsFalse(radial.IsOpen);
                Assert.IsFalse(root.activeSelf);
                var confirmed = 0;
                radial.SelectionConfirmed += n => confirmed = n;
                for (var i = 0; i < 8; i++)
                {
                    radial.BeginSelection();
                    Assert.IsTrue(root.activeSelf);
                    Assert.AreEqual(0f, icons[i].color.a);
                    Assert.AreNotSame(radial.GetItemMaterial(1), radial.GetItemMaterial(8));
                    yield return null;
                    typeof(RadialMenuController).GetField("selectedIndex", Private).SetValue(radial, i);
                    radial.EndSelection(true);
                    Assert.AreEqual(i + 1, confirmed);
                    Assert.IsFalse(root.activeSelf);
                }
                radial.BeginSelection();
                typeof(RadialMenuController).GetField("openedAt", Private).SetValue(radial, Time.unscaledTime - 2f);
                typeof(RadialMenuController).GetField("hoveredIndex", Private).SetValue(radial, 7);
                typeof(RadialMenuController).GetMethod("AnimateRadius", Private).Invoke(radial, null);
                Assert.AreEqual(1f, icons[7].color.a, "Last slice must finish revealing.");
                Assert.Greater(icons[7].transform.localScale.x, 1f, "Hovered icon must enlarge.");
                SaveMenuPreview(host.GetComponent<Canvas>());
                typeof(RadialMenuController).GetField("selectedIndex", Private).SetValue(radial, 4);
                radial.EndSelection(false);
                Assert.AreEqual(8, confirmed, "Cancellation must not fire another selection.");
                radial.BeginSelection();
                radial.EndSelection(true);
                Assert.AreEqual(8, confirmed, "Center/no selection must not fire.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        static void SaveMenuPreview(Canvas canvas)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            var cameraObject = new GameObject("Emotion preview camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1024, 1024, 24);
            var pixels = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f, .065f, .085f);
                camera.orthographic = true;
                camera.orthographicSize = 512;
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                pixels.Apply();
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.WriteAllBytes("Logs/EmotionMenuPreview.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                canvas.worldCamera = null;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(pixels);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [UnityTest]
        public IEnumerator AllEightAnimatorTriggersPlayAndMovementCancels()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var source = player.GetComponentInChildren<Animator>(true);
            var model = Object.Instantiate(source.gameObject);
            var inputHost = new GameObject("Emotion input test");
            inputHost.SetActive(false);
            var input = inputHost.AddComponent<PlayerAnimation>();
            var playerCamera = inputHost.AddComponent<PlayerCamera>();
            var cameraObject = new GameObject("Emotion camera", typeof(Camera));
            cameraObject.transform.SetParent(inputHost.transform, false);
            var viewCamera = cameraObject.GetComponent<Camera>();
            viewCamera.cullingMask = ~(1 << 30);
            var originalPosition = new Vector3(0, 1.7f, 0);
            var originalRotation = Quaternion.Euler(15, 0, 0);
            cameraObject.transform.localPosition = originalPosition;
            cameraObject.transform.localRotation = originalRotation;
            typeof(PlayerCamera).GetField("cameraObject", Private).SetValue(playerCamera, cameraObject);
            try
            {
                var animator = model.GetComponent<Animator>();
                typeof(PlayerAnimation).GetField("animator", Private).SetValue(input, animator);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                var machine = controller.layers[0].stateMachine;
                yield return null;
                foreach (var trigger in Triggers)
                {
                    var transition = machine.anyStateTransitions.Single(t => t.conditions.Any(c => c.parameter == trigger));
                    var clip = (AnimationClip)transition.destinationState.motion;
                    Assert.Greater(clip.length, 0);
                    Assert.IsFalse(clip.legacy);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(Transform)))
                        Assert.IsTrue(binding.path == "" || model.transform.Find(binding.path), trigger + ": " + binding.path);
                    input.PlayEmotion(System.Array.IndexOf(Triggers, trigger) + 1);
                    Assert.IsTrue(playerCamera.IsEmotionView);
                    Assert.Less(cameraObject.transform.localPosition.z, -1f);
                    Assert.AreNotEqual(0, viewCamera.cullingMask & (1 << 30), "Own avatar must be visible.");
                    // Selecting a different emotion must not overwrite the original first-person pose.
                    playerCamera.SetEmotionView(true);
                    animator.Update(.01f);
                    animator.Update(.2f);
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(transition.destinationState.name), trigger);
                    animator.Update(.1f);
                    Assert.Greater(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 0f);
                    if (trigger == "HurricaneKick")
                    {
                        Assert.IsTrue(clip.isLooping);
                        animator.Update(clip.length * 2.2f);
                        var state = animator.GetCurrentAnimatorStateInfo(0);
                        Assert.IsTrue(state.IsName(transition.destinationState.name));
                        Assert.IsTrue(state.loop);
                        Assert.Greater(state.normalizedTime, 2f, "Kick must continue beyond two cycles.");
                        var bones = model.GetComponentsInChildren<Transform>(true);
                        var rotations = bones.Select(b => b.localRotation).ToArray();
                        animator.Update(clip.length * .2f);
                        Assert.IsTrue(bones.Where((b, i) => Quaternion.Angle(rotations[i], b.localRotation) > .1f).Any(),
                            "Looping kick must keep moving rather than hold its last pose.");
                    }
                    input.StopEmotion(true);
                    animator.Update(.01f);
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Walking"));
                    Assert.IsFalse(input.EmotionPlaying);
                    Assert.IsFalse(playerCamera.IsEmotionView);
                    Assert.AreEqual(originalPosition, cameraObject.transform.localPosition);
                    Assert.Less(Quaternion.Angle(originalRotation, cameraObject.transform.localRotation), .01f);
                    Assert.AreEqual(0, viewCamera.cullingMask & (1 << 30));
                }
                Assert.IsFalse(animator.applyRootMotion);
            }
            finally
            {
                Object.DestroyImmediate(inputHost);
                Object.DestroyImmediate(model);
            }
        }
    }
}
