using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CueAudio.Editor
{
    internal sealed class CueDraft : ScriptableObject
    {
        public CueDocument document;
    }

    public sealed class CueBrowserWindow : EditorWindow
    {
        [SerializeField] private CueCatalog catalog;
        [SerializeField] private int selected;
        [SerializeField] private string filter = "";
        private CueDraft draft;
        private SerializedObject serializedDraft;
        [SerializeField] private string loadedJson;
        [SerializeField] private string pendingJson;
        private string status;
        private Vector2 listScroll, detailScroll;
        private List<CueUsage> usages;
        private List<string> errors;
        private bool bindingsExpanded;
        private AudioHandle previewHandle;
        private bool IsDirty => draft != null && JsonUtility.ToJson(draft.document, true) != loadedJson;

        [MenuItem("Tools/CueAudio/Cue Browser")]
        public static void ShowWindow() => GetWindow<CueBrowserWindow>("Cue Browser");

        public static void Open(CueCatalog catalog, string id = null)
        {
            var window = GetWindow<CueBrowserWindow>("Cue Browser");
            if (window.catalog != catalog && !window.CanDiscard()) return;
            if (window.catalog != catalog || window.draft == null) { window.catalog = catalog; window.Load(); }
            if (window.draft != null && id != null)
                window.selected = Mathf.Max(0, window.draft.document.cues.FindIndex(cue => cue.id == id));
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(780, 560);
            Undo.undoRedoPerformed += Repaint;
            if (catalog != null && !string.IsNullOrEmpty(pendingJson))
            {
                draft = CreateInstance<CueDraft>();
                draft.hideFlags = HideFlags.HideAndDontSave;
                draft.document = CueDocument.Parse(pendingJson);
                serializedDraft = new SerializedObject(draft);
                hasUnsavedChanges = IsDirty;
            }
            else Load();
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            CuePreview.Stop();
            previewHandle.Stop();
            pendingJson = draft != null ? JsonUtility.ToJson(draft.document, true) : null;
            if (draft != null) DestroyImmediate(draft);
        }

        private void Load()
        {
            if (draft != null) DestroyImmediate(draft);
            draft = null;
            serializedDraft = null;
            usages = null;
            errors = null;
            status = null;
            pendingJson = null;
            hasUnsavedChanges = false;
            if (catalog == null) return;
            try
            {
                draft = CreateInstance<CueDraft>();
                draft.hideFlags = HideFlags.HideAndDontSave;
                draft.document = catalog.ReadDocument();
                serializedDraft = new SerializedObject(draft);
                loadedJson = JsonUtility.ToJson(draft.document, true);
            }
            catch (Exception exception) { status = exception.Message; if (draft != null) DestroyImmediate(draft); draft = null; }
        }

        private bool CanDiscard() => !IsDirty || EditorUtility.DisplayDialog("Unsaved cue changes",
            "Discard the unsaved JSON edits?", "Discard", "Cancel");

        public override void SaveChanges()
        {
            if (Save()) base.SaveChanges();
        }

        private bool Save()
        {
            if (draft == null || catalog == null) return false;
            errors = catalog.Validate(draft.document);
            if (errors.Count > 0) { status = "Fix validation errors before saving."; return false; }
            string path = AssetDatabase.GetAssetPath(catalog.Definitions);
            string disk = File.ReadAllText(path);
            string normalized;
            try { normalized = JsonUtility.ToJson(CueDocument.Parse(disk), true); }
            catch (Exception) { normalized = disk; }
            if (normalized != loadedJson && !EditorUtility.DisplayDialog("JSON changed on disk",
                "The source file changed after this editor loaded it. Overwrite it with these edits?", "Overwrite", "Cancel")) return false;
            File.WriteAllText(path, JsonUtility.ToJson(draft.document, true) + "\n", new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            loadedJson = JsonUtility.ToJson(draft.document, true);
            hasUnsavedChanges = false;
            status = "Saved. Restart Play mode to load the updated catalog into the runtime engine.";
            return true;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            var next = (CueCatalog)EditorGUILayout.ObjectField(catalog, typeof(CueCatalog), false, GUILayout.MinWidth(180));
            if (EditorGUI.EndChangeCheck() && CanDiscard()) { catalog = next; Load(); }
            if (GUILayout.Button("Create Catalog", EditorStyles.toolbarButton)) CreateCatalog();
            if (GUILayout.Button("Runtime Monitor", EditorStyles.toolbarButton)) CueRuntimeWindow.ShowWindow();
            EditorGUILayout.EndHorizontal();
            if (catalog == null) { EditorGUILayout.HelpBox("Create or select a catalog. Try Tools > CueAudio > Open Demo for a ready-to-play example.", MessageType.Info); return; }

            bindingsExpanded = EditorGUILayout.Foldout(bindingsExpanded, "Catalog: JSON, clip bindings and mixer buses", true);
            if (bindingsExpanded)
            {
                using (var serialized = new SerializedObject(catalog))
                {
                    serialized.Update();
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serialized.FindProperty("definitions"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("clips"), true);
                    EditorGUILayout.PropertyField(serialized.FindProperty("buses"), true);
                    if (serialized.ApplyModifiedProperties()) { AssetDatabase.SaveAssetIfDirty(catalog); errors = null; }
                }
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(draft == null))
            {
                if (GUILayout.Button("Save JSON")) Save();
                if (GUILayout.Button("Validate"))
                {
                    errors = catalog.Validate(draft.document);
                    status = errors.Count == 0 ? "Valid." : "Validation failed.";
                }
            }
            if (GUILayout.Button("Reload JSON") && CanDiscard()) Load();
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            if (draft == null) return;
            serializedDraft.Update();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, draft.document.cues.Count - 1));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(220));
            filter = EditorGUILayout.TextField("Search", filter);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            for (int i = 0; i < draft.document.cues.Count; i++)
            {
                string id = draft.document.cues[i]?.id ?? "(null cue)";
                if (id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Toggle(i == selected, id, "Button"))
                { if (selected != i) usages = null; selected = i; }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("+ Add Cue"))
            {
                Undo.RecordObject(draft, "Add cue");
                string id = "new.cue";
                int suffix = 1;
                while (draft.document.cues.Any(cue => cue.id == id)) id = "new.cue." + suffix++;
                draft.document.cues.Add(new CueDefinition { id = id });
                selected = draft.document.cues.Count - 1;
                serializedDraft.Update();
                usages = null;
            }
            EditorGUILayout.EndVertical();
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (errors != null) foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (draft.document.cues.Count > 0) DrawCue();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            if (serializedDraft.ApplyModifiedProperties()) { usages = null; errors = null; }
            hasUnsavedChanges = IsDirty;
            saveChangesMessage = "Save changes to the CueAudio JSON file?";
        }

        private void DrawCue()
        {
            var cue = draft.document.cues[selected];
            if (cue == null)
            {
                EditorGUILayout.HelpBox("This JSON contains a null cue entry.", MessageType.Error);
                if (GUILayout.Button("Remove null entry"))
                {
                    Undo.RecordObject(draft, "Remove null cue");
                    draft.document.cues.RemoveAt(selected);
                    serializedDraft.Update();
                }
                return;
            }
            var property = serializedDraft.FindProperty("document").FindPropertyRelative("cues").GetArrayElementAtIndex(selected);
            EditorGUILayout.LabelField("Cue settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("id"));
            EditorGUILayout.HelpBox("IDs are stable keys. Renaming an ID requires updating its references; use Find Connections first.", MessageType.None);
            DrawChoice(property, "selection", new[] { "first", "random", "randomNoRepeat", "sequence" });
            DrawChoice(property, "bus", catalog.Buses.Select(bus => bus.id).ToArray());
            foreach (string field in new[] { "volume", "pitchRange", "loop", "spatialBlend", "minDistance", "maxDistance", "cooldownSeconds", "maxInstances" })
                EditorGUILayout.PropertyField(property.FindPropertyRelative(field));
            DrawChoice(property, "overflow", new[] { "reject", "stopOldest" });
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);
            var clips = property.FindPropertyRelative("clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clipId = clips.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                clipId.stringValue = EditorGUILayout.TextField(clipId.stringValue);
                catalog.TryResolve(clipId.stringValue, out var clip);
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField(clip, typeof(AudioClip), false, GUILayout.Width(160));
                if (GUILayout.Button("Listen", GUILayout.Width(50))) status = CuePreview.Play(clip);
                if (GUILayout.Button("−", GUILayout.Width(25)))
                {
                    clips.DeleteArrayElementAtIndex(i);
                    serializedDraft.ApplyModifiedProperties();
                    hasUnsavedChanges = IsDirty;
                    usages = null;
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
            var added = (AudioClip)EditorGUILayout.ObjectField("Add clip (drag here)", null, typeof(AudioClip), false);
            if (added != null) AddClip(clips, added);
            if (GUILayout.Button("Add existing clip ID")) { clips.arraySize++; clips.GetArrayElementAtIndex(clips.arraySize - 1).stringValue = ""; }
            EditorGUILayout.HelpBox("Listen auditions the original clip. To hear cue volume, pitch, routing and fades, use Play Cue with a running host. Runtime uses the catalog loaded at Play mode start.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!Application.isPlaying || CueAudioHost.Instance == null || IsDirty))
                if (GUILayout.Button("Play Cue"))
                {
                    previewHandle.Stop();
                    previewHandle = CueAudioHost.Instance.Engine.Play(new CueReference(catalog, cue.id));
                }
            if (GUILayout.Button("Stop Preview")) { CuePreview.Stop(); previewHandle.Stop(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Find Connections (scenes / prefabs / assets / code)"))
                {
                    if (string.IsNullOrWhiteSpace(cue.id)) status = "Enter a cue ID before searching.";
                    else usages = CueUsageScanner.Scan(catalog, cue.id);
                }
            if (Application.isPlaying) EditorGUILayout.HelpBox("Exit Play mode to scan scene connections. Runtime Monitor shows live requests.", MessageType.None);
            if (usages != null)
            {
                EditorGUILayout.LabelField($"{usages.Count} connections / candidates", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Serialized references and UnityEvent calls are direct connections. Code results are text candidates, including comments. Dynamically constructed IDs require the Runtime Monitor. Open scenes use current unsaved contents.", MessageType.None);
                foreach (var usage in usages)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    if (GUILayout.Button(usage.Kind + " · " + (string.IsNullOrEmpty(usage.AssetPath) ? "Unsaved scene" : usage.AssetPath), EditorStyles.linkLabel))
                        CueUsageScanner.Navigate(usage);
                    EditorGUILayout.LabelField(usage.Location, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(usage.Line > 0 ? "Line " + usage.Line : usage.Property, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Delete Cue") && EditorUtility.DisplayDialog("Delete cue", "Delete '" + cue.id + "' from this draft? Existing references will need updating.", "Delete", "Cancel"))
            {
                Undo.RecordObject(draft, "Delete cue");
                draft.document.cues.RemoveAt(selected);
                serializedDraft.Update();
                usages = null;
            }
        }

        private static void DrawChoice(SerializedProperty cue, string name, string[] choices)
        {
            var property = cue.FindPropertyRelative(name);
            var options = choices.ToList();
            int index = options.IndexOf(property.stringValue);
            if (index < 0) { index = options.Count; options.Add(property.stringValue); }
            int next = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(name), index, options.ToArray());
            property.stringValue = options[next];
        }

        private void AddClip(SerializedProperty cueClips, AudioClip clip)
        {
            string id = catalog.Clips.FirstOrDefault(binding => binding.clip == clip)?.id;
            if (id == null)
            {
                string original = clip.name;
                id = original;
                int suffix = 1;
                while (catalog.Clips.Any(binding => binding.id == id)) id = original + "_" + suffix++;
                using (var serialized = new SerializedObject(catalog))
                {
                    var bindings = serialized.FindProperty("clips");
                    int index = bindings.arraySize++;
                    bindings.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue = id;
                    bindings.GetArrayElementAtIndex(index).FindPropertyRelative("clip").objectReferenceValue = clip;
                    serialized.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
            }
            for (int i = 0; i < cueClips.arraySize; i++) if (cueClips.GetArrayElementAtIndex(i).stringValue == id) return;
            int slot = cueClips.arraySize++;
            cueClips.GetArrayElementAtIndex(slot).stringValue = id;
        }

        private void CreateCatalog()
        {
            if (!CanDiscard()) return;
            string path = EditorUtility.SaveFilePanelInProject("Create audio catalog", "AudioCatalog", "asset", "Choose a location for the catalog and its JSON.");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string jsonPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(path, ".json"));
            File.WriteAllText(jsonPath, JsonUtility.ToJson(new CueDocument(), true));
            AssetDatabase.ImportAsset(jsonPath);
            var asset = CreateInstance<CueCatalog>();
            using (var serialized = new SerializedObject(asset))
            {
                serialized.FindProperty("definitions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(asset, path);
            catalog = asset;
            selected = 0;
            Load();
        }
    }
}
