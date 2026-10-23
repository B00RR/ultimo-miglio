using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

/// <summary>#186: Baloo 2 e Fredoka (OFL) come Font Asset, insegna di noce, niente pillola piatta.</summary>
public class Etichette186Tests
{
    static string Data(string rel) => Path.Combine(Application.dataPath, rel);

    [Test]
    public void Font_Ofl_E_File_Veri()
    {
        AssertOfl("_Game/UI/Fonts/OFL/Baloo2/OFL.txt", "Baloo 2 Project Authors");
        AssertOfl("_Game/UI/Fonts/OFL/Fredoka/OFL.txt", "Fredoka Project Authors");
        AssertTtf("_Game/UI/Fonts/Baloo2-Bold.ttf", "Baloo 2", "Bold");
        AssertTtf("_Game/UI/Fonts/Fredoka-Medium.ttf", "Fredoka", "Medium");
    }

    [Test]
    public void FontAsset_TextCore_Collegati()
    {
        string balooGuid = GuidOf("_Game/UI/Fonts/Baloo2-Bold.ttf.meta");
        string fredokaGuid = GuidOf("_Game/UI/Fonts/Fredoka-Medium.ttf.meta");
        string baloo = File.ReadAllText(Data("_Game/UI/Fonts/Baloo2-Bold_SDF.asset"));
        string fredoka = File.ReadAllText(Data("_Game/UI/Fonts/Fredoka-Medium_SDF.asset"));
        StringAssert.Contains("m_FamilyName: Baloo 2", baloo);
        StringAssert.Contains("m_StyleName: Bold", baloo);
        StringAssert.Contains("m_AtlasPopulationMode: 1", baloo);
        StringAssert.Contains(balooGuid, baloo);
        StringAssert.Contains("m_FamilyName: Fredoka\n", fredoka.Replace("\r\n", "\n"));
        StringAssert.Contains("m_StyleName: Medium\n", fredoka.Replace("\r\n", "\n"));
        StringAssert.Contains(fredokaGuid, fredoka);
        string pannello = File.ReadAllText(Data("_Game/UI/Fonts/UM_PanelTextSettings.asset"));
        string fredokaAssetGuid = GuidOf("_Game/UI/Fonts/Fredoka-Medium_SDF.asset.meta");
        StringAssert.Contains(fredokaAssetGuid, pannello);
    }

    [Test]
    public void Foglio_Insegna_Senza_Bianco_Ne_9Slice()
    {
        string uss = File.ReadAllText(Data("_Game/UI/USS/Etichette.uss"));
        StringAssert.Contains("Baloo2-Bold_SDF.asset", uss);
        StringAssert.Contains("Fredoka-Medium_SDF.asset", uss);
        StringAssert.Contains("tex_insegna_noce.png", uss);
        StringAssert.Contains("#7A4A2E", uss);
        StringAssert.Contains("#F3E3C3", uss);
        StringAssert.Contains("#F2B53A", uss);
        StringAssert.Contains("#3B2A20", uss);
        StringAssert.Contains("#9E7873", uss);
        StringAssert.Contains("text-shadow", uss);
        StringAssert.Contains("white-space: normal !important", uss);
        StringAssert.Contains("text-overflow: clip", uss);
        StringAssert.Contains("tex_fumetto_crema.png", uss);
        StringAssert.Contains("tex_capsula_noce_40.png", uss);
        StringAssert.Contains("tex_capsula_noce_32.png", uss);
        StringAssert.Contains("color: #3B2A20 !important", uss);
        StringAssert.Contains("rgba(158, 120, 115, 0.6)", uss);
        StringAssert.Contains("-unity-slice-bottom: 24px !important", uss);
        StringAssert.Contains("-unity-slice-bottom: 20px !important", uss);
        StringAssert.Contains("border-radius: 0 !important", uss);
        StringAssert.Contains("height: auto !important", uss);
        StringAssert.Contains("max-height: none !important", uss);
        StringAssert.Contains("-unity-text-outline-width: 0 !important", uss);
        StringAssert.Contains("padding-bottom: 20px !important", uss);
        StringAssert.Contains("text-overflow: clip !important", uss);
        StringAssert.DoesNotContain("ombra-portata", uss);
        StringAssert.DoesNotContain("tex_capsula_noce_48.png", uss);
        StringAssert.DoesNotContain("ellipsis", uss);
        StringAssert.DoesNotContain("#FFFFFF", uss.ToUpperInvariant());
        StringAssert.DoesNotContain("#FFF ", uss.ToUpperInvariant());
        StringAssert.DoesNotContain("tex_wood_panel", uss);
        StringAssert.DoesNotContain("tex_cardboard_panel", uss);
        string pad = File.ReadAllText(Data("_Game/UI/USS/PadTag.uss"));
        StringAssert.DoesNotContain("#FFFFFF", pad.ToUpperInvariant());
        StringAssert.DoesNotContain("#5E7F86", pad.ToUpperInvariant());
        StringAssert.DoesNotContain("#3E5A61", pad.ToUpperInvariant());
    }

