using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Idempotent project configuration (02_TECH §3). Batchmode: -executeMethod UltimoMiglio.EditorTools.ProjectSetup.Apply</summary>
    public static class ProjectSetup
    {
        public const string AppId = "com.velaefigli.ultimomiglio";

        [MenuItem("Ultimo Miglio/Setup/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Vela & Figli"; PlayerSettings.productName = "Ultimo Miglio";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
            PlayerSettings.bundleVersion = "0.1.0"; PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto; // highest installed (Play requirement)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.renderOutsideSafeArea = true; // edge-to-edge, safe area handled in UI
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.Android.minifyRelease = true;
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            // New Input System only
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var p = so.FindProperty("activeInputHandler"); if (p != null) { p.intValue = 1; so.ApplyModifiedProperties(); }
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EnsureUrp();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Android/URP project settings applied");
        }

        /// <summary>Creates a mobile URP asset + renderer if none assigned.</summary>
        public static void EnsureUrp()
        {
            const string dir = "Assets/_Game/Settings", path = dir + "/URP_Mobile.asset", rpath = dir + "/URP_Mobile_Renderer.asset";
            Directory.CreateDirectory(dir);
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (!urp)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(rd, rpath);
                urp = UniversalRenderPipelineAsset.Create(rd); AssetDatabase.CreateAsset(urp, path);
                urp.supportsHDR = false; urp.msaaSampleCount = 1; urp.renderScale = 0.75f; urp.shadowDistance = 25f; urp.shadowCascadeCount = 1;
            }
            GraphicsSettings.defaultRenderPipeline = urp;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = urp; }
        }
    }
}
