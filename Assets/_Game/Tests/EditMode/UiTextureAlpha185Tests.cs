using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>#185: i riquadri arrotondati non devono più avere angoli pieni.
/// Le texture UI hanno il canale alpha e l'import lo conserva (anche su WebGL).</summary>
public class UiTextureAlpha185Tests
{
    const int RaggioSlice = 16;

    static readonly string[] Pannelli9Slice =
    {
        "Assets/_Game/UI/Textures/tex_cardboard_panel.png",
        "Assets/_Game/UI/Textures/tex_wood_panel.png",
    };

    static string Assoluto(string assetPath) =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);

    [Test]
    public void TextureUi_HannoAlphaEImportTrasparente()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Game/UI/Textures", "Assets/_Game/UI/Icons" });
        Assert.Greater(guids.Length, 0, "nessuna texture UI");
        int n = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            n++;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, path);
            Assert.IsTrue(imp.DoesSourceTextureHaveAlpha(), path + ": il PNG non ha il canale alpha");
            Assert.IsTrue(imp.alphaIsTransparency, path + ": alphaIsTransparency spento");
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, imp.alphaSource, path);
        }
        Assert.Greater(n, 2, "attese le texture dei pannelli e le icone");
    }

    [Test]
    public void Pannelli9Slice_AngoliTrasparentiEWebGlRgba()
    {
        foreach (var path in Pannelli9Slice)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, path);
            var bordo = imp.spriteBorder;
            Assert.GreaterOrEqual(bordo.x, RaggioSlice, path + " bordo sinistro");
            Assert.GreaterOrEqual(bordo.y, RaggioSlice, path + " bordo inferiore");
            Assert.GreaterOrEqual(bordo.z, RaggioSlice, path + " bordo destro");
            Assert.GreaterOrEqual(bordo.w, RaggioSlice, path + " bordo superiore");

            var set = new TextureImporterSettings();
            imp.ReadTextureSettings(set);
            Assert.AreEqual(SpriteMeshType.FullRect, set.spriteMeshType, path + ": la mesh Tight taglia gli angoli trasparenti");

            var web = imp.GetPlatformTextureSettings("WebGL");
            Assert.IsTrue(web.overridden, path + ": override WebGL assente");
            Assert.AreEqual(TextureImporterFormat.RGBA32, web.format, path + ": WebGL senza alpha");

            var bytes = File.ReadAllBytes(Assoluto(path));
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(tex.LoadImage(bytes), path + ": PNG illeggibile");
                // GetPixel ha origine in basso a sinistra: i quattro angoli del file vanno tutti vuoti.
                Assert.Less(tex.GetPixel(0, tex.height - 1).a, 0.15f, path + " angolo alto-sinistra opaco");
                Assert.Less(tex.GetPixel(tex.width - 1, tex.height - 1).a, 0.15f, path + " angolo alto-destra opaco");
                Assert.Less(tex.GetPixel(0, 0).a, 0.15f, path + " angolo basso-sinistra opaco");
                Assert.Less(tex.GetPixel(tex.width - 1, 0).a, 0.15f, path + " angolo basso-destra opaco");
                Assert.Greater(tex.GetPixel(tex.width / 2, tex.height / 2).a, 0.95f, path + " centro bucato");
                // il lato, fuori dall'angolo, resta opaco: lo slice non deve aprire un buco sul bordo
                Assert.Greater(tex.GetPixel(tex.width / 2, tex.height - 2).a, 0.95f, path + " bordo superiore bucato");
            }
            finally { Object.DestroyImmediate(tex); }
        }
    }
}
