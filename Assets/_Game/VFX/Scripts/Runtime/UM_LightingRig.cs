// Ultimo Miglio - warm "Diorama Solare" lighting. Put on the scene's sun (Directional Light).
// Applies sun, ambient gradient, fog. Day/Night presets (night = same palette, warm & dark, no neon).
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimoMiglio.VFX
{
    [ExecuteAlways, RequireComponent(typeof(Light))]
    public class UM_LightingRig : MonoBehaviour
    {
        public enum Preset { Day, Golden, Night }
        public Preset preset = Preset.Day;
        [Range(0, 1)] public float blendToNight = 0f; // driven by day cycle (DayCycle -> SetNightBlend)

        // Sun from upper-left of the 3/4 ortho camera, shadows fall toward lower-right (as in conceptA)
        public Vector3 sunEulerDay = new Vector3(50f, -35f, 0f);
        public Vector3 sunEulerNight = new Vector3(35f, 150f, 0f);

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        public void SetNightBlend(float t) { blendToNight = Mathf.Clamp01(t); Apply(); }

        public void Apply()
        {
            var sun = GetComponent<Light>();
            sun.type = LightType.Directional;
            float n = preset == Preset.Night ? 1f : blendToNight;
            Color dayCol = preset == Preset.Golden ? UM_Palette.GialloSole : UM_Palette.Sole;
            float dayInt = preset == Preset.Golden ? 0.95f : 1.05f;

            sun.color = Color.Lerp(dayCol, UM_Palette.LunaCalda, n);
            sun.intensity = Mathf.Lerp(dayInt, 0.5f, n); // night ~45-50% of day with ambient; lanterns = key light on characters
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(sunEulerDay), Quaternion.Euler(sunEulerNight), n);
            sun.shadows = LightShadows.Soft;          // PCF low; toon edge done in shader. UM_QualityTier disables on Base/Standard
            sun.shadowStrength = Mathf.Lerp(0.6f, 0.45f, n);
            sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = Color.Lerp(UM_Palette.AmbSky, UM_Palette.NottSky, n);
            RenderSettings.ambientEquatorColor = Color.Lerp(UM_Palette.AmbEquator, UM_Palette.NottEq, n);
            RenderSettings.ambientGroundColor  = Color.Lerp(UM_Palette.AmbGround, UM_Palette.NottGround, n);

            // Linear fog = warm haze on the distant OmniFreight tower / bay (cheap on mobile)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(UM_Palette.Cielo, UM_Palette.NottEq, n);
            RenderSettings.fogStartDistance = 45f; // lighter: OmniFreight tower stays readable
            RenderSettings.fogEndDistance = 140f;
        }
    }
}
