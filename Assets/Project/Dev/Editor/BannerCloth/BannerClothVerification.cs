using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BuyBidBlow.Dev.BannerCloth;

namespace BuyBidBlow.Dev.Editor
{
    public static class BannerClothVerification
    {
        const string Key = "BBB_BannerVerification";
        const string Output = "Docs/Arts/Props/ChampionshipBanner";
        static Cloth cloth, control;
        static Vector3[] initial;
        static float start, maxMove, maxPin, maxContact;

        public static void RunBatch()
        {
            BannerClothBuilder.Build();
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void Restore()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                var demo = UnityEngine.Object.FindFirstObjectByType<BannerClothDemo>();
                if (!demo) throw new Exception("Demo missing");
                if (!cloth)
                {
                    cloth = demo.banner; initial = cloth.vertices; start = Time.time;
                    var copy = UnityEngine.Object.Instantiate(cloth.gameObject);
                    copy.name = "Verification_NoCollision_Control";
                    copy.transform.position += Vector3.right * 15;
                    control = copy.GetComponent<Cloth>(); control.capsuleColliders = Array.Empty<CapsuleCollider>();
                    return;
                }
                control.externalAcceleration = demo.WindAcceleration;
                var v = cloth.vertices; var cv = control.vertices; var coeff = cloth.coefficients;
                for (int i = 0; i < v.Length; i++)
                {
                    if (float.IsNaN(v[i].x) || float.IsInfinity(v[i].magnitude) || v[i].magnitude > 20)
                        throw new Exception("Unstable simulation");
                    float movement = Vector3.Distance(v[i], initial[i]);
                    if (coeff[i].maxDistance == 0) maxPin = Mathf.Max(maxPin, movement);
                    else maxMove = Mathf.Max(maxMove, movement);
                    if (Time.time - start > 2) maxContact = Mathf.Max(maxContact, Vector3.Distance(v[i], cv[i]));
                }
                if (Time.time - start < 12) return;
                if (maxMove < .05f || maxPin > .01f || maxContact < .05f)
                    throw new Exception($"Cloth verification failed: motion={maxMove}, pin={maxPin}, collision difference={maxContact}");
                var camera = Camera.main;
                var rt = new RenderTexture(1280, 720, 24);
                camera.targetTexture = rt; camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/Unity_Cloth_Preview.png", image.EncodeToPNG());
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                File.WriteAllText(Output + "/Verification.txt", $"PASS Unity {Application.unityVersion}, 12 seconds Play Mode\nVertices: {v.Length}\nMax free vertex displacement: {maxMove:F4} m\nMax pinned vertex displacement: {maxPin:F6} m\nMax difference against no-collider control: {maxContact:F4} m\nFinite/bounded vertices: PASS\nKeyboard interaction remains manual; moving capsule contact is automated.\n");
                Finish(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        static void Finish(int code)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
    }
}
