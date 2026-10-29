using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    // Client: menu and the shared lobby/game hall as a normal Windows player. A player hosts the match (Mirror host mode).
    public static class GameBuild
    {
        const string ClientOutput = "Builds/Client/BuyBidBlow.exe";

        [MenuItem("Tools/Buy Bid Blow/Build/Client (Windows)")]
        public static void BuildClient()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
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
            try { BuildClient(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
