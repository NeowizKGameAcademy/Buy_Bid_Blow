using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CueAudio.Editor
{
    [CustomPropertyDrawer(typeof(CueReference))]
    public sealed class CueReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 3 + 6;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var catalog = property.FindPropertyRelative("catalog");
            var id = property.FindPropertyRelative("id");
            float height = EditorGUIUtility.singleLineHeight;
            var row = new Rect(position.x, position.y, position.width, height);
            EditorGUI.LabelField(row, label);
            row.y += height + 2;
            EditorGUI.PropertyField(row, catalog, new GUIContent("Catalog"));
            row.y += height + 2;
            var selectedCatalog = catalog.objectReferenceValue as CueCatalog;
            var values = new List<string> { "" };
            var names = new List<string> { "(None)" };
            try
            {
                if (selectedCatalog != null)
                    foreach (var cue in selectedCatalog.ReadDocument().cues)
                        if (cue != null) { values.Add(cue.id); names.Add(cue.id); }
            }
            catch (Exception) { names[0] = "(Invalid JSON — open Cue Browser)"; }
            int index = values.IndexOf(id.stringValue);
            if (index < 0) { index = values.Count; values.Add(id.stringValue); names.Add("Missing: " + id.stringValue); }
            var dropdown = new Rect(row.x, row.y, row.width - 62, height);
            EditorGUI.showMixedValue = id.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(dropdown, "Cue", index, names.ToArray());
            if (EditorGUI.EndChangeCheck()) id.stringValue = values[next];
            EditorGUI.showMixedValue = false;
            using (new EditorGUI.DisabledScope(selectedCatalog == null || id.hasMultipleDifferentValues))
                if (GUI.Button(new Rect(row.xMax - 58, row.y, 58, height), "Browse"))
                    CueBrowserWindow.Open(selectedCatalog, id.stringValue);
            EditorGUI.EndProperty();
        }
    }

    [CustomEditor(typeof(CueCatalog))]
    public sealed class CueCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var catalog = (CueCatalog)target;
            if (GUILayout.Button("Open Cue Browser")) CueBrowserWindow.Open(catalog);
            if (GUILayout.Button("Validate Catalog"))
            {
                var errors = catalog.Validate();
                EditorUtility.DisplayDialog("CueAudio validation", errors.Count == 0 ? "Catalog is valid." : string.Join("\n", errors), "OK");
            }
        }
    }

    [CustomEditor(typeof(CueEmitter))]
    public sealed class CueEmitterInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play")) ((CueEmitter)target).Play();
                if (GUILayout.Button("Stop")) ((CueEmitter)target).Stop();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox("Connect a Button OnClick / UnityEvent to CueEmitter.Play(). Use Browse to inspect this cue's connections.", MessageType.Info);
        }
    }
}
