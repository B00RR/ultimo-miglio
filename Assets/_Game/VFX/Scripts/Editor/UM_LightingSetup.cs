// Ultimo Miglio - one-click scene lighting: finds/creates the sun, adds UM_LightingRig, sets mobile shadow settings on the URP asset.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_LightingSetup
    {
        [MenuItem("Ultimo Miglio/VFX/Setup Warm Lighting (open scene)")]
        public static void Setup()
        {
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            if (!sun) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            var rig = sun.GetComponent<UM_LightingRig>();
            if (!rig) rig = sun.gameObject.AddComponent<UM_LightingRig>();
            rig.Apply();
            RenderSettings.skybox = null; // flat Cielo background via camera clear color
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = UM_Palette.Cielo; }

            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp)
            {
                // Mobile shadows (tier Alto default; UM_QualityTier zeroes distance on Base/Standard)
                urp.shadowDistance = 25f;          // ortho 3/4 camera over the diorama
                urp.shadowCascadeCount = 1;
                urp.mainLightShadowmapResolution = 1024;
                // Soft shadows: enable in URP asset inspector (Alto only uses them)
                urp.maxAdditionalLightsCount = 4;  // night lanterns; prefer baked
                urp.supportsHDR = false;           // LDR keeps bandwidth low; bloom works in LDR
                EditorUtility.SetDirty(urp);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sun.gameObject.scene);
            Debug.Log("[UM VFX] Warm lighting applied.");
        }
    }
}
