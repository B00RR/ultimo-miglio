using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Batchmode builds (ci/build.sh). Outputs in Builds/:
    ///  release AAB  UltimoMiglio_release.aab   (BuildAab)
    ///  release APK  UltimoMiglio_release.apk   (BuildApk)
    ///  dev APK      UltimoMiglio_dev.apk       (BuildDevApk: Development + Autoconnect Profiler + Script Debugging; QA log to logcat)
    /// Signing: env UM_KEYSTORE, UM_KEYSTORE_PASS, UM_KEY_ALIAS, UM_KEY_PASS (release only; unsigned/debug key otherwise).</summary>
    public static class BuildScript
    {
        const string Out = "Builds";
        static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).DefaultIfEmpty(SliceSceneBuilder.ScenePath).ToArray();

        [MenuItem("Ultimo Miglio/Build/Release AAB")] public static void BuildAab() => Build(true, false, "UltimoMiglio_release.aab");
        [MenuItem("Ultimo Miglio/Build/Release APK")] public static void BuildApk() => Build(false, false, "UltimoMiglio_release.apk");
        [MenuItem("Ultimo Miglio/Build/Development APK")] public static void BuildDevApk() => Build(false, true, "UltimoMiglio_dev.apk");
        static bool _batchAll;
        public static void BuildAll() { _batchAll = true; BuildDevApk(); BuildApk(); _batchAll = false; BuildAab(); }

        static void Build(bool aab, bool dev, string file)
        {
            ProjectSetup.Apply();
            if (!File.Exists(SliceSceneBuilder.ScenePath)) SliceSceneBuilder.Build();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = aab;
            EditorUserBuildSettings.development = dev;
            EditorUserBuildSettings.allowDebugging = dev;
            EditorUserBuildSettings.connectProfiler = dev;
            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android, dev ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
            PlayerSettings.Android.useCustomKeystore = !dev && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UM_KEYSTORE"));
            if (PlayerSettings.Android.useCustomKeystore)
            {
                PlayerSettings.Android.keystoreName = Environment.GetEnvironmentVariable("UM_KEYSTORE");
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("UM_KEYSTORE_PASS");
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("UM_KEY_ALIAS");
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("UM_KEY_PASS");
            }
            var opts = BuildOptions.None;
            if (dev) opts |= BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging;
            Directory.CreateDirectory(Out);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = Path.Combine(Out, file), target = BuildTarget.Android, options = opts });
            var s = report.summary;
            Debug.Log($"[Build] {file}: {s.result} size={s.totalSize / 1048576f:0.0} MB time={s.totalTime} errors={s.totalErrors}");
            if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
            if (!_batchAll && Application.isBatchMode) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
