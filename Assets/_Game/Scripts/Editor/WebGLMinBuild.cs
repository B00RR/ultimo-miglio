using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.EditorTools
{
    public static class WebGLMinBuild
    {
        public static void Build()
        {
            ProjectSetup.EnsureUrp();
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp)
            {
                urp.supportsHDR = false; urp.msaaSampleCount = 1; urp.renderScale = 1f;
                // Evita StencilDitherMaskSeed su WebGL/SwiftShader
                try
                {
                    var uso = new SerializedObject(urp);
                    var lod = uso.FindProperty("m_EnableLODCrossFade"); if (lod != null) lod.boolValue = false;
                    var lodt = uso.FindProperty("m_LODCrossFadeDitheringType"); if (lodt != null) lodt.intValue = 0;
                    uso.ApplyModifiedPropertiesWithoutUndo();
                }
                catch { }
                EditorUtility.SetDirty(urp);
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Game/Settings/URP_Mobile_Renderer.asset");
                if (rd)
                {
                    rd.renderingMode = RenderingMode.Forward;
                    rd.intermediateTextureMode = IntermediateTextureMode.Auto;
                    // Azzera debug PS (HDRDebugView) — altrimenti Unity li carica comunque
                    var rso = new SerializedObject(rd);
                    var dbg = rso.FindProperty("debugShaders");
                    if (dbg != null)
                    {
                        var hdr = dbg.FindPropertyRelative("hdrDebugViewPS"); if (hdr != null) hdr.objectReferenceValue = null;
                        var rep = dbg.FindPropertyRelative("debugReplacementPS"); if (rep != null) rep.objectReferenceValue = null;
                    }
                    rso.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(rd);
                }
            }
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]; var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            // Solo shader di gioco/URP base. NON includere Hidden/*: su SwiftShader "not supported" = ERROR in console.
            var keep = new[] {
                "UltimoMiglio/ToonDiorama",
                "UltimoMiglio/UI/ZoneRing",
                "UltimoMiglio/FX/RingFill",
                "UltimoMiglio/FX/SkyGradient",
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Particles/Unlit",
            };
            foreach (var n in keep)
            {
                var sh = Shader.Find(n); if (!sh) continue;
                bool has = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
                if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
            }
            // Rimuovi Hidden forzati in sessioni precedenti
            for (int i = arr.arraySize - 1; i >= 0; i--)
            {
                var sh = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (sh != null && sh.name.StartsWith("Hidden/"))
                    arr.DeleteArrayElementAtIndex(i);
            }
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
            foreach (var st in r.steps) foreach (var msg in st.messages) if (msg.type == LogType.Error || msg.type == LogType.Exception) Debug.Log("[WebGLMin][err] step=" + st.name + " :: " + msg.content.Replace("\n", " | "));
            Debug.Log($"[WebGLMin] {r.summary.result} errors={r.summary.totalErrors} size={r.summary.totalSize}");
            EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
