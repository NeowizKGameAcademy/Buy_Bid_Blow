using System;
using System.Collections.Generic;
using System.Linq;
using CueAudio;
using CueAudio.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class CueAudioTests
{
    private CueCatalog catalog;
    private GameObject root;
    private AudioClip clipA, clipB;
    private TextAsset json;
    private CueAudioEngine engine;
    private double now;
    private CueDefinition cue;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("CueAudio test");
        clipA = AudioClip.Create("A", 44100, 1, 44100, false);
        clipB = AudioClip.Create("B", 44100, 1, 44100, false);
        catalog = ScriptableObject.CreateInstance<CueCatalog>();
        cue = new CueDefinition { id = "test.cue", clips = new List<string> { "a", "b" }, loop = true };
        now = 0;
    }

    private void Build(int capacity = 4)
    {
        json = new TextAsset(JsonUtility.ToJson(new CueDocument { cues = new List<CueDefinition> { cue } }));
        using (var serialized = new SerializedObject(catalog))
        {
            serialized.FindProperty("definitions").objectReferenceValue = json;
            var bindings = serialized.FindProperty("clips");
            bindings.arraySize = 2;
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "a";
            bindings.GetArrayElementAtIndex(0).FindPropertyRelative("clip").objectReferenceValue = clipA;
            bindings.GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "b";
            bindings.GetArrayElementAtIndex(1).FindPropertyRelative("clip").objectReferenceValue = clipB;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        engine = new CueAudioEngine(catalog, root.transform, capacity, () => now, new System.Random(42));
    }

    [TearDown]
    public void TearDown()
    {
        engine?.Dispose();
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(clipA);
        Object.DestroyImmediate(clipB);
        if (json != null) Object.DestroyImmediate(json);
        AudioListener.pause = false;
    }

    [Test]
    public void StaleHandleCannotStopReusedVoice()
    {
        Build(1);
        var old = engine.Play(cue.id);
        old.Stop();
        var current = engine.Play(cue.id);
        old.Stop();
        Assert.That(old.IsPlaying, Is.False);
        Assert.That(current.IsPlaying, Is.True);
        Assert.That(engine.PoolSize, Is.EqualTo(1));
    }

    [Test]
    public void CooldownRejectDoesNotMoveLastSuccessfulTime()
    {
        cue.cooldownSeconds = 1;
        Build();
        Assert.That(engine.Play(cue.id).IsPlaying, Is.True);
        now = 0.9;
        Assert.That(engine.Play(cue.id).IsPlaying, Is.False);
        Assert.That(engine.History.Last().Result, Is.EqualTo("Cooldown"));
        now = 1;
        Assert.That(engine.Play(cue.id).IsPlaying, Is.True);
    }

    [Test]
    public void StopOldestInvalidatesOnlyOldestHandle()
    {
        cue.maxInstances = 2;
        cue.overflow = "stopOldest";
        Build(2);
        var first = engine.Play(cue.id);
        var second = engine.Play(cue.id);
        var third = engine.Play(cue.id);
        Assert.That(first.IsPlaying, Is.False);
        Assert.That(second.IsPlaying && third.IsPlaying, Is.True);
        Assert.That(engine.ActiveCount, Is.EqualTo(2));
    }

    [Test]
    public void PoolAndInstanceLimitsReportDifferentReasons()
    {
        Build(1);
        engine.Play(cue.id);
        Assert.That(engine.Play(cue.id).IsPlaying, Is.False);
        Assert.That(engine.History.Last().Result, Is.EqualTo("Pool full"));
        engine.Dispose();
        engine = null;
        Object.DestroyImmediate(json);
        cue.maxInstances = 1;
        Build(2);
        engine.Play(cue.id);
        Assert.That(engine.Play(cue.id).IsPlaying, Is.False);
        Assert.That(engine.History.Last().Result, Is.EqualTo("Instance limit"));
    }

    [Test]
    public void RandomNoRepeatAndSequenceChooseCorrectClips()
    {
        Build(1);
        AudioClip previous = null;
        for (int i = 0; i < 12; i++)
        {
            var handle = engine.Play(cue.id);
            var chosen = engine.GetActiveVoices().Single().Source.clip;
            Assert.That(chosen, Is.Not.SameAs(previous));
            previous = chosen;
            handle.Stop();
        }
        engine.Dispose();
        engine = null;
        Object.DestroyImmediate(json);
        cue.selection = "sequence";
        Build(1);
        for (int i = 0; i < 6; i++)
        {
            var handle = engine.Play(cue.id);
            Assert.That(engine.GetActiveVoices().Single().Source.clip, Is.SameAs(i % 2 == 0 ? clipA : clipB));
            handle.Stop();
        }
    }

    [Test]
    public void FadeUsesInjectedClockAndMultipliesBusAndMasterVolumes()
    {
        cue.volume = 0.8f;
        Build();
        AudioListener.pause = true;
        engine.SetBusVolume("sfx", 0.5f);
        engine.SetMasterVolume(0.5f);
        var handle = engine.Play(cue.id, fadeInSeconds: 2);
        var source = engine.GetActiveVoices().Single().Source;
        now = 1;
        engine.Tick();
        Assert.That(source.volume, Is.EqualTo(0.1f).Within(0.001));
        handle.Stop(1);
        now = 1.5;
        engine.Tick();
        Assert.That(source.volume, Is.EqualTo(0.05f).Within(0.001));
        now = 2;
        engine.Tick();
        Assert.That(handle.IsPlaying, Is.False);
        Assert.That(source.clip, Is.Null);
        Assert.That(source.loop, Is.False);
        Assert.That(source.pitch, Is.EqualTo(1));
    }

    [Test]
    public void FailedCrossfadePreservesCurrentMusic()
    {
        Build(1);
        var music = engine.Play(cue.id);
        Assert.That(engine.Crossfade(music, "missing").IsPlaying, Is.True);
        Assert.That(engine.History.Last().Result, Is.EqualTo("Unknown cue"));
        Assert.That(music.IsPlaying, Is.True);
    }

    [Test]
    public void ReferenceCatalogMismatchAndCallerAreRecorded()
    {
        Build();
        Assert.That(engine.Play(new CueReference(null, cue.id), root).IsPlaying, Is.False);
        Assert.That(engine.History.Last().Result, Is.EqualTo("Catalog mismatch"));
        engine.Play(new CueReference(catalog, cue.id), root);
        var record = engine.History.Last();
        Assert.That(record.Owner, Is.SameAs(root));
        Assert.That(record.File, Does.EndWith("CueAudioTests.cs"));
        Assert.That(record.Line, Is.GreaterThan(0));
    }

    [Test]
    public void HistoryIsBoundedAndDisposeInvalidatesHandles()
    {
        Build();
        var handle = engine.Play(cue.id);
        for (int i = 0; i < 210; i++) engine.Play("missing");
        Assert.That(engine.History.Count, Is.EqualTo(200));
        engine.Dispose();
        Assert.That(handle.IsPlaying, Is.False);
        Assert.That(root.transform.childCount, Is.Zero);
    }

    [Test]
    public void ValidationRejectsBrokenSettingsAndDuplicateIds()
    {
        Build();
        cue.pitchRange = new Vector2(2, 1);
        cue.clips.Add("missing");
        cue.bus = "missing";
        var document = new CueDocument { version = 99, cues = new List<CueDefinition> { cue, cue } };
        var errors = CueValidation.Validate(document, catalog, catalog.Buses);
        Assert.That(errors.Any(error => error.Contains("version")), Is.True);
        Assert.That(errors.Any(error => error.Contains("unique")), Is.True);
        Assert.That(errors.Any(error => error.Contains("pitch")), Is.True);
        Assert.That(errors.Any(error => error.Contains("missing")), Is.True);
        Assert.That(errors.Any(error => error.Contains("bus")), Is.True);
    }

    [TestCase("{}")]
    [TestCase("{\"version\":1}")]
    [TestCase("{\"cues\":[]}")]
    [TestCase("{\"version\":1,\"cues\":null}")]
    [TestCase("{\"version\":1,\"nested\":{\"cues\":[]}}")]
    [TestCase("[]")]
    public void JsonRequiresVersionAndCueArray(string text)
    {
        Assert.Throws<FormatException>(() => CueDocument.Parse(text));
    }

    [Test]
    public void UsageScanFindsUnsavedSceneReferenceWithoutSavingScene()
    {
        Build();
        var emitter = root.AddComponent<CueEmitter>();
        using (var serialized = new SerializedObject(emitter))
        {
            var reference = serialized.FindProperty("cue");
            reference.FindPropertyRelative("catalog").objectReferenceValue = catalog;
            reference.FindPropertyRelative("id").stringValue = cue.id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var scene = root.scene;
        bool wasDirty = scene.isDirty;
        var result = CueUsageScanner.Scan(catalog, cue.id);
        Assert.That(result.Any(usage => usage.Target == emitter && usage.Property == "cue"), Is.True);
        Assert.That(root.scene, Is.EqualTo(scene));
        Assert.That(scene.isDirty, Is.EqualTo(wasDirty));
    }

    [Test]
    public void UsageScanFindsClosedScenePrefabAndUnityEventConnections()
    {
        string folder = "Assets/CueAudioScanTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
        Scene testScene = default;
        var previousScene = SceneManager.GetActiveScene();
        try
        {
            var assetCatalog = ScriptableObject.CreateInstance<CueCatalog>();
            AssetDatabase.CreateAsset(assetCatalog, folder + "/Catalog.asset");
            testScene = EditorSceneManager.NewPreviewScene();
            var holder = new GameObject("Saved cue connection", typeof(CueEmitter), typeof(CueUsageEventFixture));
            SceneManager.MoveGameObjectToScene(holder, testScene);
            var emitter = holder.GetComponent<CueEmitter>();
            using (var serialized = new SerializedObject(emitter))
            {
                var reference = serialized.FindProperty("cue");
                reference.FindPropertyRelative("catalog").objectReferenceValue = assetCatalog;
                reference.FindPropertyRelative("id").stringValue = "saved.cue";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var fixture = holder.GetComponent<CueUsageEventFixture>();
            fixture.sound = new CueReference(assetCatalog, "saved.cue");
            UnityEventTools.AddPersistentListener(fixture.onPlay, emitter.Play);
            PrefabUtility.SaveAsPrefabAsset(holder, folder + "/Emitter.prefab");
            // A minimal scene can contain the same standalone serialized GameObjects as this
            // prefab. Import a copy so the fixture never saves or replaces the user's open scene.
            System.IO.File.Copy(folder + "/Emitter.prefab", folder + "/Saved.unity");
            AssetDatabase.ImportAsset(folder + "/Saved.unity", ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.ClosePreviewScene(testScene);
            testScene = default;
            SceneManager.SetActiveScene(previousScene);
            var results = CueUsageScanner.Scan(assetCatalog, "saved.cue");
            Assert.That(results.Any(usage => usage.AssetPath.EndsWith("Saved.unity") && usage.Property == "cue"), Is.True);
            Assert.That(results.Any(usage => usage.AssetPath.EndsWith("Emitter.prefab") && usage.Property == "sound"), Is.True);
            Assert.That(results.Count(usage => usage.Kind == "UnityEvent → Play"), Is.EqualTo(2));
            Assert.That(SceneManager.GetSceneByPath(folder + "/Saved.unity").isLoaded, Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            var savedUsage = results.First(usage => usage.AssetPath.EndsWith("Saved.unity") && usage.Property == "cue");
            Assert.That(GlobalObjectId.TryParse(savedUsage.GlobalId, out var globalId), Is.True);
            Assert.That(globalId.assetGUID.ToString(), Is.EqualTo(AssetDatabase.AssetPathToGUID(folder + "/Saved.unity")));
            Assert.That(globalId.targetObjectId, Is.Not.Zero);
        }
        finally
        {
            if (testScene.IsValid()) EditorSceneManager.ClosePreviewScene(testScene);
            SceneManager.SetActiveScene(previousScene);
            AssetDatabase.DeleteAsset(folder);
        }
    }
}
