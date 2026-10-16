// Ultimo Miglio - applica il tier Web al boot SOLO nella build WebGL (nell'editor e su Android non fa nulla).
// Prima nessuno chiamava UM_QualityTier.Apply: valevano solo i valori dell'URP asset e Current restava Standard
// (lanterne a 0 luci reali, particelle 0.75x). Ora su WebGL: post OFF, render scale 0.9, MSAA 2x, ombre soft 30 m 2048,
// lanterne max 2 luci reali, outline solo sui materiali toon con _OutlineOn.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public static class UM_WebTierBoot
    {
        const string ToonShader = "UltimoMiglio/ToonDiorama";

#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            UM_QualityTier.Apply(UMTier.Web, FindToonMaterials());
            Debug.Log("[UM VFX] tier Web applicato al boot (WebGL)");
        }
#endif

        /// Materiali UM_ToonDiorama caricati in memoria (scena corrente + asset referenziati). Mai null.
        public static Material[] FindToonMaterials()
        {
            var list = new System.Collections.Generic.List<Material>();
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
                if (m && m.shader && m.shader.name == ToonShader) list.Add(m);
            return list.ToArray();
        }
    }
}
