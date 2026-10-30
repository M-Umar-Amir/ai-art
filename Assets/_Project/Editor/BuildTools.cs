using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Headless builds. Android: -executeMethod DallasDash.EditorTools.BuildTools.Android
    /// Output goes to Builds/Android/dash-riders.apk next to the project (outside Assets).
    public static class BuildTools
    {
        /// Quick Windows review build (Mono, no IL2CPP) for screenshots: -executeMethod DallasDash.EditorTools.BuildTools.Windows
        [MenuItem("Dallas Dash/Build Windows (review)")]
        public static void Windows()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Main.unity" },
                locationPathName = "Builds/Windows/DashRiders.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[DallasDash] Windows build {s.result}: {s.outputPath} ({s.totalErrors} errors)");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Dallas Dash/Build Android APK")]
        public static void Android()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // ARM64 for phones; x86-64 too so the test APK runs natively in the Android Emulator on a PC
            // (ARM-only builds go through the emulator's ARM translation, slow and flaky with Unity).
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            // Smaller, faster IL2CPP output: the full-speed C++ build ran this 16 GB machine out of memory.
            PlayerSettings.SetIl2CppCodeGeneration(android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Main.unity" },
                locationPathName = "Builds/Android/dash-riders.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[DallasDash] Android build {s.result}: {s.outputPath} ({s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors)");
            if (Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
