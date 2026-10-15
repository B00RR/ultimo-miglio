using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Inter (OFL, rsms/inter v4.1) + Roboto fallback (OFL, roboto-3-classic) as dynamic font assets for
    /// UI Toolkit (PanelTextSettings on both HUD PanelSettings) and TextMeshPro. Menu: Ultimo Miglio/UI/Setup Fonts.</summary>
    public static class FontSetup
    {
        const string Dir = "Assets/_Game/UI/Fonts";

        [MenuItem("Ultimo Miglio/UI/Setup Fonts (Inter + Roboto)")]
        public static void Apply()
        {
            var roboto = Make("Roboto-Medium", null);
            var robotoB = Make("Roboto-Bold", null);
            var fb = new List<FontAsset> { roboto, robotoB };
            var semi = Make("Inter-SemiBold", fb); Make("Inter-Regular", fb); Make("Inter-Bold", fb); Make("Inter-ExtraBold", fb);

            const string tsPath = Dir + "/UM_PanelTextSettings.asset";
            var ts = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(tsPath);
            if (!ts) { ts = ScriptableObject.CreateInstance<PanelTextSettings>(); AssetDatabase.CreateAsset(ts, tsPath); }
            ts.defaultFontAsset = semi; ts.fallbackFontAssets = fb; EditorUtility.SetDirty(ts);
            foreach (var g in AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets/_Game/UI" }))
            {
                var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(g));
                ps.textSettings = ts; EditorUtility.SetDirty(ps);
            }
            // TextMeshPro (uGUI/world-space text, if used)
            var interFont = AssetDatabase.LoadAssetAtPath<Font>(Dir + "/Inter-SemiBold.ttf");
            const string tmpPath = Dir + "/Inter-SemiBold_TMP.asset";
            if (interFont && !AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(tmpPath))
            {
                var tmp = TMPro.TMP_FontAsset.CreateFontAsset(interFont); AssetDatabase.CreateAsset(tmp, tmpPath);
                if (tmp.material) AssetDatabase.AddObjectToAsset(tmp.material, tmp);
                if (tmp.atlasTextures != null) foreach (var t in tmp.atlasTextures) if (t) AssetDatabase.AddObjectToAsset(t, tmp);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Fonts] Inter + Roboto fallback applied to UI Toolkit PanelSettings and TMP");
        }

        static FontAsset Make(string name, List<FontAsset> fallbacks)
        {
            string path = $"{Dir}/{name}_SDF.asset";
            var fa = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (!fa)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{Dir}/{name}.ttf");
                if (!font) { Debug.LogWarning("[Fonts] missing " + name); return null; }
                fa = FontAsset.CreateFontAsset(font); fa.name = name + "_SDF"; AssetDatabase.CreateAsset(fa, path);
                if (fa.material) AssetDatabase.AddObjectToAsset(fa.material, fa);
                if (fa.atlasTextures != null) foreach (var t in fa.atlasTextures) if (t) AssetDatabase.AddObjectToAsset(t, fa);
            }
            if (fallbacks != null) { fa.fallbackFontAssetTable = new List<FontAsset>(fallbacks); EditorUtility.SetDirty(fa); }
            return fa;
        }
    }
}
