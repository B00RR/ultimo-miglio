using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    public static class WebGLMinBuild
    {
        public static void Build()
        {
            ProjectSetup.EnsureUrp(); AssetDatabase.SaveAssets();
            SliceSceneBuilder.Build();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = false;
            try { PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize); } catch { }
            try { UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.BuildTimes; } catch { }
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);
            var outDir = "Builds/WebGL_min";
            Directory.CreateDirectory(outDir);
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { SliceSceneBuilder.ScenePath }, locationPathName = outDir, target = BuildTarget.WebGL, options = BuildOptions.None });
            Debug.Log($"[WebGLMin] {r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize}");
            EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
