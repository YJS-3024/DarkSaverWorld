using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace DarkSaver.EditorTools
{
    // Inert unless Unity is launched with -darkSaverMemoryCase OFF or ON.
    // Does not rely on AI/MCP, enter Play, save scenes, or load project assets.
    [InitializeOnLoad]
    public static class MemoryComparisonProbe
    {
        [Serializable] private sealed class Sample
        {
            public string caseName, utc;
            public int pid, phase, assistantAssemblies, commandAssemblies, totalAssemblies;
            public double readySeconds, uptimeSeconds, privateMB, workingSetMB;
            public long monoUsedMB, monoHeapMB, nativeAllocatedMB;
            public bool playing, compilationFailed;
        }
        private static readonly string caseName;
        private static double readyAt = -1;
        private static int phase;
        private static readonly double[] delays = { 0, 15, 45 };

        static MemoryComparisonProbe()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-darkSaverMemoryCase" && (args[i + 1] == "OFF" || args[i + 1] == "ON")) caseName = args[i + 1];
            if (caseName != null) EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (phase >= delays.Length) { EditorApplication.update -= Tick; return; }
            if (readyAt < 0) readyAt = EditorApplication.timeSinceStartup;
            double elapsed = EditorApplication.timeSinceStartup - readyAt;
            if (elapsed < delays[phase]) return;
            GC.Collect(); // Same post-GC sampling policy in both cases; no asset unload or heap trim.
            using (var process = System.Diagnostics.Process.GetCurrentProcess()) {
                process.Refresh();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var sample = new Sample {
                    caseName = caseName, utc = DateTime.UtcNow.ToString("o"), pid = process.Id, phase = phase,
                    readySeconds = elapsed, uptimeSeconds = EditorApplication.timeSinceStartup,
                    privateMB = process.PrivateMemorySize64 / 1048576.0, workingSetMB = process.WorkingSet64 / 1048576.0,
                    monoUsedMB = Profiler.GetMonoUsedSizeLong() / 1048576,
                    monoHeapMB = Profiler.GetMonoHeapSizeLong() / 1048576,
                    nativeAllocatedMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576,
                    playing = EditorApplication.isPlaying, compilationFailed = EditorUtility.scriptCompilationFailed,
                    totalAssemblies = assemblies.Length
                };
                foreach (var assembly in assemblies) {
                    string name = assembly.GetName().Name;
                    if (name.StartsWith("Unity.AI.Assistant") || name.StartsWith("Unity.AI.MCP")) sample.assistantAssemblies++;
                    if (name.StartsWith("Unity.AI.Assistant.RunCommand.Dynamic")) sample.commandAssemblies++;
                }
                string folder = "Tools/MemoryDiagnostics/AB-20261019";
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, caseName + ".jsonl"), JsonUtility.ToJson(sample) + Environment.NewLine);
                Debug.Log("MemoryComparisonProbe " + caseName + " phase=" + phase + " monoUsedMB=" + sample.monoUsedMB + " monoHeapMB=" + sample.monoHeapMB);
            }
            phase++;
            if (phase >= delays.Length) EditorApplication.update -= Tick;
        }
    }
}
