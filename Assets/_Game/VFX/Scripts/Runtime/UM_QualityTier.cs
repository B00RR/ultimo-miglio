// Ultimo Miglio - VFX side of the 3 quality tiers of 02_TECH §4 (Base / Standard / Alto).
// Tech Lead's tier selector calls UM_QualityTier.Apply(tier) at boot.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX
{
    public enum UMTier { Base, Standard, Alto, Web }

    public static class UM_QualityTier
    {
        public static UMTier Current { get; private set; } = UMTier.Standard;
        /// Particle emission multiplier used by UM_FxFactory.
        public static float ParticleScale => Current == UMTier.Base ? 0.5f : (Current == UMTier.Standard || Current == UMTier.Web) ? 0.75f : 1f;
        /// Web: outline/shadows cheap variants allowed (desktop-class GPUs via WebGL2), DPR capped in the WebGL template.
        /// Post-process: OFF di default su Web (in WebGL ha dato schermo magenta). Abilitarlo solo dopo verifica in build.
        public static bool AllowPostOnWeb = false;
        public static bool PostProcessingEnabled { get; private set; }
        public const float ShadowDistAlto = 26f, ShadowDistWeb = 24f;
        static bool OutlineTier(UMTier t) => t == UMTier.Alto || t == UMTier.Web;

        public static void Apply(UMTier tier, Material[] toonMaterials = null)
        {
            Current = tier;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = tier == UMTier.Base ? 0.6f : tier == UMTier.Standard ? 0.75f : tier == UMTier.Web ? 0.9f : 1f;
                // Shadow distance is measured from the camera: = camera distance + play-area depth. Assumes CameraRigSetup.Distance = 15 (see README).
                urp.shadowDistance = tier == UMTier.Alto ? ShadowDistAlto : tier == UMTier.Web ? ShadowDistWeb : 0f;  // none on Base/Standard (02_TECH)
                urp.shadowCascadeCount = 1;
                if (tier == UMTier.Alto) urp.mainLightShadowmapResolution = 2048;
                if (tier == UMTier.Web) urp.mainLightShadowmapResolution = 1024;
                urp.msaaSampleCount = (tier == UMTier.Alto || tier == UMTier.Web) ? 2 : 1;  // Web: MSAA 2x (niente FXAA: il post è spento)
                urp.supportsHDR = false;
            }
            var sun = RenderSettings.sun;
            if (sun != null) sun.shadows = (tier == UMTier.Alto || tier == UMTier.Web) ? LightShadows.Soft : LightShadows.None; // soft = URP Low quality (4-tap) set in UM_LightingSetup

            if (toonMaterials != null)
                foreach (var m in toonMaterials)
                    if (m) m.SetShaderPassEnabled("SRPDefaultUnlit", OutlineTier(tier) && m.HasProperty("_OutlineOn") && m.GetFloat("_OutlineOn") > 0.5f); // per-material toggle, Alto only

            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.profile && v.profile.TryGet(out Bloom b))
                {
                    b.active = tier != UMTier.Base;
                    if (tier == UMTier.Web) { b.downscale.Override(BloomDownscaleMode.Quarter); b.maxIterations.Override(2); b.highQualityFiltering.Override(false); }
                }
            // Web: post spento (nessun Volume, niente FXAA che passa dal post) salvo opt-in esplicito e verificato.
            PostProcessingEnabled = tier != UMTier.Web || AllowPostOnWeb;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var d = cam.GetUniversalAdditionalCameraData();
                d.renderPostProcessing = PostProcessingEnabled;
                d.antialiasing = (!PostProcessingEnabled || tier == UMTier.Base) ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing;
            }
        }
    }
}