    [Test]
    public void Cartellino_Non_E_Piu_Una_Pillola_Gialla()
    {
        string src = File.ReadAllText(Data("_Game/Scripts/Gameplay/InteractionPoint.cs"));
        StringAssert.DoesNotContain("backgroundColor = Kind == PoiKind.Acquisto ? Giallo", src);
        StringAssert.DoesNotContain("unityFontDefinition = FontDefinition.FromFont", src);
        StringAssert.Contains("insegna-noce", src);
        StringAssert.Contains("poi-chip__valore", src);
    }

    [Test]
    public void Texture_Insegna_Solo_Colori_Caldi()
    {
        var bytes = File.ReadAllBytes(Data("_Game/UI/Textures/tex_insegna_noce.png"));
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(tex.LoadImage(bytes), "PNG insegna illeggibile");
        var px = tex.GetPixels32();
        int bianchi = 0, freddi = 0;
        foreach (var c in px)
        {
            if (c.r >= 250 && c.g >= 250 && c.b >= 250) bianchi++;
            bool blu = c.b > c.r + 18 && c.b > c.g + 8 && c.b > 90;
            bool viola = c.r > 140 && c.b > 140 && c.b > c.g + 30 && c.r > c.g + 25;
            if (blu || viola) freddi++;
        }
        Assert.AreEqual(0, bianchi, "pixel bianchi nell'insegna");
        Assert.AreEqual(0, freddi, "pixel blu o viola nell'insegna");
        Object.DestroyImmediate(tex);
    }

    [Test]
    public void Capsule_RaggioMetaAltezza_E_AngoliVuoti()
    {
        // Il corpo è una capsula (raggio = metà altezza). I 4 px in più in basso sono l'ombra cotta.
        ControllaCapsula("_Game/UI/Textures/tex_capsula_noce_32.png", 32);
        ControllaCapsula("_Game/UI/Textures/tex_capsula_noce_40.png", 40);
        ControllaCapsula("_Game/UI/Textures/tex_capsula_noce_48.png", 48);
        ControllaCapsula("_Game/UI/Textures/tex_capsula_terracotta_32.png", 32);
        string hud = File.ReadAllText(Data("_Game/UI/USS/HudV2.uss"));
        StringAssert.DoesNotContain("white-space: nowrap !important", hud);
        string ctrl = File.ReadAllText(Data("_Game/UI/Runtime/HudController.cs"));
        StringAssert.Contains("padT + 18f", ctrl);
        StringAssert.Contains("78f", ctrl);
        string uxml = File.ReadAllText(Data("_Game/UI/UXML/Hud.uxml"));
        StringAssert.DoesNotContain("ombra-portata", uxml);
        StringAssert.DoesNotContain("ombra-portata", File.ReadAllText(Data("_Game/UI/Runtime/AdaNoteView.cs")));
        StringAssert.DoesNotContain("ombra-portata", File.ReadAllText(Data("_Game/Scripts/Gameplay/TutorialManager.cs")));
    }

