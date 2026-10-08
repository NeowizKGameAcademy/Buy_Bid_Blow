using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CueAudio
{
    public sealed class PlaybackRecord
    {
        public double Time { get; internal set; }
        public string CueId { get; internal set; }
        public string Result { get; internal set; }
        public string Clip { get; internal set; }
        public Object Owner { get; internal set; }
        public string OwnerName { get; internal set; }
        public string File { get; internal set; }
        public int Line { get; internal set; }
    }

    public readonly struct ActiveVoice
    {
        public readonly string CueId;
        public readonly AudioSource Source;
        public readonly Object Owner;
        public readonly AudioHandle Handle;
        internal ActiveVoice(string cueId, AudioSource source, Object owner, AudioHandle handle)
        { CueId = cueId; Source = source; Owner = owner; Handle = handle; }
    }

    /// <summary>Main-thread playback service. Call Tick once per frame and Dispose on shutdown.</summary>
    public sealed class CueAudioEngine : IDisposable
    {
        private sealed class RuntimeCue
        {
            public CueDefinition Definition;
            public AudioClip[] Clips;
            public int LastClip = -1;
            public double LastPlayed = double.NegativeInfinity;
        }

        private sealed class Voice
        {
            public AudioSource Source;
            public RuntimeCue Cue;
            public Object Owner;
            public ulong Generation;
            public double Started;
            public float Envelope = 1;
            public float FadeFrom;
            public float FadeTarget;
            public double FadeStart;
            public float FadeDuration;
            public bool StopAfterFade;
        }

        private readonly CueCatalog catalog;
        private readonly Transform root;
        private readonly Func<double> clock;
        private readonly System.Random random;
        private readonly Dictionary<string, RuntimeCue> cues = new Dictionary<string, RuntimeCue>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioBusBinding> buses = new Dictionary<string, AudioBusBinding>(StringComparer.Ordinal);
        private readonly List<Voice> voices = new List<Voice>();
        private readonly List<PlaybackRecord> history = new List<PlaybackRecord>();
        private readonly int capacity;
        private ulong nextGeneration;
        private bool disposed;
        private float masterVolume = 1;

        public IReadOnlyList<PlaybackRecord> History => history;
        public CueCatalog Catalog => catalog;
        public int PoolSize => voices.Count;
        public int ActiveCount
        {
            get { int count = 0; foreach (var voice in voices) if (voice.Cue != null) count++; return count; }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool RecordHistory { get; set; } = true;
#else
        public bool RecordHistory { get; set; }
#endif

        public CueAudioEngine(CueCatalog catalog, Transform root, int capacity = 32,
            Func<double> clock = null, System.Random random = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            var errors = catalog.Validate();
            if (errors.Count != 0) throw new ArgumentException(string.Join("\n", errors), nameof(catalog));
            this.catalog = catalog;
            this.root = root;
            this.capacity = capacity;
            this.clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
            this.random = random ?? new System.Random();
            foreach (var bus in catalog.Buses)
                buses.Add(bus.id, new AudioBusBinding { id = bus.id, mixerGroup = bus.mixerGroup, volume = bus.volume });
            foreach (var definition in catalog.ReadDocument().cues)
            {
                var resolved = new AudioClip[definition.clips.Count];
                for (int i = 0; i < resolved.Length; i++) catalog.TryResolve(definition.clips[i], out resolved[i]);
                cues.Add(definition.id, new RuntimeCue { Definition = definition, Clips = resolved });
            }
        }

        public AudioHandle Play(CueReference cue, Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (cue.Catalog != catalog) return Reject(cue.Id, "Catalog mismatch", owner, file, line);
            return Play(cue.Id, owner, fadeInSeconds, file, line);
        }

        public AudioHandle Play(string cueId, Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
            PlayInternal(cueId, Vector3.zero, owner, fadeInSeconds, file, line);

        public AudioHandle PlayAt(CueReference cue, Vector3 position, Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (cue.Catalog != catalog) return Reject(cue.Id, "Catalog mismatch", owner, file, line);
            return PlayAt(cue.Id, position, owner, fadeInSeconds, file, line);
        }

        public AudioHandle PlayAt(string cueId, Vector3 position, Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
            PlayInternal(cueId, position, owner, fadeInSeconds, file, line);

        public AudioHandle Crossfade(AudioHandle previous, string cueId, float seconds = 1, Object owner = null,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var next = Play(cueId, owner, seconds, file, line);
            // Keep the existing music if the next request was rejected.
            if (next.IsPlaying) previous.Stop(seconds);
            return next.IsPlaying ? next : previous;
        }

        public AudioHandle Crossfade(AudioHandle previous, CueReference cue, float seconds = 1, Object owner = null,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var next = Play(cue, owner, seconds, file, line);
            if (next.IsPlaying) previous.Stop(seconds);
            return next.IsPlaying ? next : previous;
        }

        private AudioHandle PlayInternal(string id, Vector3 position, Object owner, float fade, string file, int line)
        {
            if (disposed || root == null) return Reject(id, "Engine unavailable", owner, file, line);
            if (id == null || !cues.TryGetValue(id, out var cue)) return Reject(id, "Unknown cue", owner, file, line);
            if (!CueValidation.InRange(fade, 0, float.MaxValue)) return Reject(id, "Invalid fade", owner, file, line);
            if (!root.gameObject.activeInHierarchy) return Reject(id, "Host inactive", owner, file, line);
            double now = clock();
            if (now - cue.LastPlayed < cue.Definition.cooldownSeconds) return Reject(id, "Cooldown", owner, file, line);
            int count = 0, oldest = -1, free = -1;
            for (int i = 0; i < voices.Count; i++)
            {
                var voice = voices[i];
                if (voice.Cue == null) { if (free < 0) free = i; continue; }
                if (voice.Cue != cue) continue;
                count++;
                if (oldest < 0 || voice.Generation < voices[oldest].Generation) oldest = i;
            }
            if (count >= cue.Definition.maxInstances)
            {
                if (cue.Definition.overflow == "reject") return Reject(id, "Instance limit", owner, file, line);
                free = oldest;
            }
            if (free < 0 && voices.Count >= capacity) return Reject(id, "Pool full", owner, file, line);

            int clipIndex = SelectClip(cue);
            var clip = cue.Clips[clipIndex];
            if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                return Reject(id, "Clip unavailable", owner, file, line);
            if (free < 0)
            {
                free = voices.Count;
                var child = new GameObject("CueAudio Voice");
                child.transform.SetParent(root, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                voices.Add(new Voice { Source = source });
            }
            var target = voices[free];
            Release(target);
            if (target.Source == null)
            {
                var child = new GameObject("CueAudio Voice");
                child.transform.SetParent(root, false);
                target.Source = child.AddComponent<AudioSource>();
                target.Source.playOnAwake = false;
            }
            target.Cue = cue;
            target.Owner = owner;
            target.Generation = ++nextGeneration;
            target.Started = now;
            target.Envelope = fade > 0 ? 0 : 1;
            target.FadeFrom = target.Envelope;
            target.FadeTarget = 1;
            target.FadeStart = now;
            target.FadeDuration = fade;
            target.StopAfterFade = false;
            var settings = cue.Definition;
            var audio = target.Source;
            audio.gameObject.name = "CueAudio: " + id;
            audio.transform.position = position;
            audio.clip = clip;
            audio.loop = settings.loop;
            audio.pitch = Mathf.Lerp(settings.pitchRange.x, settings.pitchRange.y, (float)random.NextDouble());
            audio.spatialBlend = settings.spatialBlend;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.minDistance = settings.minDistance;
            audio.maxDistance = settings.maxDistance;
            audio.dopplerLevel = 0;
            audio.outputAudioMixerGroup = buses[settings.bus].mixerGroup;
            ApplyVolume(target);
            audio.Play();
            cue.LastPlayed = now;
            cue.LastClip = clipIndex;
            Record(id, "Playing", clip.name, owner, file, line);
            return new AudioHandle(this, free, target.Generation);
        }

        private int SelectClip(RuntimeCue cue)
        {
            int length = cue.Clips.Length;
            switch (cue.Definition.selection)
            {
                case "first": return 0;
                case "sequence": return (cue.LastClip + 1) % length;
                case "randomNoRepeat":
                    if (length > 1 && cue.LastClip >= 0)
                    {
                        int index = random.Next(length - 1);
                        return index >= cue.LastClip ? index + 1 : index;
                    }
                    return random.Next(length);
                default: return random.Next(length);
            }
        }

        public void Tick()
        {
            if (disposed) return;
            double now = clock();
            foreach (var voice in voices)
            {
                if (voice.Cue == null) continue;
                if (voice.Source == null || voice.Source.clip == null ||
                    voice.Source.clip.loadState == AudioDataLoadState.Failed ||
                    (!AudioListener.pause && now - voice.Started > 0.05 &&
                     voice.Source.clip.loadState == AudioDataLoadState.Loaded && !voice.Source.isPlaying))
                { Release(voice); continue; }
                if (voice.FadeDuration <= 0) continue;
                float progress = Mathf.Clamp01((float)((now - voice.FadeStart) / voice.FadeDuration));
                voice.Envelope = Mathf.Lerp(voice.FadeFrom, voice.FadeTarget, progress);
                ApplyVolume(voice);
                if (progress < 1) continue;
                voice.FadeDuration = 0;
                if (voice.StopAfterFade) Release(voice);
            }
        }

        public void SetMasterVolume(float volume)
        {
            if (!CueValidation.InRange(volume, 0, 1)) throw new ArgumentOutOfRangeException(nameof(volume));
            masterVolume = volume;
            foreach (var voice in voices) if (voice.Cue != null) ApplyVolume(voice);
        }

        public void SetBusVolume(string bus, float volume)
        {
            if (!CueValidation.InRange(volume, 0, 1)) throw new ArgumentOutOfRangeException(nameof(volume));
            if (!buses.TryGetValue(bus, out var binding)) throw new ArgumentException("Unknown bus: " + bus);
            binding.volume = volume;
            foreach (var voice in voices) if (voice.Cue != null) ApplyVolume(voice);
        }

        public float GetBusVolume(string bus) => buses[bus].volume;
        public void ClearHistory() => history.Clear();
        public void StopAll(float fadeOutSeconds = 0)
        {
            for (int i = 0; i < voices.Count; i++) Stop(i, voices[i].Generation, fadeOutSeconds);
        }

        public IEnumerable<ActiveVoice> GetActiveVoices()
        {
            for (int i = 0; i < voices.Count; i++)
                if (voices[i].Cue != null)
                    yield return new ActiveVoice(voices[i].Cue.Definition.id, voices[i].Source, voices[i].Owner,
                        new AudioHandle(this, i, voices[i].Generation));
        }

        internal bool IsActive(int slot, ulong generation) => !disposed && slot >= 0 && slot < voices.Count &&
            voices[slot].Cue != null && voices[slot].Generation == generation;

        internal void Stop(int slot, ulong generation, float seconds)
        {
            if (!IsActive(slot, generation)) return;
            if (!CueValidation.InRange(seconds, 0, float.MaxValue)) throw new ArgumentOutOfRangeException(nameof(seconds));
            var voice = voices[slot];
            if (seconds == 0) { Release(voice); return; }
            voice.FadeFrom = voice.Envelope;
            voice.FadeTarget = 0;
            voice.FadeStart = clock();
            voice.FadeDuration = seconds;
            voice.StopAfterFade = true;
        }

        private void ApplyVolume(Voice voice) => voice.Source.volume =
            voice.Cue.Definition.volume * buses[voice.Cue.Definition.bus].volume * masterVolume * voice.Envelope;

        private static void Release(Voice voice)
        {
            if (voice.Source != null)
            {
                voice.Source.Stop();
                voice.Source.clip = null;
                voice.Source.loop = false;
                voice.Source.pitch = 1;
                voice.Source.volume = 1;
                voice.Source.spatialBlend = 0;
                voice.Source.outputAudioMixerGroup = null;
            }
            voice.Cue = null;
            voice.Owner = null;
            voice.FadeDuration = 0;
            voice.StopAfterFade = false;
        }

        private AudioHandle Reject(string id, string reason, Object owner, string file, int line)
        { Record(id, reason, null, owner, file, line); return default; }

        private void Record(string id, string result, string clip, Object owner, string file, int line)
        {
            if (!RecordHistory) return;
            if (history.Count >= 200) history.RemoveAt(0);
            history.Add(new PlaybackRecord { Time = clock(), CueId = id, Result = result, Clip = clip,
                Owner = owner, OwnerName = owner != null ? owner.name : "(none)", File = file, Line = line });
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var voice in voices)
            {
                Release(voice);
                if (voice.Source == null) continue;
                if (Application.isPlaying) Object.Destroy(voice.Source.gameObject);
                else Object.DestroyImmediate(voice.Source.gameObject);
            }
            voices.Clear();
        }
    }
}
