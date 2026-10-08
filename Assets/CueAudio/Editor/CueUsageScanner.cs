using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CueAudio.Editor
{
    public sealed class CueUsage
    {
        public string AssetPath;
        public string Location;
        public string Property;
        public string Kind;
        public string GlobalId;
        public int Line;
        public Object Target;
    }

    public static class CueUsageScanner
    {
        public static List<CueUsage> Scan(CueCatalog catalog, string cueId)
        {
            var results = new List<CueUsage>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (string filter in new[] { "t:Prefab", "t:Scene", "t:ScriptableObject", "t:MonoScript" })
                foreach (string guid in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            // Include unsaved/open scenes using their current in-memory state.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                ScanScene(scene, catalog, cueId, results);
                paths.Remove(scene.path);
            }
            int progress = 0;
            try
            {
                foreach (string path in paths)
                {
                    EditorUtility.DisplayProgressBar("CueAudio: finding connections", path, (float)progress++ / paths.Count);
                    if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] lines = File.ReadAllLines(path);
                        string literal = "\"" + cueId.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                        for (int i = 0; i < lines.Length; i++)
                            if (lines[i].Contains(literal)) results.Add(new CueUsage { AssetPath = path,
                                Location = lines[i].Trim(), Kind = "Code candidate", Line = i + 1 });
                    }
                    else if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        // Preview scenes do not replace or save the user's scene setup.
                        Scene scene = default;
                        try { scene = EditorSceneManager.OpenPreviewScene(path); ScanScene(scene, catalog, cueId, results); }
                        finally { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
                    }
                    else
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null) ScanHierarchy(prefab, path, catalog, cueId, results, false);
                        else foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                            if (asset is ScriptableObject && asset != catalog)
                                ScanObject(asset, path, asset.name, catalog, cueId, results, true);
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            return results;
        }

        private static void ScanScene(Scene scene, CueCatalog catalog, string id, List<CueUsage> results)
        {
            foreach (var root in scene.GetRootGameObjects()) ScanHierarchy(root, scene.path, catalog, id, results,
                !EditorSceneManager.IsPreviewScene(scene));
        }

        private static void ScanHierarchy(GameObject root, string path, CueCatalog catalog, string id,
            List<CueUsage> results, bool keepTarget)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                string location = component.gameObject.name;
                var parent = component.transform.parent;
                while (parent != null) { location = parent.name + "/" + location; parent = parent.parent; }
                ScanObject(component, path, location + " → " + component.GetType().Name, catalog, id, results, keepTarget);
            }
        }

        private static void ScanObject(Object target, string path, string location, CueCatalog catalog, string id,
            List<CueUsage> results, bool keepTarget)
        {
            using (var serialized = new SerializedObject(target))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    bool reference = property.propertyType == SerializedPropertyType.Generic && property.type == nameof(CueReference)
                        && Matches(property, catalog, id);
                    bool eventCall = false;
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.name == "m_Target" && property.objectReferenceValue is CueEmitter emitter)
                    {
                        string prefix = property.propertyPath.Substring(0, property.propertyPath.Length - "m_Target".Length);
                        var method = serialized.FindProperty(prefix + "m_MethodName");
                        using (var emitterObject = new SerializedObject(emitter))
                            eventCall = method?.stringValue == "Play" && Matches(emitterObject.FindProperty("cue"), catalog, id);
                    }
                    if (!reference && !eventCall) continue;
                    results.Add(new CueUsage { AssetPath = path, Location = location, Property = property.propertyPath,
                        Kind = eventCall ? "UnityEvent → Play" : "Serialized reference",
                        GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString(), Target = keepTarget ? target : null });
                }
            }
        }

        private static bool Matches(SerializedProperty property, CueCatalog catalog, string id) => property != null &&
            property.FindPropertyRelative("catalog")?.objectReferenceValue == catalog &&
            property.FindPropertyRelative("id")?.stringValue == id;

        public static void Navigate(CueUsage usage)
        {
            if (usage.Line > 0)
            { AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(usage.AssetPath), usage.Line); return; }
            Object target = usage.Target;
            if (target == null && usage.AssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                var scene = SceneManager.GetSceneByPath(usage.AssetPath);
                if (!scene.isLoaded)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    EditorSceneManager.OpenScene(usage.AssetPath, OpenSceneMode.Additive);
                }
            }
            if (target == null && GlobalObjectId.TryParse(usage.GlobalId, out var globalId))
                target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
            if (target == null) target = AssetDatabase.LoadMainAssetAtPath(usage.AssetPath);
            if (target == null) return;
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            if (usage.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) AssetDatabase.OpenAsset(target);
        }
    }
}
