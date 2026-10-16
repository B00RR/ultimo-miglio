// Ultimo Miglio - issue #48 «Luce e palette Diorama Solare» (guida visiva cap. 1 §2/§9/§11).
// Un clic per il Tech Lead: applica a materiali, URP asset, luce, cielo, look e camera della scena aperta
// i valori v2.1 (niente post-processing: il look viene solo da luci/materiali/shader, WebGL-safe).
// Menu "Ultimo Miglio/VFX/Applica Look #48 (luce + palette + camera)"
// oppure -executeMethod UltimoMiglio.VFX.EditorTools.UM_Look48Setup.Apply
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_Look48Setup
    {
        const string ToonShader = "UltimoMiglio/ToonDiorama";
        static readonly string[] FloorMats = { "M_Diorama_Atlas", "M_Diorama_Atlas_Emissive" };

        [MenuItem("Ultimo Miglio/VFX/Applica Look #48 (luce + palette + camera)")]
        public static void Apply()
        {
            int mats = ApplyMaterials();
            ApplySky();
            UM_LightingSetup.Setup();                  // sole #FFE6B8 50°/-35°, ambient caldo, nebbia, URP ombre (valori UM_QualityTier)
            ApplyLook();
            int cams = ApplyCamera();
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[UM VFX #48] look applicato: {mats} materiali toon, cielo crema/pesca, luce calda, ombre anti-scalettatura, {cams} camera fit. Salvare la scena (Ctrl+S).");
        }

        static int ApplyMaterials()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Game" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m || !m.shader || m.shader.name != ToonShader) continue;
                bool floor = System.Array.IndexOf(FloorMats, m.name) >= 0;
                m.SetColor("_ShadowTint", UM_Palette.Ombra);       // #9E7873 (guida §9)
                m.SetFloat("_ShadowStrength", 0.72f); // contrasto luce/ombra
                m.SetFloat("_ShadowCool", 0.20f); // Concept: ombre leggermente fresche                    // lieve tinta fresca (decisione aperta DevOn: max 0.3, mai blu/viola)
                m.SetFloat("_ShadowSoft", 0.35f);                    // bordo ombra = PCF filtrato, niente scalini
                m.SetFloat("_Bands", 3f); m.SetFloat("_RampSoftness", 0.06f);
                if (floor)
                {
                    m.SetFloat("_FloorBaseMix", 1f);                 // pavimento = lastre crema/tufo, non arancio saturo
                    m.SetColor("_BaseColor", UM_Palette.Pavimento);  // albedo crema neutro (no wash arancio)
                    if (m.HasProperty("_Color")) m.SetColor("_Color", UM_Palette.Pavimento);
                    m.SetColor("_FloorTint", UM_Palette.Pavimento);  // #F3E3C3
                    m.SetColor("_FloorTintB", UM_Palette.CremaVarA); // #E8D3AC
                    m.SetColor("_FloorTintC", UM_Palette.CremaVarB); // #E9D2A8
                    m.SetFloat("_TileSize", 1.1f);                   // lastre grandi in camera
                    m.SetFloat("_TileVar", 0.025f);
                    m.SetFloat("_GroutDark", 0.035f);
                    m.SetFloat("_NoiseStrength", 0.02f); m.SetFloat("_NoiseScale", 0.22f);
                }
                else m.SetFloat("_FloorBaseMix", 0f);
                EditorUtility.SetDirty(m); n++;
            }
            return n;
        }

        static void ApplySky()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_FX_Sky.mat");
            if (!sky) return;
            sky.SetColor("_Top", UM_Palette.CieloTop); sky.SetColor("_Horizon", UM_Palette.CieloOriz); sky.SetColor("_Bottom", UM_Palette.CieloBasso);
            EditorUtility.SetDirty(sky);
        }

        // I valori serializzati in scena vincono sui default dello script: li riscrivo esplicitamente.
        static void ApplyLook()
        {
            foreach (var look in Object.FindObjectsByType<UM_LookSettings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var d = new GameObject("tmp").AddComponent<UM_LookSettings>(); // default v2.1
                d.gameObject.hideFlags = HideFlags.HideAndDontSave;
                look.exposure = d.exposure; look.contrast = d.contrast; look.saturation = d.saturation; look.glow = d.glow; look.lift = d.lift;
                look.vignetteDark = d.vignetteDark; look.vignetteColor = d.vignetteColor;
                look.skyTopDay = d.skyTopDay; look.skyHorizonDay = d.skyHorizonDay; look.skyBottomDay = d.skyBottomDay;
                look.skyTopSunset = d.skyTopSunset; look.skyHorizonSunset = d.skyHorizonSunset; look.skyBottomSunset = d.skyBottomSunset;
                Object.DestroyImmediate(d.gameObject);
                look.Apply(); EditorUtility.SetDirty(look);
            }
        }

        // DepotCameraFit vive nell'assembly Gameplay (non referenziato qui): campi via SerializedObject.
        static int ApplyCamera()
        {
            int n = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!mb) continue;
                var tn = mb.GetType().Name;
                var so = new SerializedObject(mb);
                if (tn == "DepotCameraFit")
                {
                    Set(so, "DefaultPortraitOrtho", 4.5f); Set(so, "DefaultLandscapeOrtho", 4.8f);
                    Set(so, "MinOrtho", 4.2f); Set(so, "CameraDistance", UM_QualityTier.CameraDistance);
                    so.ApplyModifiedProperties(); n++;
                }
                else if (tn == "CameraRigSetup")
                {
                    Set(so, "OrthoSize", 4.5f); Set(so, "Distance", UM_QualityTier.CameraDistance);
                    so.ApplyModifiedProperties(); n++;
                }
            }
            return n;
        }

        static void Set(SerializedObject so, string prop, float v) { var p = so.FindProperty(prop); if (p != null) p.floatValue = v; }
    }
}
