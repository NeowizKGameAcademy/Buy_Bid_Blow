using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CueAudio.Editor
{
    // Unity has no public edit-mode clip audition API. Keep this optional adapter isolated.
    [InitializeOnLoad]
    internal static class CuePreview
    {
        private static readonly Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayMethod = AudioUtil?.GetMethod("PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        private static readonly MethodInfo StopMethod = AudioUtil?.GetMethod("StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        static CuePreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.playModeStateChanged += _ => Stop();
            EditorApplication.quitting += Stop;
        }

        public static string Play(AudioClip clip)
        {
            if (clip == null) return "Assign an AudioClip first.";
            if (PlayMethod == null) return "Clip preview is unavailable in this Unity version. Use Play mode.";
            try { Stop(); PlayMethod.Invoke(null, new object[] { clip, 0, false }); return null; }
            catch (Exception exception) { return "Preview unavailable: " + exception.GetBaseException().Message; }
        }

        public static void Stop()
        {
            try { StopMethod?.Invoke(null, null); }
            catch (Exception) { /* Preview must never prevent reload or entering play mode. */ }
        }
    }
}
