using System;
using System.Collections.Generic;
using UnityEngine;

namespace CueAudio
{
    [CreateAssetMenu(menuName = "CueAudio/Catalog", fileName = "AudioCatalog")]
    public sealed class CueCatalog : ScriptableObject, IAudioClipResolver
    {
        [SerializeField] private TextAsset definitions;
        [SerializeField] private List<ClipBinding> clips = new List<ClipBinding>();
        [SerializeField] private List<AudioBusBinding> buses = new List<AudioBusBinding>
        {
            new AudioBusBinding { id = "sfx" },
            new AudioBusBinding { id = "music" },
            new AudioBusBinding { id = "ui" }
        };

        public TextAsset Definitions => definitions;
        public IReadOnlyList<ClipBinding> Clips => clips;
        public IReadOnlyList<AudioBusBinding> Buses => buses;

        public CueDocument ReadDocument()
        {
            if (definitions == null) throw new InvalidOperationException("Assign a cue JSON file to the catalog.");
            return CueDocument.Parse(definitions.text);
        }

        public bool TryResolve(string id, out AudioClip clip)
        {
            foreach (var entry in clips)
                if (entry != null && entry.id == id) { clip = entry.clip; return clip != null; }
            clip = null;
            return false;
        }

        public List<string> Validate(CueDocument document = null)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in clips)
                if (binding == null || string.IsNullOrWhiteSpace(binding.id) || !ids.Add(binding.id) || binding.clip == null)
                    errors.Add("Clip bindings require unique IDs and assigned AudioClips.");
            try { errors.AddRange(CueValidation.Validate(document ?? ReadDocument(), this, buses)); }
            catch (Exception exception) { errors.Add(exception.Message); }
            return errors;
        }
    }
}
