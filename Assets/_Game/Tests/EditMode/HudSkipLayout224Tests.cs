using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>#224: "Salta tutorial" non copre l'ingranaggio. Layout in px USS (dp), safe area portrait a 360, 390 e 412.</summary>
public class HudSkipLayout224Tests
{
    const float GapMin = 8f;
    const float ToccoMin = 44f;

    struct Insetti { public float Top, Right, Bottom, Left; }
    struct R { public float X, Y, W, H; public float Rgt => X + W; public float Btm => Y + H; }

    static string Root => Directory.GetParent(Application.dataPath).FullName;
    static string Leggi(string rel) => File.ReadAllText(Path.Combine(Root, rel));

    static string Blocco(string uss, string selettore)
    {
        var m = Regex.Match(uss, Regex.Escape(selettore) + @"\s*\{([^}]*)\}", RegexOptions.Singleline);
        Assert.IsTrue(m.Success, "manca il blocco " + selettore);
        return m.Groups[1].Value;
    }

    static float Px(string blocco, string prop)
    {
        var m = Regex.Match(blocco, @"(?:^|[^\w-])" + Regex.Escape(prop) + @"\s*:\s*(-?\d+(?:\.\d+)?)px");
        Assert.IsTrue(m.Success, "manca " + prop + " in " + blocco);
        return float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    static bool SiSovrappongono(R a, R b) => a.X < b.Rgt - 0.01f && b.X < a.Rgt - 0.01f && a.Y < b.Btm - 0.01f && b.Y < a.Btm - 0.01f;

    static bool Contiene(R esterno, R interno) =>
        interno.X >= esterno.X - 0.01f && interno.Y >= esterno.Y - 0.01f &&
        interno.Rgt <= esterno.Rgt + 0.01f && interno.Btm <= esterno.Btm + 0.01f;

    [Test]
    public void SaltaTutorial_NonCopreIngranaggio_ERispettaSafeArea()
    {
        var tokens = Leggi("Assets/_Game/UI/Tokens.uss");
        var hud = Leggi("Assets/_Game/UI/USS/HudV2.uss");
        var settings = Leggi("Assets/_Game/UI/USS/Settings.uss");
        var onb = Leggi("Assets/_Game/UI/USS/Onboarding.uss");
        var uxml = Leggi("Assets/_Game/UI/UXML/Hud.uxml");
        var codice = Leggi("Assets/_Game/Scripts/Gameplay/TutorialManager.cs");

        var sp2 = Regex.Match(tokens, @"--sp-2:\s*(\d+(?:\.\d+)?)px");
        Assert.IsTrue(sp2.Success, "--sp-2");
        float pad = float.Parse(sp2.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        StringAssert.Contains("padding: var(--sp-2) var(--sp-2) 0 var(--sp-2)", hud);

        var gear = Blocco(settings, "#settings-button");
        float gearW = Px(gear, "width"), gearH = Px(gear, "height"), gearMr = Px(gear, "margin-right");

        var skip = Blocco(onb, "#skip-tutorial.skip-tut");
        float skipRight = Px(skip, "right"), skipTop = Px(skip, "top"), skipW = Px(skip, "width"), skipH = Px(skip, "height");
        Assert.GreaterOrEqual(Px(skip, "min-width"), ToccoMin);
        Assert.GreaterOrEqual(Px(skip, "min-height"), ToccoMin);
        Assert.GreaterOrEqual(skipW, ToccoMin, "larghezza tocco");
        Assert.GreaterOrEqual(skipH, ToccoMin, "altezza tocco");
        Assert.AreEqual("absolute", Regex.Match(skip, @"position\s*:\s*(\w+)").Groups[1].Value);
        Assert.GreaterOrEqual(Px(skip, "border-radius") * 2f, skipH - 0.5f, "resta una pillola");
        StringAssert.Contains("rgba(243, 227, 195, 0.85)", skip);
        StringAssert.Contains("#3B2A20", skip);
        StringAssert.Contains("background-image: none", skip);
        StringAssert.Contains("font-size: 11px", skip);
        StringAssert.Contains("Fredoka-SemiBold.ttf", skip);

        // 8 di padding riga + margine ingranaggio + larghezza + aria: se l'ingranaggio cresce, right va aggiornato.
        Assert.GreaterOrEqual(skipRight, pad + gearMr + gearW + GapMin, "la pillola entra nell'ingranaggio");

        var obj = Blocco(onb, ".hud--skip #safe-root .tutorial-objective");
        float objLeft = Px(obj, "left"), objRight = Px(obj, "right");
        Assert.GreaterOrEqual(objRight, skipRight + skipW + GapMin, "l'obiettivo copre la pillola");

        int barra = uxml.IndexOf("class=\"top-bar\"");
        int fineBarra = uxml.IndexOf("tutorial-objective");
        int iSkip = uxml.IndexOf("name=\"skip-tutorial\"");
        int iGear = uxml.IndexOf("name=\"settings-button\"");
        Assert.Greater(barra, 0);
        Assert.Greater(iSkip, barra, "Salta tutorial fuori dalla riga alta");
        Assert.Less(iSkip, fineBarra);
        Assert.Less(iSkip, iGear, "la pillola deve stare prima dell'ingranaggio");
        StringAssert.Contains("Q<Button>(\"skip-tutorial\")", codice);
        StringAssert.Contains("className: \"top-bar\"", codice);

        // Portrait QA: 360×800, 390×844 (iPhone 14), 412×915 (Pixel). Insetti tipici, lati a 0.
        var telefoni = new[]
        {
            new { W = 360f, H = 800f, S = new Insetti { Top = 24, Right = 0, Bottom = 48, Left = 0 }, Nome = "360" },
            new { W = 390f, H = 844f, S = new Insetti { Top = 47, Right = 0, Bottom = 34, Left = 0 }, Nome = "390" },
            new { W = 412f, H = 915f, S = new Insetti { Top = 24, Right = 0, Bottom = 24, Left = 0 }, Nome = "412" },
            // cutout laterale: la riga è nel padding di safe-root, pillola e ingranaggio si spostano insieme
            new { W = 412f, H = 915f, S = new Insetti { Top = 32, Right = 12, Bottom = 24, Left = 12 }, Nome = "412-cutout" },
        };

        foreach (var t in telefoni)
        {
            var safe = new R { X = t.S.Left, Y = t.S.Top, W = t.W - t.S.Left - t.S.Right, H = t.H - t.S.Top - t.S.Bottom };
            float barRight = t.W - t.S.Right;
            float barTop = t.S.Top;
            var pill = new R { X = barRight - skipRight - skipW, Y = barTop + skipTop, W = skipW, H = skipH };
            var ingranaggio = new R { X = barRight - pad - gearMr - gearW, Y = barTop + pad, W = gearW, H = gearH };
            Assert.IsFalse(SiSovrappongono(pill, ingranaggio), t.Nome + " pillola sopra l'ingranaggio");
            Assert.GreaterOrEqual(ingranaggio.X - pill.Rgt, GapMin - 0.01f, t.Nome + " aria fra pillola e ingranaggio");
            Assert.IsTrue(Contiene(safe, pill), t.Nome + " pillola fuori dalla safe area");

            if (t.S.Right <= 0.01f && t.S.Left <= 0.01f)
            {
                var obiettivo = new R { X = objLeft, Y = 14f, W = t.W - objRight - objLeft, H = 28f };
                Assert.IsFalse(SiSovrappongono(pill, obiettivo), t.Nome + " pillola sopra l'obiettivo");
                Assert.IsFalse(SiSovrappongono(obiettivo, ingranaggio), t.Nome + " obiettivo sopra l'ingranaggio");
            }
        }
    }
}
