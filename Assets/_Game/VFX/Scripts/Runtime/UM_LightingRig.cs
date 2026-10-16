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
        [Range(0, 1)] public float blendToGolden = 0f; // pilotato da UM_DayCycle (cap. 1: giorno -> tramonto)
        [Range(0, 1)] public float blendToNight = 0f;  // notte: dal cap. 6, non usata nel cap. 1

        // Sole da alto-sinistra rispetto alla camera 3/4 (ombre in basso a destra, come conceptA)
        public Vector3 sunEulerDay = new Vector3(50f, -35f, 0f);
        public Vector3 sunEulerGolden = new Vector3(22f, -65f, 0f); // sole basso e radente al tramonto
        public Vector3 sunEulerNight = new Vector3(35f, 150f, 0f);

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        public void SetGoldenBlend(float t) { blendToGolden = Mathf.Clamp01(t); Apply(); }
        public void SetNightBlend(float t) { blendToNight = Mathf.Clamp01(t); Apply(); }

        public void Apply()
        {
            var sun = GetComponent<Light>();
            sun.type = LightType.Directional;
            float g = preset == Preset.Golden ? 1f : blendToGolden;
            float n = preset == Preset.Night ? 1f : blendToNight;
            Color dayCol = Color.Lerp(UM_Palette.Sole, UM_Palette.SoleTramonto, g);
            float dayInt = Mathf.Lerp(1.15f, 0.95f, g); // Concept: meno wash arancio, contrasto da ombre

            sun.color = Color.Lerp(dayCol, UM_Palette.LunaCalda, n);
            sun.intensity = Mathf.Lerp(dayInt, 0.5f, n);
            var dayRot = Quaternion.Slerp(Quaternion.Euler(sunEulerDay), Quaternion.Euler(sunEulerGolden), g);
            transform.rotation = Quaternion.Slerp(dayRot, Quaternion.Euler(sunEulerNight), n);
            sun.shadows = LightShadows.Soft;          // PCF low; toon edge done in shader. UM_QualityTier disables on Base/Standard
            sun.shadowStrength = Mathf.Lerp(0.75f, 0.5f, n);
            sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f; // usati solo se la luce non usa i bias della pipeline (vedi URP asset: depth 0.6 / normal 0.7)

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = Color.Lerp(Color.Lerp(UM_Palette.AmbSky, UM_Palette.TramSky, g), UM_Palette.NottSky, n);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(UM_Palette.AmbEquator, UM_Palette.TramEq, g), UM_Palette.NottEq, n);
            RenderSettings.ambientGroundColor  = Color.Lerp(UM_Palette.AmbGround, UM_Palette.NottGround, n);

            // Linear fog = warm haze on the distant OmniFreight tower / bay (cheap on mobile)
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(UM_Palette.Foschia, UM_Palette.TramCielo, g), UM_Palette.NottEq, n);
            // Guida §9: nebbia calda leggera 30-90 m, misurata dalla scena (camera a 20 m) -> 50..110 dalla camera
            RenderSettings.fogStartDistance = UM_QualityTier.CameraDistance + 30f;
            RenderSettings.fogEndDistance = UM_QualityTier.CameraDistance + 90f;
        }
    }
}
