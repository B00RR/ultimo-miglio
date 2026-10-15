using System.Linq;
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
            ProjectSetup.EnsureUrp();
            var urp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp) { urp.supportsHDR = false; urp.msaaSampleCount = 1; urp.renderScale = 1f; EditorUtility.SetDirty(urp);
                var rd = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/_Game/Settings/URP_Mobile_Renderer.asset");
                if (rd) { rd.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.Forward; EditorUtility.SetDirty(rd); } }
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]; var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var n in new[] { "UltimoMiglio/ToonDiorama", "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit" }) { var sh = Shader.Find(n); if (!sh) continue; bool has = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true; if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; } }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            { var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UltimoMiglio.UI.EditorTools.HudSetup")).FirstOrDefault(x => x != null); t?.GetMethod("BuildAll")?.Invoke(null, null); Debug.Log("[WebGLMin] HudSetup ran=" + (t != null)); }
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
            if (r.summary.result == BuildResult.Succeeded) { File.Copy("Assets/_Game/Scripts/Editor/WebTemplate/index.html.txt", Path.Combine(outDir, "index.html"), true); if (Directory.Exists(Path.Combine(outDir, "TemplateData"))) Directory.Delete(Path.Combine(outDir, "TemplateData"), true); }
            Debug.Log($"[WebGLMin] {r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize}");
            EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
