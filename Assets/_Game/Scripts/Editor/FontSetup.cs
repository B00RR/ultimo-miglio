using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Baloo 2 (titoli e valori) e Fredoka (testo), licenza OFL, come Font Asset TextCore dinamici
    /// per UI Toolkit. Il fallback di sistema resta Roboto. Menu: Ultimo Miglio/UI/Setup Fonts.</summary>
    public static class FontSetup
    {
        const string Dir = "Assets/_Game/UI/Fonts";

        [MenuItem("Ultimo Miglio/UI/Setup Fonts (Baloo 2 + Fredoka)")]
        public static void Apply()
        {
            var roboto = Make("Roboto-Medium", null);
            var robotoB = Make("Roboto-Bold", null);
            var corpo = new List<FontAsset>();
            if (roboto) corpo.Add(roboto);
            if (robotoB) corpo.Add(robotoB);
            var fredoka = Make("Fredoka-Medium", corpo);
            var titoli = new List<FontAsset>();
            if (fredoka) titoli.Add(fredoka);
            titoli.AddRange(corpo);
            var baloo = Make("Baloo2-Bold", titoli);

            const string tsPath = Dir + "/UM_PanelTextSettings.asset";
            var ts = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(tsPath);
            if (!ts) { ts = ScriptableObject.CreateInstance<PanelTextSettings>(); AssetDatabase.CreateAsset(ts, tsPath); }
            ts.defaultFontAsset = fredoka ? fredoka : baloo;
            var fb = new List<FontAsset>();
            if (baloo) fb.Add(baloo);
            fb.AddRange(corpo);
            ts.fallbackFontAssets = fb;
            EditorUtility.SetDirty(ts);
            foreach (var g in AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets/_Game/UI" }))
            {
                var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(g));
                ps.textSettings = ts; EditorUtility.SetDirty(ps);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Fonts] Baloo 2 (titoli) + Fredoka (testo) sui PanelSettings UI Toolkit");
        }

        static FontAsset Make(string name, List<FontAsset> fallbacks)
        {
            string path = $"{Dir}/{name}_SDF.asset";
            var fa = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (!fa)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{Dir}/{name}.ttf");
                if (!font) { Debug.LogWarning("[Fonts] manca " + name); return null; }
                fa = FontAsset.CreateFontAsset(font); fa.name = name + "_SDF"; AssetDatabase.CreateAsset(fa, path);
                if (fa.material) AssetDatabase.AddObjectToAsset(fa.material, fa);
                if (fa.atlasTextures != null) foreach (var t in fa.atlasTextures) if (t) AssetDatabase.AddObjectToAsset(t, fa);
            }
            if (fa != null && fallbacks != null) { fa.fallbackFontAssetTable = new List<FontAsset>(fallbacks); EditorUtility.SetDirty(fa); }
            return fa;
        }
    }
}
