using CueAudio;
using UnityEngine;

namespace BuyBidBlow.Dev
{
    public sealed class CueAudioDemo : MonoBehaviour
    {
        [SerializeField] private CueReference click;
        [SerializeField] private CueReference bid;
        [SerializeField] private CueReference spatial;
        [SerializeField] private CueReference musicA;
        [SerializeField] private CueReference musicB;
        [SerializeField] private CueEmitter emitter;
        private AudioHandle music;
        private float masterVolume = 0.5f;
        private string lastAction = "Ready. Open Tools > CueAudio > Runtime Monitor to inspect requests.";

        private void Start() => CueAudioHost.Instance?.Engine?.SetMasterVolume(masterVolume);

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Mathf.Min(640, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("CueAudio — Interactive Demo");
            GUILayout.Label("Synthetic test tones. Volume starts at 50%.");
            var engine = CueAudioHost.Instance != null ? CueAudioHost.Instance.Engine : null;
            if (engine == null) { GUILayout.Label("Host unavailable. Check the Console and AudioCatalog."); GUILayout.EndArea(); return; }
            GUILayout.Space(12);
            if (GUILayout.Button("UI click")) engine.Play(click, this);
            if (GUILayout.Button("Bid: random variation / 100ms cooldown")) engine.Play(bid, this);
            if (GUILayout.Button("Burst 10 requests: inspect rejected cooldowns"))
                for (int i = 0; i < 10; i++) engine.Play(bid, this);
            if (GUILayout.Button("Play through CueEmitter (serialized connection)")) emitter.Play();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3D left")) engine.PlayAt(spatial, new Vector3(-3, 0, 0), this);
            if (GUILayout.Button("3D right")) engine.PlayAt(spatial, new Vector3(3, 0, 0), this);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Music A / crossfade 1s")) music = engine.Crossfade(music, musicA, 1, this);
            if (GUILayout.Button("Music B / crossfade 1s")) music = engine.Crossfade(music, musicB, 1, this);
            if (GUILayout.Button("Fade out")) music.Stop(1);
            GUILayout.EndHorizontal();
            GUILayout.Label("Master volume: " + Mathf.RoundToInt(masterVolume * 100) + "%");
            float next = GUILayout.HorizontalSlider(masterVolume, 0, 1);
            if (!Mathf.Approximately(next, masterVolume)) { masterVolume = next; engine.SetMasterVolume(next); }
            if (GUILayout.Button("Stop all")) engine.StopAll(0.1f);
            GUILayout.Space(10);
            GUILayout.Label($"Active voices: {engine.ActiveCount}    Pooled sources: {engine.PoolSize}");
            if (engine.History.Count > 0)
            {
                var record = engine.History[engine.History.Count - 1];
                lastAction = record.CueId + " → " + record.Result;
            }
            GUILayout.Label(lastAction);
            GUILayout.Label("Cue Browser: edit settings, Listen, Save JSON, Find Connections.\nRestart Play mode after saving cue settings.");
            GUILayout.EndArea();
        }
    }
}
