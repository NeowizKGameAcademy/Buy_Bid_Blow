using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev
{
    [InitializeOnLoad]
    public static class WolfAnimatorPlaybackRunner
    {
        private const string Running = "BuyBidBlow.WolfPlaybackChecks";
        private const string Result = "Library/WolfAnimatorPlayback.result";

        static WolfAnimatorPlaybackRunner()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += WatchResult;
        }

        [MenuItem("Tools/Buy Bid Blow/Wolf/Run Playback Checks")]
        public static void Run()
        {
            WolfAnimatorBuilder.Validate();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Result)) File.Delete(Result);
            if (File.Exists("Library/WolfAnimatorChecks.request")) File.Delete("Library/WolfAnimatorChecks.request");
            SessionState.SetBool(Running, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var sceneCharacters = Object.FindObjectsByType<BBBWolfCharacter>(FindObjectsSortMode.None);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Project/Characters/Wolf/WolfExpressions/PF_Wolf_Character.prefab");
                var clone = Object.Instantiate(prefab);
                clone.transform.position += Vector3.right * 4;
                var runner = new GameObject("Wolf Playback Checks").AddComponent<WolfAnimatorPlaybackChecks>();
                runner.characters = sceneCharacters.Concat(clone.GetComponentsInChildren<BBBWolfCharacter>()).ToArray();
                SessionState.SetFloat(Running + ".StartedAt", (float)EditorApplication.timeSinceStartup);
            }
            if (change == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Running, false);
        }

        private static void WatchResult()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying) return;
            if (File.Exists(Result))
            {
                Debug.Log(File.ReadAllText(Result));
                SessionState.SetBool(Running, false);
                EditorApplication.isPlaying = false;
            }
            else if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Running + ".StartedAt", (float)EditorApplication.timeSinceStartup) > 90)
            {
                File.WriteAllText(Result, "FAIL: Playback checks timed out.");
                SessionState.SetBool(Running, false);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
