using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace BuyBidBlow.Dev.Editor
{
    // Leak hunt: while in play mode, every 20s appends memory totals and the object types whose count or native size
    // grew to Logs/MemoryProbe.log. The snapshot menu writes a .snap for the Memory Profiler window.
    [InitializeOnLoad]
    public static class MemoryLeakProbe
    {
        const string LogPath = "Logs/MemoryProbe.log";
        const string EnabledKey = "BuyBidBlow.MemoryLeakProbe";
        const double Interval = 20;
        const long StopAtBytes = 12L * 1024 * 1024 * 1024;
        static double next;
        static Dictionary<string, (int count, long bytes)> previous;
        static ProfilerRecorder systemUsed;

        static MemoryLeakProbe()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                previous = null; next = 0;
                if (Enabled) Append($"\n===== Play started {DateTime.Now:HH:mm:ss} =====");
            };
        }

        static bool Enabled { get => EditorPrefs.GetBool(EnabledKey, true); set => EditorPrefs.SetBool(EnabledKey, value); }

        [MenuItem("Tools/Buy Bid Blow/Memory/Log Growth In Play Mode")]
        static void Toggle() => Enabled = !Enabled;
        [MenuItem("Tools/Buy Bid Blow/Memory/Log Growth In Play Mode", true)]
        static bool ToggleCheck() { Menu.SetChecked("Tools/Buy Bid Blow/Memory/Log Growth In Play Mode", Enabled); return true; }

        [MenuItem("Tools/Buy Bid Blow/Memory/Take Snapshot")]
        static void TakeSnapshot()
        {
            Directory.CreateDirectory("MemoryCaptures");
            var path = Path.GetFullPath($"MemoryCaptures/Leak_{DateTime.Now:HHmmss}.snap");
            Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(path, (file, ok) => Debug.Log(ok ? "Memory snapshot: " + file : "Memory snapshot failed"));
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || !Enabled || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + Interval;
            var current = new Dictionary<string, (int count, long bytes)>();
            foreach (var o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                var key = o.GetType().Name;
                current.TryGetValue(key, out var v);
                current[key] = (v.count + 1, v.bytes + Profiler.GetRuntimeMemorySizeLong(o));
            }
            const double MB = 1024 * 1024;
            // Process.PrivateMemorySize64 reads 0 under Unity's Mono; this counter is what the OS reports for the editor.
            if (!systemUsed.Valid) systemUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            long committed = systemUsed.LastValue;
            var report = new StringBuilder();
            report.AppendLine($"[{DateTime.Now:HH:mm:ss}] frame {Time.frameCount} · process {committed / MB:N0}MB · allocated {Profiler.GetTotalAllocatedMemoryLong() / MB:N0}MB · reserved {Profiler.GetTotalReservedMemoryLong() / MB:N0}MB · mono {Profiler.GetMonoUsedSizeLong() / MB:N0}MB · gfx {Profiler.GetAllocatedMemoryForGraphicsDriver() / MB:N0}MB · objects {current.Values.Sum(v => v.count)}");
            if (previous != null)
                foreach (var (type, now, delta) in current
                    .Select(p => (p.Key, p.Value, d: (count: p.Value.count - (previous.TryGetValue(p.Key, out var b) ? b.count : 0), bytes: p.Value.bytes - b.bytes)))
                    .Where(t => t.d.count > 0 || t.d.bytes > 1024 * 1024)
                    .OrderByDescending(t => t.d.bytes).Take(15))
                    report.AppendLine($"    {type}: {now.count} (+{delta.count}), {now.bytes / MB:N1}MB (+{delta.bytes / MB:N1}MB)");
            previous = current;
            Append(report.ToString().TrimEnd());
            // The leak has exhausted the pagefile and frozen the PC; leave play mode well before that.
            if (committed > StopAtBytes)
            {
                Append($"    Stopped play: process memory over {StopAtBytes / MB:N0}MB");
                Debug.LogWarning("MemoryLeakProbe stopped play mode: editor process memory passed 12GB.");
                EditorApplication.isPlaying = false;
            }
        }

        static void Append(string text)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(LogPath, text + "\n");
        }
    }
}
