using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CueAudio
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class CueAudioHost : MonoBehaviour
    {
        [SerializeField] private CueCatalog catalog;
        [SerializeField, Min(1)] private int maxVoices = 32;
        [SerializeField] private bool persistAcrossScenes = true;
        public static CueAudioHost Instance { get; private set; }
        public CueAudioEngine Engine { get; private set; }
        public CueCatalog Catalog => catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;

        // Null-safe shortcuts for game code. Nothing plays while no host runs (dedicated server, before setup)
        // or for an empty reference, and nothing is recorded as rejected for those.
        public static AudioHandle Play(CueReference cue, UnityEngine.Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
            Ready(cue) ? Instance.Engine.Play(cue, owner, fadeInSeconds, file, line) : default;

        public static AudioHandle PlayAt(CueReference cue, Vector3 position, UnityEngine.Object owner = null, float fadeInSeconds = 0,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) =>
            Ready(cue) ? Instance.Engine.PlayAt(cue, position, owner, fadeInSeconds, file, line) : default;

        private static bool Ready(CueReference cue) => Instance != null && Instance.Engine != null && !string.IsNullOrEmpty(cue.Id);

        private void OnEnable()
        {
#if UNITY_SERVER
            enabled = false;
            return;
#else
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("A CueAudioHost already exists. This duplicate host is disabled.", this);
                enabled = false;
                return;
            }
            try { Engine = new CueAudioEngine(catalog, transform, maxVoices); }
            catch (Exception exception)
            {
                Debug.LogError("CueAudio initialization failed:\n" + exception.Message, this);
                enabled = false;
                return;
            }
            Instance = this;
            if (persistAcrossScenes)
            {
                if (transform.parent == null) DontDestroyOnLoad(gameObject);
                else Debug.LogWarning("Place CueAudioHost at the scene root to persist across scenes.", this);
            }
#endif
        }

        private void Update() => Engine?.Tick();
        private void OnDisable()
        {
            Engine?.Dispose();
            Engine = null;
            if (Instance == this) Instance = null;
        }
    }
}
