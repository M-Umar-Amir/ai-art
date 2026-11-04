using System;
using System.Collections.Generic;
using System.IO;
using DallasDash.Core;
using Unity.Profiling;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Opt-in real-time profiling, independent of fixed-time video export.
    [DefaultExecutionOrder(10000)]
    public class DashPerformance : MonoBehaviour
    {
        string destination;
        readonly List<float> frames = new(12000);
        ProfilerRecorder allocations, batches;
        long peakAllocation, peakBatches;
        double total;
        Quaternion previousRotation; float previousFov; bool cameraReady;
        float maxCameraAngle, maxFovChange; int invalidCameraFrames;
        long batchTotal;
        bool inspected;

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
            if (!inspected && gm.Distance > 180f)
            {
                inspected = true;
                var counts = new Dictionary<string,int>();
                foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (renderer.enabled && renderer.isVisible) foreach (var material in renderer.sharedMaterials)
                        if (material) { counts.TryGetValue(material.name, out int count); counts[material.name] = count + 1; }
                var sorted = new List<KeyValuePair<string,int>>(counts); sorted.Sort((a,b) => b.Value.CompareTo(a.Value));
                var message = new System.Text.StringBuilder("[DashPerformance] Visible material groups: ");
                for (int i=0; i<Math.Min(24,sorted.Count);i++) message.Append(sorted[i].Key).Append('=').Append(sorted[i].Value).Append("; ");
                Debug.Log(message.ToString());
            }
            float milliseconds = Time.unscaledDeltaTime * 1000f;
            frames.Add(milliseconds); total += milliseconds;
            if (allocations.Valid) peakAllocation = Math.Max(peakAllocation, allocations.LastValue);
            if (batches.Valid) { peakBatches = Math.Max(peakBatches, batches.LastValue); batchTotal += batches.LastValue; }
            var cam = Camera.main;
            if (cam)
            {
                var position = cam.transform.position;
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) invalidCameraFrames++;
                if (cameraReady)
                {
                    maxCameraAngle = Mathf.Max(maxCameraAngle, Quaternion.Angle(previousRotation, cam.transform.rotation));
                    maxFovChange = Mathf.Max(maxFovChange, Mathf.Abs(previousFov - cam.fieldOfView));
                }
                previousRotation = cam.transform.rotation; previousFov = cam.fieldOfView; cameraReady = true;
            }
        }
        [Serializable] class Report
        {
            public int frames, width, height, quality;
            public double averageFps, medianMs, p95Ms, p99Ms, worstMs;
            public long peakGcBytes, peakBatches;
            public double averageBatches;
            public float maxCameraAnglePerFrame, maxFovChangePerFrame, renderScale;
            public int invalidCameraFrames;
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
                peakBatches = peakBatches, averageBatches = frames.Count > 0 ? (double)batchTotal / frames.Count : 0,
                renderScale = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset pipeline ? pipeline.renderScale : 1f,
                maxCameraAnglePerFrame = maxCameraAngle, maxFovChangePerFrame = maxFovChange, invalidCameraFrames = invalidCameraFrames, gcCounterAvailable=allocations.Valid, batchCounterAvailable=batches.Valid, gpu = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType,
                note = "Real-time built-player samples after 2s warmup. Includes streaming. Fixed-time movie frames excluded. Desktop result does not establish mobile performance."
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)));
            File.WriteAllText(destination, JsonUtility.ToJson(report, true));
            Debug.Log("[DashPerformance] " + JsonUtility.ToJson(report));
        }
        void OnDestroy() { allocations.Dispose(); batches.Dispose(); }
    }
}
