using UnityEngine;

namespace CueAudio
{
    [AddComponentMenu("CueAudio/Cue Emitter")]
    public sealed class CueEmitter : MonoBehaviour
    {
        [SerializeField] private CueReference cue;
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool useWorldPosition;
        [SerializeField] private bool stopOnDisable = true;
        [SerializeField, Min(0)] private float fadeInSeconds;
        [SerializeField, Min(0)] private float fadeOutSeconds = 0.1f;
        private readonly System.Collections.Generic.List<AudioHandle> handles = new System.Collections.Generic.List<AudioHandle>();

        private void Start() { if (playOnStart) Play(); }

        // These parameterless methods can be wired directly to a UI Button or UnityEvent.
        public void Play()
        {
            var engine = CueAudioHost.Instance != null ? CueAudioHost.Instance.Engine : null;
            if (engine == null) { Debug.LogWarning("No active CueAudioHost in the scene.", this); return; }
            handles.RemoveAll(handle => !handle.IsPlaying);
            var next = useWorldPosition ? engine.PlayAt(cue, transform.position, this, fadeInSeconds)
                : engine.Play(cue, this, fadeInSeconds);
            if (next.IsPlaying) handles.Add(next);
        }

        public void Stop()
        {
            foreach (var handle in handles) handle.Stop(fadeOutSeconds);
            handles.Clear();
        }

        private void OnDisable() { if (stopOnDisable) Stop(); }
    }
}
