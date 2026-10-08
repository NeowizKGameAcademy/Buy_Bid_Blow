using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueAudio
{
    [Serializable]
    public sealed class CueDocument
    {
        public int version = 1;
        public List<CueDefinition> cues = new List<CueDefinition>();

        public static CueDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                throw new FormatException("Cue JSON must be an object containing version and cues.");
            var result = new CueDocument { version = 0, cues = null };
            JsonUtility.FromJsonOverwrite(json, result);
            if (result.version == 0 || result.cues == null || !HasRootCueArray(json))
                throw new FormatException("Cue JSON requires version and a cues array.");
            return result;
        }

        // JsonUtility normalizes missing/null lists to empty lists. Check the required root field
        // separately, ignoring strings and nested objects rather than accepting a nested "cues" key.
        private static bool HasRootCueArray(string json)
        {
            int depth = 0;
            for (int i = 0; i < json.Length; i++)
            {
                char token = json[i];
                if (token == '{' || token == '[') { depth++; continue; }
                if (token == '}' || token == ']') { depth--; continue; }
                if (token != '"') continue;
                int start = ++i;
                for (; i < json.Length; i++)
                {
                    if (json[i] == '\\') { i++; continue; }
                    if (json[i] == '"') break;
                }
                if (depth != 1 || i - start != 4 || string.CompareOrdinal(json, start, "cues", 0, 4) != 0) continue;
                int next = i + 1;
                while (next < json.Length && char.IsWhiteSpace(json[next])) next++;
                if (next >= json.Length || json[next++] != ':') continue;
                while (next < json.Length && char.IsWhiteSpace(json[next])) next++;
                if (next < json.Length && json[next] == '[') return true;
            }
            return false;
        }
    }

    [Serializable]
    public sealed class CueDefinition
    {
        public string id = "new.cue";
        public List<string> clips = new List<string>();
        [Tooltip("first, random, randomNoRepeat, sequence")]
        public string selection = "randomNoRepeat";
        public string bus = "sfx";
        [Range(0, 1)] public float volume = 1;
        public Vector2 pitchRange = Vector2.one;
        public bool loop;
        [Range(0, 1)] public float spatialBlend;
        [Min(0.01f)] public float minDistance = 1;
        [Min(0.01f)] public float maxDistance = 30;
        [Min(0)] public float cooldownSeconds;
        [Min(1)] public int maxInstances = 8;
        [Tooltip("reject or stopOldest")]
        public string overflow = "reject";
    }

    public interface IAudioClipResolver
    {
        bool TryResolve(string id, out AudioClip clip);
    }

    [Serializable]
    public sealed class ClipBinding
    {
        public string id;
        public AudioClip clip;
    }

    [Serializable]
    public sealed class AudioBusBinding
    {
        public string id = "sfx";
        public UnityEngine.Audio.AudioMixerGroup mixerGroup;
        [Range(0, 1)] public float volume = 1;
    }

    public static class CueValidation
    {
        public static List<string> Validate(CueDocument document, IAudioClipResolver resolver,
            IEnumerable<AudioBusBinding> buses)
        {
            var errors = new List<string>();
            var busIds = new HashSet<string>(StringComparer.Ordinal);
            if (buses != null)
                foreach (var bus in buses)
                {
                    if (bus == null || string.IsNullOrWhiteSpace(bus.id) || !busIds.Add(bus.id))
                        errors.Add("Bus IDs must be nonempty and unique.");
                    else if (!InRange(bus.volume, 0, 1)) errors.Add($"Bus '{bus.id}': volume must be 0..1.");
                }
            if (document == null || document.cues == null)
            {
                errors.Add("Missing cue document.");
                return errors;
            }
            if (document.version != 1) errors.Add($"Unsupported cue version: {document.version}.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cue in document.cues)
            {
                if (cue == null) { errors.Add("Null cue entry."); continue; }
                string label = $"Cue '{cue.id}'";
                if (string.IsNullOrWhiteSpace(cue.id) || cue.id.Trim() != cue.id || !ids.Add(cue.id))
                    errors.Add(label + ": ID must be nonempty, unique and have no surrounding whitespace.");
                if (cue.selection != "first" && cue.selection != "random" &&
                    cue.selection != "randomNoRepeat" && cue.selection != "sequence")
                    errors.Add(label + ": invalid selection mode.");
                if (cue.overflow != "reject" && cue.overflow != "stopOldest")
                    errors.Add(label + ": invalid overflow policy.");
                if (cue.bus == null || !busIds.Contains(cue.bus)) errors.Add(label + ": unknown bus.");
                if (!InRange(cue.volume, 0, 1) || !InRange(cue.spatialBlend, 0, 1))
                    errors.Add(label + ": volume and spatialBlend must be 0..1.");
                if (!InRange(cue.pitchRange.x, 0.01f, 3) || !InRange(cue.pitchRange.y, cue.pitchRange.x, 3))
                    errors.Add(label + ": pitch must satisfy 0.01 <= min <= max <= 3.");
                if (!InRange(cue.cooldownSeconds, 0, float.MaxValue) || cue.maxInstances < 1)
                    errors.Add(label + ": cooldown must be nonnegative and maxInstances at least 1.");
                if (!InRange(cue.minDistance, 0.01f, float.MaxValue) ||
                    !InRange(cue.maxDistance, cue.minDistance, float.MaxValue))
                    errors.Add(label + ": invalid 3D distances.");
                if (cue.clips == null || cue.clips.Count == 0) errors.Add(label + ": add at least one clip.");
                else
                {
                    var clipIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string clipId in cue.clips)
                        if (string.IsNullOrWhiteSpace(clipId) || !clipIds.Add(clipId) || resolver == null ||
                            !resolver.TryResolve(clipId, out var clip) || clip == null)
                            errors.Add(label + $": missing or duplicate clip '{clipId}'.");
                }
            }
            return errors;
        }

        internal static bool InRange(float value, float min, float max) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
    }
}
