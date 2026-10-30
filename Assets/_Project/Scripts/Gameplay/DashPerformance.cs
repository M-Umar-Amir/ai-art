using System;
using System.Collections.Generic;
using System.IO;
using DallasDash.Core;
using Unity.Profiling;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Opt-in real-time profiling, independent of fixed-time video export.
    public class DashPerformance : MonoBehaviour
    {
        string destination;
        readonly List<float> frames = new(12000);
        ProfilerRecorder allocations, batches;
        long peakAllocation, peakBatches;
        double total;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dash-profile")
            {
                var profile = new GameObject("Opt-in frame profiler").AddComponent<DashPerformance>();
                profile.destination = args[i + 1];
                profile.allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
                profile.batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1);
                return;
            }
        }
        void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running || gm.Paused || gm.RunTime < 2f || Time.captureFramerate != 0) return;
            float milliseconds = Time.unscaledDeltaTime * 1000f;
            frames.Add(milliseconds); total += milliseconds;
            if (allocations.Valid) peakAllocation = Math.Max(peakAllocation, allocations.LastValue);
            if (batches.Valid) peakBatches = Math.Max(peakBatches, batches.LastValue);
        }
        [Serializable] class Report
        {
            public int frames, width, height, quality;
            public double averageFps, medianMs, p95Ms, p99Ms, worstMs;
            public long peakGcBytes, peakBatches;
            public bool gcCounterAvailable, batchCounterAvailable;
            public string gpu, cpu, note;
        }
        void OnApplicationQuit()
        {
            if (frames.Count == 0 || string.IsNullOrEmpty(destination)) return;
            frames.Sort();
            float Percentile(float p) => frames[Mathf.Clamp(Mathf.CeilToInt(frames.Count * p) - 1, 0, frames.Count - 1)];
            var report = new Report {
                frames = frames.Count, width = Screen.width, height = Screen.height, quality = PlayerProfile.Graphics,
                averageFps = frames.Count * 1000d / total, medianMs = Percentile(.5f), p95Ms = Percentile(.95f),
                p99Ms = Percentile(.99f), worstMs = frames[frames.Count - 1], peakGcBytes = peakAllocation,
                peakBatches = peakBatches, gcCounterAvailable=allocations.Valid, batchCounterAvailable=batches.Valid, gpu = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType,
                note = "Real-time built-player samples after 2s warmup. Includes streaming. Fixed-time movie frames excluded. Desktop result does not establish mobile performance."
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)));
            File.WriteAllText(destination, JsonUtility.ToJson(report, true));
            Debug.Log("[DashPerformance] " + JsonUtility.ToJson(report));
        }
        void OnDestroy() { allocations.Dispose(); batches.Dispose(); }
    }
}
