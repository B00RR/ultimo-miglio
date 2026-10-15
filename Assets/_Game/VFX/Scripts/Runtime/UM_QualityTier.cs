// Ultimo Miglio - VFX side of the 3 quality tiers of 02_TECH §4 (Base / Standard / Alto).
// Tech Lead's tier selector calls UM_QualityTier.Apply(tier) at boot.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX
{
    public enum UMTier { Base, Standard, Alto }

    public static class UM_QualityTier
    {
        public static UMTier Current { get; private set; } = UMTier.Standard;
        /// Particle emission multiplier used by UM_FxFactory.
        public static float ParticleScale => Current == UMTier.Base ? 0.5f : Current == UMTier.Standard ? 0.75f : 1f;

        public static void Apply(UMTier tier, Material[] toonMaterials = null)
        {
            Current = tier;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = tier == UMTier.Base ? 0.6f : tier == UMTier.Standard ? 0.75f : 1f;
                urp.shadowDistance = tier == UMTier.Alto ? 25f : 0f;  // 02_TECH: no shadows on Base/Standard
                urp.msaaSampleCount = tier == UMTier.Alto ? 2 : 1;
            }
            var sun = RenderSettings.sun;
            if (sun != null) sun.shadows = tier == UMTier.Alto ? LightShadows.Soft : LightShadows.None;

            if (toonMaterials != null)
                foreach (var m in toonMaterials)
                    if (m) m.SetShaderPassEnabled("SRPDefaultUnlit", tier == UMTier.Alto && m.HasProperty("_OutlineOn") && m.GetFloat("_OutlineOn") > 0.5f); // per-material toggle, Alto only

            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.profile && v.profile.TryGet(out Bloom b)) b.active = tier != UMTier.Base;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var d = cam.GetUniversalAdditionalCameraData();
                d.renderPostProcessing = true;
                d.antialiasing = tier == UMTier.Base ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing;
            }
        }
    }
}
