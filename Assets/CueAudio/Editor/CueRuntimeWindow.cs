using System.IO;
using UnityEditor;
using UnityEngine;

namespace CueAudio.Editor
{
    public sealed class CueRuntimeWindow : EditorWindow
    {
        private Vector2 scroll;
        private string filter = "";

        [MenuItem("Tools/CueAudio/Runtime Monitor")]
        public static void ShowWindow() => GetWindow<CueRuntimeWindow>("Cue Runtime");
        private void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }

        private void OnGUI()
        {
            var host = CueAudioHost.Instance;
            if (!Application.isPlaying || host == null || host.Engine == null)
            { EditorGUILayout.HelpBox("Enter Play mode with a configured CueAudioHost to see playback requests.", MessageType.Info); return; }
            var engine = host.Engine;
            EditorGUILayout.ObjectField("Host", host, typeof(CueAudioHost), true);
            EditorGUILayout.LabelField($"Active: {engine.ActiveCount}    Pool: {engine.PoolSize}", EditorStyles.boldLabel);
            engine.RecordHistory = EditorGUILayout.Toggle("Record requests (last 200)", engine.RecordHistory);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop All")) engine.StopAll(0.1f);
            if (GUILayout.Button("Clear History")) engine.ClearHistory();
            EditorGUILayout.EndHorizontal();
            foreach (var bus in engine.Catalog.Buses)
            {
                EditorGUI.BeginChangeCheck();
                float volume = EditorGUILayout.Slider(bus.id, engine.GetBusVolume(bus.id), 0, 1);
                if (EditorGUI.EndChangeCheck()) engine.SetBusVolume(bus.id, volume);
            }
            filter = EditorGUILayout.TextField("Filter cue", filter);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Playing", EditorStyles.boldLabel);
            foreach (var voice in engine.GetActiveVoices())
            {
                if (!Matches(voice.CueId)) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(voice.CueId);
                EditorGUILayout.ObjectField(voice.Owner, typeof(Object), true, GUILayout.Width(140));
                if (GUILayout.Button("Stop", GUILayout.Width(50))) voice.Handle.Stop(0.1f);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Requests (newest first)", EditorStyles.boldLabel);
            for (int i = engine.History.Count - 1; i >= 0; i--)
            {
                var record = engine.History[i];
                if (!Matches(record.CueId)) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{record.Time:F2}  {record.CueId}  —  {record.Result}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Clip: {record.Clip ?? "—"}    Owner: {record.OwnerName}");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Owner") && record.Owner != null) { Selection.activeObject = record.Owner; EditorGUIUtility.PingObject(record.Owner); }
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(record.File) || !File.Exists(record.File)))
                    if (GUILayout.Button(Path.GetFileName(record.File) + ":" + record.Line))
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(record.File, record.Line);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private bool Matches(string id) => string.IsNullOrEmpty(filter) ||
            (id ?? "").IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
