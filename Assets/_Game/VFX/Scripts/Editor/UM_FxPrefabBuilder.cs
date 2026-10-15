// Ultimo Miglio - bakes FX prefabs + shared materials as assets (headless-safe; no scene needed).
// Menu: Ultimo Miglio/VFX/Build FX Prefabs + Materials. Batchmode: -executeMethod UltimoMiglio.VFX.EditorTools.UM_FxPrefabBuilder.BuildAll
using UnityEditor;
using UnityEngine;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_FxPrefabBuilder
    {
        const string Root = "Assets/_Game/VFX";
        const string PrefabDir = Root + "/Prefabs";
        const string MatDir = Root + "/Materials";

        [MenuItem("Ultimo Miglio/VFX/Build FX Prefabs + Materials")]
        public static void BuildAll()
        {
            Ensure(Root, "Prefabs"); Ensure(Root, "Materials");
            var fxMat = SaveMat(new Material(UM_FxFactory.SharedMaterial), "M_FX_Particles");
            BuildToonMaterials();
            Save(UM_FxFactory.BuildPackagePop(fxMat));
            Mesh coin = null; foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/FX/fx_coin.fbx")) { if (o is Mesh m && (coin == null || m.name.Contains("LOD1"))) coin = m; } // Solara lira; particles use 34-tri LOD1
            Save(UM_FxFactory.BuildCoins(coin, fxMat));
            Save(UM_FxFactory.BuildUpgrade(fxMat));
            Save(UM_FxFactory.BuildDust(fxMat));
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[UM VFX] FX prefabs built in " + PrefabDir);
        }

        /// M_Diorama_Atlas (+ _Emissive), M_Char_Atlas, M_Props_Interactive (05_3D_PIPELINE + Concept feedback), on UltimoMiglio/ToonDiorama.
        public static void BuildToonMaterials()
        {
            var sh = Shader.Find("UltimoMiglio/ToonDiorama");
            if (!sh) { Debug.LogError("[UM VFX] ToonDiorama shader missing"); return; }
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Textures/T_Palette_Atlas.png");
            Material Mk(string n, bool outline) { var m = new Material(sh); m.SetFloat("_OutlineOn", outline ? 1f : 0f); m.SetShaderPassEnabled("SRPDefaultUnlit", outline); if (atlas) m.SetTexture("_BaseMap", atlas); m.enableInstancing = false; return m; }
            SaveMat(Mk("M_Diorama_Atlas", false), "M_Diorama_Atlas"); // set: no outline
            var em = Mk("M_Diorama_Atlas_Emissive", false); em.SetColor("_EmissionColor", UM_Palette.Lanterna * 0.8f);
            SaveMat(em, "M_Diorama_Atlas_Emissive");
            var ch = Mk("M_Char_Atlas", true); // characters + robots ch.SetFloat("_RimStrength", 0.35f); ch.SetFloat("_OutlineWidth", 0.008f);
            SaveMat(ch, "M_Char_Atlas");
            var ip = Mk("M_Props_Interactive", true); ip.SetFloat("_OutlineWidth", 0.01f); // packages, van, pads
            SaveMat(ip, "M_Props_Interactive");
        }

        static Material SaveMat(Material m, string name)
        {
            string p = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (existing) { existing.CopyPropertiesFromMaterial(m); existing.shader = m.shader; EditorUtility.SetDirty(existing); return existing; }
            m.name = name; AssetDatabase.CreateAsset(m, p); return m;
        }

        static void Save(GameObject go)
        {
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{go.name}.prefab");
            Object.DestroyImmediate(go);
        }

        static void Ensure(string parent, string child)
        { if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child); }
    }
}
