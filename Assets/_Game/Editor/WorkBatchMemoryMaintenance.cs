using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace DarkSaver.EditorTools
{
    // Editor-only, explicit batch boundaries. Never periodically interrupts editing or Play.
    public static class WorkBatchMemoryMaintenance
    {
        private static bool queued;
        public static string LastReport { get; private set; } = "Not run";

        public static string BlockingReason()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return "Editor busy";
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return "Prefab editing stage open";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return "Unsaved scene";
            return string.Empty;
        }

        [MenuItem("DarkSaver/Tools/Clean unused memory after work batch")]
        public static void CollectIfIdle()
        {
            string reason = BlockingReason();
            if (reason.Length != 0) { LastReport = "Skipped: " + reason; Debug.Log("Batch memory: " + LastReport); return; }
            string before = Snapshot();
            // Treat managed references as roots; do not invalidate catalogs held by scripts.
            // No scene/asset saves, cache deletion, reference clearing, or process working-set trimming.
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate(true);
            GC.Collect();
            LastReport = "before [" + before + "] after [" + Snapshot() + "]";
            Debug.Log("Batch memory: " + LastReport);
        }

        public static void ScheduleAfterBatch()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += RunScheduled;
        }

        private static void RunScheduled()
        {
            queued = false;
            CollectIfIdle(); // Busy/dirty state is checked again at execution time; no retry loop.
        }

        private static string Snapshot() => "allocatedMB=" + Profiler.GetTotalAllocatedMemoryLong() / 1048576
            + " reservedMB=" + Profiler.GetTotalReservedMemoryLong() / 1048576
            + " monoUsedMB=" + Profiler.GetMonoUsedSizeLong() / 1048576
            + " monoHeapMB=" + Profiler.GetMonoHeapSizeLong() / 1048576;
    }
}
