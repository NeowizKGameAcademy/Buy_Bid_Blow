using System;
using System.Collections.Generic;
using System.IO;
using CueAudio;
using CueAudio.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class CueAudioDemoBuilder
    {
        public const string ScenePath = "Assets/Project/Dev/Sandbox/CueAudioDemo.unity";
        public const string CatalogPath = "Assets/Project/Audio/Cues/CueAudioDemoCatalog.asset";
        private const string ClipPath = "Assets/Project/Audio/Clips/CueAudioDemo";
        private const string JsonPath = "Assets/Project/Audio/Cues/CueAudioDemo.json";
        private const string PrefabPath = "Assets/Project/Content/Prefabs/Audio/CueAudioHost.prefab";

        [MenuItem("Tools/CueAudio/Open Demo")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("CueAudio", "Exit Play mode before opening the demo scene.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) BuildDemo();
            else EditorSceneManager.OpenScene(ScenePath);
            CueBrowserWindow.Open(AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath));
        }

        // Also callable in batchmode to create and verify the checked-in sample assets.
        public static void BuildDemo()
        {
            Directory.CreateDirectory(ClipPath);
            Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            string[] ids = { "demo_click", "demo_bid_a", "demo_bid_b", "demo_spatial", "demo_music_a", "demo_music_b" };
            float[] frequencies = { 880, 660, 990, 440, 220, 261.6256f };
            for (int i = 0; i < ids.Length; i++) WriteTone(ClipPath + "/" + ids[i] + ".wav", frequencies[i], i >= 4 ? 4 : 0.18f, i >= 4);
            var cues = new List<CueDefinition>
            {
                new CueDefinition { id = "ui.click", bus = "ui", clips = new List<string> { ids[0] }, volume = 0.6f },
                new CueDefinition { id = "auction.bid", clips = new List<string> { ids[1], ids[2] }, volume = 0.6f,
                    pitchRange = new Vector2(0.97f, 1.03f), cooldownSeconds = 0.1f, maxInstances = 3 },
                new CueDefinition { id = "world.ping", clips = new List<string> { ids[3] }, spatialBlend = 1, minDistance = 2 },
                new CueDefinition { id = "bgm.demo_a", bus = "music", clips = new List<string> { ids[4] }, loop = true, volume = 0.4f, maxInstances = 2 },
                new CueDefinition { id = "bgm.demo_b", bus = "music", clips = new List<string> { ids[5] }, loop = true, volume = 0.4f, maxInstances = 2 }
            };
            if (!File.Exists(JsonPath)) File.WriteAllText(JsonPath, JsonUtility.ToJson(new CueDocument { cues = cues }, true));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = AssetDatabase.LoadAssetAtPath<CueCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CueCatalog>();
                using (var serialized = new SerializedObject(catalog))
                {
                    serialized.FindProperty("definitions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
                    var clips = serialized.FindProperty("clips");
                    clips.arraySize = ids.Length;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var entry = clips.GetArrayElementAtIndex(i);
                        entry.FindPropertyRelative("id").stringValue = ids[i];
                        entry.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath + "/" + ids[i] + ".wav");
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var errors = catalog.Validate();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Camera + AudioListener", typeof(Camera), typeof(AudioListener));
                cameraObject.transform.position = Vector3.zero;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
                var hostObject = new GameObject("CueAudioHost", typeof(CueAudioHost));
                using (var serialized = new SerializedObject(hostObject.GetComponent<CueAudioHost>()))
                {
                    serialized.FindProperty("catalog").objectReferenceValue = catalog;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (!File.Exists(PrefabPath)) PrefabUtility.SaveAsPrefabAssetAndConnect(hostObject, PrefabPath, InteractionMode.AutomatedAction);
                var emitter = new GameObject("Bid Sound Emitter", typeof(CueEmitter)).GetComponent<CueEmitter>();
                SetReference(emitter, "cue", catalog, "auction.bid");
                var demo = new GameObject("CueAudio Demo Controls", typeof(CueAudioDemo)).GetComponent<CueAudioDemo>();
                SetReference(demo, "click", catalog, "ui.click");
                SetReference(demo, "bid", catalog, "auction.bid");
                SetReference(demo, "spatial", catalog, "world.ping");
                SetReference(demo, "musicA", catalog, "bgm.demo_a");
                SetReference(demo, "musicB", catalog, "bgm.demo_b");
                using (var serialized = new SerializedObject(demo))
                { serialized.FindProperty("emitter").objectReferenceValue = emitter; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CueAudio demo is ready: " + ScenePath);
        }

        private static void SetReference(UnityEngine.Object target, string property, CueCatalog catalog, string id)
        {
            using (var serialized = new SerializedObject(target))
            {
                var reference = serialized.FindProperty(property);
                reference.FindPropertyRelative("catalog").objectReferenceValue = catalog;
                reference.FindPropertyRelative("id").stringValue = id;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WriteTone(string path, float frequency, float duration, bool music)
        {
            if (File.Exists(path)) return;
            const int rate = 22050;
            int count = Mathf.RoundToInt(rate * duration);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / rate;
                    float envelope = music ? Mathf.Min(1, t * 20, (duration - t) * 20) * (0.65f + 0.35f * Mathf.Cos(t * Mathf.PI * 4))
                        : Mathf.Min(1, t * 150) * Mathf.Exp(-t * 25) * Mathf.Min(1, (duration - t) * 150);
                    float signal = Mathf.Sin(2 * Mathf.PI * frequency * t);
                    if (music) signal = (signal + 0.5f * Mathf.Sin(2 * Mathf.PI * frequency * 1.5f * t)) / 1.5f;
                    writer.Write((short)(signal * envelope * 0.25f * short.MaxValue));
                }
            }
        }
    }
}
