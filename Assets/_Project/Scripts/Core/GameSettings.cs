using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DallasDash.Core
{
    /// Applies the settings menu: graphics tier (render scale, shadows, anti-aliasing, post effects)
    /// and audio (volume slider and mute). Called at startup and whenever a setting changes.
    public static class GameSettings
    {
        public static readonly string[] TierNames = { "Low", "Medium", "High" };

        public static void ApplyAll()
        {
            ApplyGraphics();
            ApplyAudio();
        }

        /// Mute is the master switch; the music and effects levels are applied by GameFeel to each source.
        public static void ApplyAudio() => AudioListener.volume = PlayerProfile.Muted ? 0f : 1f;

        public static void ApplyGraphics()
        {
            int tier = PlayerProfile.Graphics;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                bool sharedGpu = SystemInfo.graphicsDeviceName.Contains("Intel(R) HD") || SystemInfo.graphicsDeviceName.Contains("UHD");
                urp.renderScale = tier switch { 0 => 0.7f, 1 => 0.85f, _ => sharedGpu ? 0.85f : 1f };
                urp.msaaSampleCount = tier == 2 ? 2 : 1;
                urp.shadowCascadeCount = tier == 2 ? 2 : 1;
                urp.shadowDistance = tier switch { 0 => 0f, 1 => 32f, _ => 65f };
            }

            var sun = RenderSettings.sun;
            if (!sun)
                foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) sun.shadows = tier switch { 0 => LightShadows.None, 1 => LightShadows.Hard, _ => LightShadows.Soft };

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.targetTexture) continue;
                if (cam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                {
                    data.renderPostProcessing = tier > 0;
                    // High uses SMAA (crisper edges on the bikes and signs than FXAA); Medium keeps the cheap FXAA.
                    data.antialiasing = tier == 2 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : tier > 0 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                    if (tier == 2) data.antialiasingQuality = AntialiasingQuality.High;
                }
                cam.farClipPlane = 240f;
            }
            RenderSettings.fogStartDistance = 95f;
            RenderSettings.fogEndDistance = 230f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
