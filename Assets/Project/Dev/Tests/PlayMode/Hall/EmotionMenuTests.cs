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

        // Every radial item plays on the wolf every player shows (PlayerAvatar.Emotes -> BBBWolfCharacter),
        // and the emote view puts the camera behind the body and back.
        [UnityTest]
        public IEnumerator AllEightEmotesPlayOnTheWolfAndTheViewReturns()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var wolf = Object.Instantiate(player.GetComponentInChildren<BBBWolfCharacter>(true).gameObject);
            var host = new GameObject("Emotion view test");
            host.SetActive(false);
            var playerCamera = host.AddComponent<PlayerCamera>();
            var cameraObject = new GameObject("Emotion camera", typeof(Camera));
            cameraObject.transform.SetParent(host.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(host.transform, false); body.layer = 8;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var viewCamera = cameraObject.GetComponent<Camera>();
            viewCamera.cullingMask = ~(1 << 8);
            var originalPosition = new Vector3(0, 1.7f, 0);
            cameraObject.transform.localPosition = originalPosition;
            typeof(PlayerCamera).GetField("cameraObject", Private).SetValue(playerCamera, cameraObject);
            host.SetActive(true);
            try
            {
                var character = wolf.GetComponent<BBBWolfCharacter>();
                var animator = character.animator;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                yield return null;
                var armature = character.model.Find("Armature") ?? animator.transform.Find("Armature");
                var hips = armature.Find("mixamorig:Hips");
                for (float end = Time.time + .3f; Time.time < end;) yield return null;
                Vector3 restArmature = armature.localPosition; float restHips = hips.localPosition.y;
                Assert.AreEqual(8, PlayerAvatar.Emotes.Length);
                for (int i = 0; i < PlayerAvatar.Emotes.Length; i++)
                {
                    var emote = PlayerAvatar.Emotes[i];
                    // The first four are the original Mixamo states; the rest are named after the action.
                    var expected = (i < 4 ? "Mixamo_" : "") + emote;
                    character.PlayAction(emote);
                    for (float end = Time.time + .3f; Time.time < end;) yield return null;
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(expected), emote + " plays");
                    character.StopAction();
                    for (float end = Time.time + .3f; Time.time < end;) yield return null;
                    // The windmill lowers the Armature and the laying pose the hips; idle must bring both back.
                    Assert.Less(Vector3.Distance(restArmature, armature.localPosition), .01f, emote + " leaves the body lowered");
                    Assert.AreEqual(restHips, hips.localPosition.y, .05f, emote + " leaves the hips lowered");
                }
                playerCamera.SetEmotionView(true);
                Assert.IsTrue(playerCamera.IsEmotionView);
                Assert.Less(cameraObject.transform.localPosition.z, -1f);
                Assert.AreNotEqual(0, viewCamera.cullingMask & (1 << 8), "own body visible from behind");
                playerCamera.SetEmotionView(false);
                Assert.AreEqual(originalPosition, cameraObject.transform.localPosition);
                Assert.AreEqual(0, viewCamera.cullingMask & (1 << 8));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(wolf);
            }
        }
    }
}
