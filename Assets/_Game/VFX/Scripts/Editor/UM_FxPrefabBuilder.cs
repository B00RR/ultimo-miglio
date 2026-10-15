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
            ConfigureFxSheet(); ConfigureTex(Root + "/Textures/T_FX_ContactBlob.png", 64);
            var fxSrc = new Material(UM_FxFactory.SharedMaterial);
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/T_FX_Sheet.png");
            if (sheet) fxSrc.SetTexture("_BaseMap", sheet); // one texture, one material for every FX = batching
            var fxMat = SaveMat(fxSrc, "M_FX_Particles");
            BuildToonMaterials();
            Save(UM_FxFactory.BuildPackagePop(fxMat));
            Mesh coin = null; foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/FX/fx_coin.fbx")) { if (o is Mesh m && (coin == null || m.name.Contains("LOD1"))) coin = m; } // Solara lira; particles use 34-tri LOD1
            var atlasTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Textures/T_Palette_Atlas.png");
// Coins = toon shader + palette atlas (lit like the set, exact atlas hexes, no outline). URP Particles/Unlit with
            // white vertex colour also works, but the toon version reads as a real object next to Nina.
            var coinMat = new Material(Shader.Find("UltimoMiglio/ToonDiorama"));
            if (atlasTex) coinMat.SetTexture("_BaseMap", atlasTex);
            coinMat.SetFloat("_OutlineOn", 0f); coinMat.SetShaderPassEnabled("SRPDefaultUnlit", false);
            coinMat.SetFloat("_RimStrength", 0.4f);
            coinMat = SaveMat(coinMat, "M_FX_Coin");
            Save(UM_FxFactory.BuildCoins(coin, fxMat, coinMat));
            Save(UM_FxFactory.BuildUpgrade(fxMat));
            Save(UM_FxFactory.BuildDust(fxMat));
            // Contact AO blob (URP Unlit transparent, warm dark, alpha 0.35)
            var blobTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/T_FX_ContactBlob.png");
            var blob = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            blob.SetFloat("_Surface", 1f); blob.SetFloat("_Blend", 0f); blob.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            blob.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); blob.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            blob.SetFloat("_ZWrite", 0f); blob.renderQueue = 2990;
            var bc = UM_Palette.ScuroCaldo; bc.a = 0.45f; blob.SetColor("_BaseColor", bc);
            if (blobTex) blob.SetTexture("_BaseMap", blobTex);
            blob = SaveMat(blob, "M_FX_ContactShadow");
            Save(UM_FxFactory.BuildContactShadow(blob));

            // --- Game feel ---
            Save(UM_FxFactory.BuildPackagePickup(fxMat));
            Save(UM_FxFactory.BuildPackageDeliver(fxMat));
            Save(UM_FxFactory.BuildUpgradeComplete(fxMat));
            Save(UM_FxFactory.BuildPurchase(fxMat));
            var sky = Shader.Find("UltimoMiglio/FX/SkyGradient");
            if (sky) SaveMat(new Material(sky), "M_FX_Sky"); else Debug.LogError("[UM VFX] shader UltimoMiglio/FX/SkyGradient mancante");
            var trail = new Material(Shader.Find(UM_FxFactory.ParticleShader));
            trail.SetFloat("_Surface", 1f); trail.SetFloat("_Blend", 0f); trail.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); trail.renderQueue = 3000;
            trail = SaveMat(trail, "M_FX_Trail");
            Save(UM_FxFactory.BuildCoinFlyer(coin, coinMat, fxMat, trail));
            var ringSh = Shader.Find("UltimoMiglio/FX/RingFill");
            if (ringSh) Save(UM_FxFactory.BuildPadFillRing(SaveMat(new Material(ringSh), "M_FX_RingFill")));
            else Debug.LogError("[UM VFX] shader UltimoMiglio/FX/RingFill mancante");
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
            var mDia = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Diorama_Atlas.mat");
            if (mDia) { mDia.SetFloat("_NoiseStrength", 0.06f); mDia.SetFloat("_NoiseScale", 0.35f); } // subtle floor/wall variation on set only
            var ip = Mk("M_Props_Interactive", true); ip.SetFloat("_OutlineWidth", 0.01f); // packages, van, pads
            SaveMat(ip, "M_Props_Interactive");
            BuildLookAssets();
        }

        /// v2.0 premium: valori di profondita/variazione per materiale (ombre colorate e grading arrivano da shader + UM_LookSettings)
        public static void BuildLookAssets()
        {
            void Set(string n, float tile, float hDark, float wallAO, float top, float rim)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{n}.mat"); if (!m) return;
                m.SetColor("_ShadowTint", new Color(0.55f, 0.50f, 0.72f)); m.SetFloat("_ShadowStrength", 0.6f);
                m.SetFloat("_TileVar", tile); m.SetFloat("_TileSize", 1f); m.SetColor("_FloorTint", new Color(1f, 0.95f, 0.9f));
                m.SetFloat("_HeightDark", hDark); m.SetFloat("_WallAO", wallAO); m.SetFloat("_TopLight", top); m.SetFloat("_RimStrength", rim);
                EditorUtility.SetDirty(m);
            }
            Set("M_Diorama_Atlas", 0.08f, 0.18f, 0.3f, 0.1f, 0.2f);
            Set("M_Diorama_Atlas_Emissive", 0.08f, 0.18f, 0.3f, 0.1f, 0.2f);
            Set("M_Props_Interactive", 0f, 0.1f, 0.15f, 0.12f, 0.3f);
            Set("M_Char_Atlas", 0f, 0.08f, 0f, 0.05f, 0.35f);
        }

        static void ConfigureFxSheet()
        {
            var ti = AssetImporter.GetAtPath(Root + "/Textures/T_FX_Sheet.png") as TextureImporter;
            if (!ti) return;
            ti.textureType = TextureImporterType.Default; ti.alphaIsTransparency = true; ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 256; ti.sRGBTexture = true;
            ti.textureCompression = TextureImporterCompression.Compressed; // ASTC/ETC2 on WebGL2 per platform override
            ti.SaveAndReimport();
        }

        static void ConfigureTex(string path, int max)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (!ti) return;
            ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = max; ti.SaveAndReimport();
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
