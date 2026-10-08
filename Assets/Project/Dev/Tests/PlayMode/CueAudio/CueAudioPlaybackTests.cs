using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CueAudio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CueAudioPlaybackTests
{
    private GameObject root, listener;
    private CueCatalog catalog;
    private AudioClip clip;
    private TextAsset json;
    private CueAudioHost host;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        clip = AudioClip.Create("Playback test", 4410, 1, 44100, false);
        var document = new CueDocument { cues = new List<CueDefinition>
        {
            new CueDefinition { id = "once", clips = new List<string> { "tone" } },
            new CueDefinition { id = "loop", clips = new List<string> { "tone" }, loop = true }
        } };
        json = new TextAsset(JsonUtility.ToJson(document));
        catalog = ScriptableObject.CreateInstance<CueCatalog>();
        Set(catalog, "definitions", json);
        Set(catalog, "clips", new List<ClipBinding> { new ClipBinding { id = "tone", clip = clip } });
        listener = new GameObject("Test listener", typeof(AudioListener));
        root = new GameObject("Test host");
        root.SetActive(false);
        host = root.AddComponent<CueAudioHost>();
        Set(host, "catalog", catalog);
        Set(host, "persistAcrossScenes", false);
        root.SetActive(true);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(root);
        Object.Destroy(listener);
        yield return null;
        Object.Destroy(catalog);
        Object.Destroy(clip);
        Object.Destroy(json);
    }

    [UnityTest]
    public IEnumerator OneShotCompletesAndItsSourceIsReused()
    {
        var once = host.Engine.Play("once");
        var source = host.Engine.GetActiveVoices().Single().Source;
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(once.IsPlaying, Is.False);
        Assert.That(host.Engine.ActiveCount, Is.Zero);
        var loop = host.Engine.Play("loop");
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(loop.IsPlaying, Is.True);
        Assert.That(host.Engine.GetActiveVoices().Single().Source, Is.SameAs(source));
        loop.Stop(0.1f);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(loop.IsPlaying, Is.False);
    }

    [UnityTest]
    public IEnumerator HostDisableStopsAndReenableCreatesFreshEngine()
    {
        var previousEngine = host.Engine;
        var playing = previousEngine.Play("loop");
        host.enabled = false;
        Assert.That(playing.IsPlaying, Is.False);
        Assert.That(CueAudioHost.Instance, Is.Null);
        yield return null;
        host.enabled = true;
        Assert.That(host.Engine, Is.Not.SameAs(previousEngine));
        Assert.That(host.Engine.Play("loop").IsPlaying, Is.True);
    }

    [UnityTest]
    public IEnumerator DisablingEmitterStopsAllOfItsOverlappingSounds()
    {
        var emitter = root.AddComponent<CueEmitter>();
        Set(emitter, "cue", new CueReference(catalog, "loop"));
        emitter.Play();
        emitter.Play();
        Assert.That(host.Engine.ActiveCount, Is.EqualTo(2));
        emitter.enabled = false;
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(host.Engine.ActiveCount, Is.Zero);
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
