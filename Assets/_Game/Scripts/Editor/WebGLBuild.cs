using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    /// <summary>
    /// Build WebGL per CI/batchmode, compatibile con GitHub Pages.
    /// Uso: Unity -batchmode -nographics -projectPath . -buildTarget WebGL
    ///        -executeMethod UltimoMiglio.EditorTools.WebGLBuild.BuildCI -outputPath Builds/WebGL -logFile Logs/webgl.log
    /// Argomenti opzionali: -outputPath DIR, -development, -skipSceneRebuild.
    /// Esce con codice 0 se la build riesce, 1 altrimenti.
    /// </summary>
    public static class WebGLBuild
    {
        const string DefaultOut = "Builds/WebGL";
        const string TemplatePath = "Assets/_Game/Scripts/Editor/WebTemplate/index.html.txt";

        public static void BuildCI()
        {
            int code = 1;
            try { code = Build(Arg("-outputPath") ?? DefaultOut, HasArg("-development"), !HasArg("-skipSceneRebuild")) ? 0 : 1; }
            catch (Exception e) { Debug.LogError("[WebGLBuild] eccezione: " + e); }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("Ultimo Miglio/Build/WebGL (GitHub Pages)")]
        public static void BuildFromMenu() => Build(DefaultOut, false, true);

        public static bool Build(string outDir, bool development, bool rebuildScene)
        {
            if (rebuildScene)
            {
                // Riusa la preparazione esistente (URP Forward, shader inclusi, HUD) senza duplicarla.
                ProjectSetup.EnsureUrp();
                var t = Type.GetType("UltimoMiglio.UI.EditorTools.HudSetup, UltimoMiglio.UI.Editor");
                t?.GetMethod("BuildAll")?.Invoke(null, null);
                SliceSceneBuilder.Build();
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = development;

            // GitHub Pages non invia Content-Encoding per .br/.gz: niente compressione.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SliceSceneBuilder.ScenePath },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            });

            bool ok = report.summary.result == BuildResult.Succeeded;
            if (ok && File.Exists(TemplatePath))
            {
                File.Copy(TemplatePath, Path.Combine(outDir, "index.html"), true);
                var td = Path.Combine(outDir, "TemplateData");
                if (Directory.Exists(td)) Directory.Delete(td, true);
            }
            if (ok) File.WriteAllText(Path.Combine(outDir, ".nojekyll"), "");

            foreach (var st in report.steps)
                foreach (var m in st.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        Debug.Log("[WebGLBuild][errore] " + st.name + " :: " + m.content.Replace("\n", " | "));
            Debug.Log($"[WebGLBuild] esito={report.summary.result} errori={report.summary.totalErrors} dimensione={report.summary.totalSize} out={outDir}");
            return ok;
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
        static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    }
}
