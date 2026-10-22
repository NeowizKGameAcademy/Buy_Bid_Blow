using System;
using System.IO;
using System.Linq;
using BuyBidBlow.Network;
using BuyBidBlow.UI.Lobby;
using Mirror;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace BuyBidBlow.Dev.Editor
{
    // Dedicated server: SC_Server holds only the network manager, built as a console app (Server subtarget).
    // Client: menu and the shared lobby/game hall as a normal Windows player.
    public static class GameBuild
    {
        [InitializeOnLoadMethod] static void WatchRebuildRequest()=>EditorApplication.update+=CheckRebuildRequest;
        static void CheckRebuildRequest()
        {
            const string request="Library/RebuildServer.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);
            try{BuildAndRunServer();File.WriteAllText("Library/RebuildServer.result","PASS");}
            catch(Exception e){File.WriteAllText("Library/RebuildServer.result",e.ToString());Debug.LogException(e);}
            finally{EditorUserBuildSettings.standaloneBuildSubtarget=StandaloneBuildSubtarget.Player;}
        }
        const string LobbyScene = LobbyBuilder.ScenePath;
        const string ServerScene = "Assets/Project/Scenes/SC_Server.unity";
        const string Output = "Builds/Server/BuyBidBlowServer.exe";
        const string ClientOutput = "Builds/Client/BuyBidBlow.exe";
        // Build & Run Server starts the console server with these; add e.g. -roundSeconds 5 for quick rounds.
        const string RunArguments = "-minPlayers 1";

        // Copies the lobby's network manager so both sides share transport, player prefab and settings.
        [MenuItem("Tools/Buy Bid Blow/Build/Update Server Scene")]
        public static void UpdateServerScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Reopen whatever was open in the Editor afterwards (saved scenes only).
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var server = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lobby = EditorSceneManager.OpenScene(LobbyScene, OpenSceneMode.Additive);
            try
            {
                var source = lobby.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameNetworkManager>(true)).Single();
                var copy = UnityEngine.Object.Instantiate(source.gameObject);
                copy.name = "Server Network";
                UnityEngine.Object.DestroyImmediate(copy.GetComponent<ConnectionController>());
                copy.GetComponent<GameNetworkManager>().headlessStartMode = HeadlessStartOptions.AutoStartServer;
                EditorSceneManager.MoveGameObjectToScene(copy, server);
            }
            finally { EditorSceneManager.CloseScene(lobby, true); }
            EditorSceneManager.SaveScene(server, ServerScene);
            Debug.Log("Server scene saved: " + ServerScene);
            if (!Application.isBatchMode && previous.Length > 0 && previous.All(p => !string.IsNullOrEmpty(p.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }

        [MenuItem("Tools/Buy Bid Blow/Build/Dedicated Server (Windows)")]
        public static void Build() => BuildServer(true);

        // Stop the running server, rebuild it and start it again; then press Play on SC_MainMenu and join 127.0.0.1.
        [MenuItem("Tools/Buy Bid Blow/Build/Build & Run Server")]
        public static void BuildAndRunServer()
        {
            StopServer();
            BuildServer(false);
            var exe = Path.GetFullPath(Output);
            // "-logFile -" prints the log to its own console window; closing that window stops the server.
            var arguments=RunArguments+" -batchmode -nographics -logFile -";
            Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) });
            Debug.Log($"Server started: {exe} {RunArguments}");
        }

        [MenuItem("Tools/Buy Bid Blow/Build/Stop Server")]
        public static void StopServer()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Output)))
            {
                try { process.Kill(); process.WaitForExit(5000); Debug.Log("Server stopped."); }
                catch (Exception e) { Debug.LogWarning("Could not stop the server: " + e.Message); }
            }
        }

        static void BuildServer(bool reveal)
        {
            // Always refresh so the server gets the lobby manager's current settings (match rules, player prefab).
            UpdateServerScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ServerScene },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Server build failed: " + report.summary.result);
            Debug.Log($"Server build: {Path.GetFullPath(Output)} ({report.summary.totalSize / (1024 * 1024)} MB)");
            if (reveal && !Application.isBatchMode) EditorUtility.RevealInFinder(Output);
        }

        [MenuItem("Tools/Buy Bid Blow/Build/Client (Windows)")]
        public static void BuildClient()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && s.path != ServerScene).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ClientOutput,
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Client build failed: " + report.summary.result);
            Debug.Log($"Client build: {Path.GetFullPath(ClientOutput)} ({string.Join(", ", scenes.Select(Path.GetFileNameWithoutExtension))})");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(ClientOutput);
        }

        public static void BuildBatch()
        {
            try { Build(); BuildClient(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
