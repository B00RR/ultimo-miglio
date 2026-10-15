using UnityEditor; using UnityEditor.Build; using UnityEditor.Build.Reporting; using UnityEngine;
namespace UltimoMiglio.EditorTools
{
    public static class LinuxCaptureBuild
    {
        public static void Build()
        {
            ProjectSetup.EnsureUrp();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { SliceSceneBuilder.ScenePath }, locationPathName = "Builds/LinuxCapture/um.x86_64", target = BuildTarget.StandaloneLinux64, options = BuildOptions.None });
            Debug.Log("[LinuxCapture] " + r.summary.result);
            EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
