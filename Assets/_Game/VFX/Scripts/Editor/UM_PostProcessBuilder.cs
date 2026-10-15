// Ultimo Miglio - builds mobile-safe URP Volume profiles (Day + Night) and a global Volume in the open scene.
// Menu: Ultimo Miglio/VFX/Build Post-Process Profiles. Also callable via script-execute: UM_PostProcessBuilder.BuildAll()
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_PostProcessBuilder
    {
        public const string Dir = "Assets/_Game/VFX/Profiles";

        [MenuItem("Ultimo Miglio/VFX/Build Post-Process Profiles")]
        public static void BuildAll()
        {
            var day = Build("PP_Diorama_Day", false);
            Build("PP_Diorama_Night", true);
            var go = GameObject.Find("PP_GlobalVolume") ?? new GameObject("PP_GlobalVolume");
            var vol = go.GetComponent<Volume>();
            if (!vol) vol = go.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 0; vol.sharedProfile = day;
            AssetDatabase.SaveAssets();
            Debug.Log("[UM VFX] Post-process profiles built in " + Dir);
        }

        static VolumeProfile Build(string name, bool night)
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/_Game/VFX", "Profiles");
            string path = $"{Dir}/{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            foreach (var c in p.components.ToArray()) { p.Remove(c.GetType()); Object.DestroyImmediate(c, true); }

            // Neutral tonemapping keeps palette hexes close to authored values (ACES shifts terracotta/menta)
            var tm = Add<Tonemapping>(p); tm.mode.Override(TonemappingMode.Neutral);

            var bl = Add<Bloom>(p);
            bl.threshold.Override(night ? 0.8f : 0.92f);   // LDR pipeline: only near-white (crema, coin glints, lanterns) blooms
            bl.intensity.Override(night ? 0.6f : 0.25f);
            bl.scatter.Override(0.55f);
            bl.tint.Override(UM_Palette.Crema);
            bl.highQualityFiltering.Override(false);
            bl.downscale.Override(BloomDownscaleMode.Quarter);
            bl.maxIterations.Override(3);                  // mobile: few mips

            var ca = Add<ColorAdjustments>(p);
            ca.postExposure.Override(night ? 0.0f : -0.1f); // darkness comes from lighting, not post: never horror-dark
            ca.contrast.Override(night ? 8f : 6f);
            ca.saturation.Override(night ? -6f : 4f);
            ca.colorFilter.Override(night ? new Color(1f, 0.92f, 0.85f) : new Color(1f, 0.98f, 0.95f)); // warm, subtle

            var wb = Add<WhiteBalance>(p);
            wb.temperature.Override(night ? 10f : 4f);

            var vg = Add<Vignette>(p);
            vg.color.Override(UM_Palette.ScuroCaldo);
            vg.intensity.Override(night ? 0.26f : 0.2f);
            vg.smoothness.Override(0.45f);
            vg.rounded.Override(false);

            EditorUtility.SetDirty(p);
            return p;
        }

        static T Add<T>(VolumeProfile p) where T : VolumeComponent
        {
            var c = p.Add<T>(true); c.name = typeof(T).Name; c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, p);
            return c;
        }
    }
}