    [Test]
    public void Fumetto_CremaOpaco_OmbraCotta()
    {
        const string rel = "_Game/UI/Textures/tex_fumetto_crema.png";
        string meta = File.ReadAllText(Data(rel + ".meta"));
        StringAssert.Contains("spriteBorder: {x: 20, y: 24, z: 20, w: 20}", meta);
        StringAssert.Contains("spriteMeshType: 0", meta);
        StringAssert.Contains("alphaIsTransparency: 1", meta);
        var bytes = File.ReadAllBytes(Data(rel));
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(tex.LoadImage(bytes), rel);
            Assert.AreEqual(52, tex.height, rel);
            Assert.Less(tex.GetPixel(0, tex.height - 1).a, 0.15f, "angolo del fumetto opaco");
            Assert.Less(tex.GetPixel(tex.width - 1, 0).a, 0.15f, "angolo del fumetto opaco");
            var mid = tex.GetPixel(tex.width / 2, tex.height / 2);
            Assert.AreEqual(243f / 255f, mid.r, 0.01f, "crema");
            Assert.AreEqual(227f / 255f, mid.g, 0.01f, "crema");
            Assert.AreEqual(195f / 255f, mid.b, 0.01f, "crema");
            Assert.Greater(mid.a, 0.95f, "il fumetto non è trasparente");
            AssertOmbra(tex, rel);
        }
        finally { Object.DestroyImmediate(tex); }
    }

    static void ControllaCapsula(string rel, int corpo)
    {
        int raggio = corpo / 2;
        string meta = File.ReadAllText(Data(rel + ".meta"));
        StringAssert.Contains($"spriteBorder: {{x: {raggio}, y: {raggio + 4}, z: {raggio}, w: {raggio}}}", meta);
        StringAssert.Contains("spriteMeshType: 0", meta);
        StringAssert.Contains("alphaIsTransparency: 1", meta);
        var bytes = File.ReadAllBytes(Data(rel));
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(tex.LoadImage(bytes), rel);
            Assert.AreEqual(corpo + 4, tex.height, rel);
            Assert.Less(tex.GetPixel(0, tex.height - 1).a, 0.15f, rel + " angolo opaco");
            Assert.Less(tex.GetPixel(tex.width - 1, 0).a, 0.15f, rel + " angolo opaco");
            Assert.Greater(tex.GetPixel(tex.width / 2, tex.height / 2).a, 0.95f, rel + " centro bucato");
            Assert.Greater(tex.GetPixel(tex.width / 2, tex.height - 2).a, 0.95f, rel + " bordo bucato");
            AssertOmbra(tex, rel);
            var px = tex.GetPixels32();
            int bianchi = 0, freddi = 0;
            foreach (var c in px)
            {
                if (c.a < 20) continue;
                if (c.r >= 250 && c.g >= 250 && c.b >= 250) bianchi++;
                bool blu = c.b > c.r + 18 && c.b > c.g + 8 && c.b > 90;
                bool viola = c.r > 140 && c.b > 140 && c.b > c.g + 30 && c.r > c.g + 25;
                if (blu || viola) freddi++;
            }
            Assert.AreEqual(0, bianchi, rel + " pixel bianchi");
            Assert.AreEqual(0, freddi, rel + " pixel freddi");
        }
        finally { Object.DestroyImmediate(tex); }
    }

    static void AssertOmbra(Texture2D tex, string rel)
    {
        // GetPixel ha origine in basso: y = 2 è dentro i 4 px di ombra, non sul filo antialias.
        var ombra = tex.GetPixel(tex.width / 2, 2);
        Assert.AreEqual(158f / 255f, ombra.r, 0.01f, rel + " ombra");
        Assert.AreEqual(120f / 255f, ombra.g, 0.01f, rel + " ombra");
        Assert.AreEqual(115f / 255f, ombra.b, 0.01f, rel + " ombra");
        Assert.AreEqual(153f / 255f, ombra.a, 0.02f, rel + " ombra");
    }

    static void AssertOfl(string rel, string copyright)
    {
        string t = File.ReadAllText(Data(rel));
        StringAssert.Contains("SIL Open Font License", t);
        StringAssert.Contains(copyright, t);
    }

    static void AssertTtf(string rel, string family, string style)
    {
        byte[] b = File.ReadAllBytes(Data(rel));
        Assert.Greater(b.Length, 10000, rel);
        Assert.AreEqual(0, b[0], "il ttf non è un puntatore LFS");
        Assert.AreEqual(1, b[1]);
        Assert.IsTrue(Contiene(b, family), family);
        Assert.IsTrue(Contiene(b, style), style);
    }

    static bool Contiene(byte[] b, string s)
    {
        if (Encoding.ASCII.GetString(b).Contains(s)) return true;
        byte[] be = Encoding.BigEndianUnicode.GetBytes(s);
        for (int i = 0; i + be.Length <= b.Length; i++)
        {
            bool ok = true;
            for (int k = 0; k < be.Length; k++) if (b[i + k] != be[k]) { ok = false; break; }
            if (ok) return true;
        }
        return false;
    }

    static string GuidOf(string rel)
    {
        foreach (var line in File.ReadAllLines(Data(rel)))
            if (line.StartsWith("guid:")) return line.Substring(5).Trim();
        Assert.Fail("guid mancante in " + rel);
        return "";
    }
}
