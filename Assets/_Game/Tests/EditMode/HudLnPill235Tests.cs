using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.UI;

/// <summary>#235: con 4 e 6 cifre la pillola LN non copre G4. Layout in px USS (dp), portrait 360/390/412 con safe area.</summary>
public class HudLnPill235Tests
{
    const float AriaMin = 8f;
    const string FontPath = "Assets/_Game/UI/Fonts/Fredoka-Bold.ttf";

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

    static float Num(string blocco, string prop)
    {
        var m = Regex.Match(blocco, @"(?:^|[^\w-])" + Regex.Escape(prop) + @"\s*:\s*(-?\d+(?:\.\d+)?)");
        Assert.IsTrue(m.Success, "manca " + prop);
        return float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    static bool SiSovrappongono(R a, R b) => a.X < b.Rgt - 0.01f && b.X < a.Rgt - 0.01f && a.Y < b.Btm - 0.01f && b.Y < a.Btm - 0.01f;

    static bool Contiene(R esterno, R interno) =>
        interno.X >= esterno.X - 0.01f && interno.Y >= esterno.Y - 0.01f &&
        interno.Rgt <= esterno.Rgt + 0.01f && interno.Btm <= esterno.Btm + 0.01f;

    // Fredoka-Bold: avanzamenti hmtx. UI Toolkit usa questo file (Tokens !important), non il SemiBold di HudV2.
    static Dictionary<char, int> _adv;
    static int _units;

    static void CaricaFont()
    {
        if (_adv != null) return;
        var data = File.ReadAllBytes(Path.Combine(Root, FontPath));
        int U16(int o) => (data[o] << 8) | data[o + 1];
        uint U32(int o) => ((uint)data[o] << 24) | ((uint)data[o + 1] << 16) | ((uint)data[o + 2] << 8) | data[o + 3];
        var tabelle = new Dictionary<string, int>();
        int n = U16(4), p = 12;
        for (int i = 0; i < n; i++)
        {
            tabelle[System.Text.Encoding.ASCII.GetString(data, p, 4)] = (int)U32(p + 8);
            p += 16;
        }
        _units = U16(tabelle["head"] + 18);
        int cmap = tabelle["cmap"];
        int nsub = U16(cmap + 2), q = cmap + 4, best = -1;
        for (int i = 0; i < nsub; i++)
        {
            int plat = U16(q), enc = U16(q + 2), rel = (int)U32(q + 4);
            if (plat == 3 && enc == 1 && U16(cmap + rel) == 4) best = cmap + rel;
            q += 8;
        }
        Assert.Greater(best, 0, "cmap format 4");
        int segCount = U16(best + 6) / 2;
        int end = best + 14;
        int start = end + segCount * 2 + 2;
        int delta = start + segCount * 2;
        int range = delta + segCount * 2;
        int Gid(int cp)
        {
            for (int i = 0; i < segCount; i++)
            {
                int e = U16(end + i * 2), s = U16(start + i * 2);
                if (cp > e || cp < s) continue;
                int d = (short)U16(delta + i * 2);
                int ro = U16(range + i * 2);
                if (ro == 0) return (d + cp) & 0xFFFF;
                int g = U16(range + i * 2 + ro + (cp - s) * 2);
                return g == 0 ? 0 : (g + d) & 0xFFFF;
            }
            return 0;
        }
        int hmtx = tabelle["hmtx"];
        _adv = new Dictionary<char, int>();
        foreach (var c in "0123456789.,GK")
        {
            int gid = Gid(c);
            Assert.AreNotEqual(0, gid, "glifo " + c);
            _adv[c] = U16(hmtx + gid * 4);
        }
        Assert.AreEqual(1000, _units);
        Assert.AreEqual(521, _adv['9']);
        Assert.AreEqual(220, _adv['.']);
        Assert.AreEqual(719, _adv['G']);
        Assert.AreEqual(551, _adv['4']);
        Assert.AreEqual(597, _adv['K']);
        Assert.AreEqual(223, _adv[',']);
    }

    static float Testo(string s, float px)
    {
        CaricaFont();
        float w = 0;
        foreach (var c in s)
        {
            Assert.IsTrue(_adv.ContainsKey(c), "carattere senza misura: " + c);
            w += _adv[c] * px / _units;
        }
        return w;
    }

    [Test]
    public void QuattroESeiCifre_NonCopronoG4_ConSafeArea()
    {
        var tokens = Leggi("Assets/_Game/UI/Tokens.uss");
        var hud = Leggi("Assets/_Game/UI/USS/HudV2.uss");
        var tut = Leggi("Assets/_Game/UI/USS/TutorialHud.uss");
        var settings = Leggi("Assets/_Game/UI/USS/Settings.uss");
        var uxml = Leggi("Assets/_Game/UI/UXML/Hud.uxml");

        int iV2 = uxml.IndexOf("HudV2.uss"), iTok = uxml.IndexOf("Tokens.uss"), iTut = uxml.IndexOf("TutorialHud.uss");
        Assert.Greater(iV2, iTok, "Tokens prima di HudV2");
        Assert.Greater(iTut, iV2, "TutorialHud dopo HudV2: le regole #235 vincono");
        int pill = uxml.IndexOf("name=\"currency-pill\"");
        int ln = uxml.IndexOf("name=\"ln-value\"");
        int day = uxml.IndexOf("name=\"day-value\"");
        Assert.Greater(ln, pill);
        Assert.Greater(day, ln, "G4 deve stare dopo il saldo");

        var sp2 = Regex.Match(tokens, @"--sp-2:\s*(\d+(?:\.\d+)?)px");
        var bw = Regex.Match(tokens, @"--bw:\s*(\d+(?:\.\d+)?)px");
        Assert.IsTrue(sp2.Success && bw.Success);
        float barPad = float.Parse(sp2.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        float border = float.Parse(bw.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        StringAssert.Contains("padding: var(--sp-2) var(--sp-2) 0 var(--sp-2)", hud);
        StringAssert.Contains("border-width: var(--bw)", tokens);
        StringAssert.Contains("Fredoka-Bold.ttf", tokens);

        var pillV2 = Blocco(hud, "#currency-pill");
        float minPill = Px(pillV2, "min-width");
        float altezza = Px(pillV2, "height");
        var padM = Regex.Match(pillV2, @"padding\s*:\s*0\s+(\d+(?:\.\d+)?)px");
        Assert.IsTrue(padM.Success, "padding orizzontale della pillola");
        float padX = float.Parse(padM.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        float ml = Px(Blocco(hud, ".currency-value"), "margin-left");
        float fsLn = Px(Blocco(hud, ".currency-value"), "font-size");
        float fsG = Px(Blocco(hud, ".day-value"), "font-size");
        Assert.LessOrEqual(minPill, 100f, "con poche cifre la pillola resta quella di oggi");

        var pill235 = Blocco(tut, "#safe-root #currency-pill");
        Assert.AreEqual(0f, Num(pill235, "flex-shrink"), 0.01f, "la pillola non deve restringersi");
        StringAssert.Contains("nowrap", pill235);
        float maxPill = Px(pill235, "max-width");
        Assert.IsFalse(Regex.IsMatch(pill235, @"max-width\s*:\s*\d+%"), "max-width in % blocca la pillola");
        float gap = Px(Blocco(tut, "#safe-root #currency-pill > .currency"), "margin-right");
        Assert.GreaterOrEqual(gap, AriaMin, "distacco fisso fra saldo e giorno");
        Assert.AreEqual(0f, Num(Blocco(tut, "#safe-root #currency-pill > .currency"), "flex-shrink"), 0.01f);
        Assert.AreEqual(0f, Px(Blocco(tut, "#safe-root #currency-pill > .day-group"), "margin-right"), 0.01f);
        Assert.AreEqual(0f, Num(Blocco(tut, "#safe-root #currency-pill > .day-group"), "flex-shrink"), 0.01f);
        Assert.AreEqual(0f, Num(Blocco(tut, "#ln-value"), "flex-shrink"), 0.01f);
        Assert.AreEqual(0f, Num(Blocco(tut, "#day-value"), "flex-shrink"), 0.01f);
        StringAssert.Contains("nowrap", Blocco(tut, "#ln-value"));
        StringAssert.Contains("nowrap", Blocco(tut, "#day-value"));
        float icon = Px(Blocco(tut, "#safe-root #currency-pill .icon"), "width");

        var topLeft = Blocco(tut, "#safe-root .top-left");
        var pct = Regex.Match(topLeft, @"max-width\s*:\s*(\d+(?:\.\d+)?)%");
        Assert.IsTrue(pct.Success);
        float topLeftPct = float.Parse(pct.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) / 100f;
        var obj = Blocco(tut, "#safe-root .tutorial-objective");
        var objPct = Regex.Match(obj, @"left\s*:\s*(\d+(?:\.\d+)?)%");
        Assert.IsTrue(objPct.Success);
        float objLeftPct = float.Parse(objPct.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) / 100f;
        float objTop = Px(obj, "top"), objH = Px(obj, "height");

        var gear = Blocco(settings, "#settings-button");
        float gearW = Px(gear, "width"), gearH = Px(gear, "height"), gearMr = Px(gear, "margin-right");

        // 4 cifre restano leggibili (niente "10K"): è il caso del bug. 5 cifre ancora per esteso. Oltre, il formato compatto.
        Assert.AreEqual("9.999", HudFormat.Amount(9999));
        Assert.AreEqual("99.999", HudFormat.Amount(99999));
        string compatto = HudFormat.Amount(999999);
        Assert.IsNotEmpty(compatto);
        Assert.Less(Testo(compatto, fsLn), Testo("999.999", fsLn), "oltre 5 cifre il testo non deve essere più lungo di 6 cifre per esteso");

        var casi = new[]
        {
            new { Nome = "4 cifre", Testo = "9.999" },
            new { Nome = "5 cifre", Testo = "99.999" },
            new { Nome = "6 cifre", Testo = "999.999" },
            new { Nome = "6 cifre valore", Testo = compatto },
        };

        // Portrait QA, stessi insetti della #224. La safe area è il rettangolo utile: la riga parte dal suo bordo.
        var telefoni = new[]
        {
            new { W = 360f, H = 800f, S = new Insetti { Top = 24, Right = 0, Bottom = 48, Left = 0 }, Nome = "360" },
            new { W = 390f, H = 844f, S = new Insetti { Top = 47, Right = 0, Bottom = 34, Left = 0 }, Nome = "390" },
            new { W = 412f, H = 915f, S = new Insetti { Top = 24, Right = 0, Bottom = 24, Left = 0 }, Nome = "412" },
            new { W = 412f, H = 915f, S = new Insetti { Top = 32, Right = 12, Bottom = 24, Left = 12 }, Nome = "412-cutout" },
        };

        foreach (var t in telefoni)
        {
            var safe = new R { X = t.S.Left, Y = t.S.Top, W = t.W - t.S.Left - t.S.Right, H = t.H - t.S.Top - t.S.Bottom };
            float corsia = safe.W * topLeftPct;
            float objX = safe.X + safe.W * objLeftPct;
            var obiettivo = new R { X = objX, Y = t.S.Top + objTop, W = safe.Rgt - objX, H = objH };
            float gearR = safe.Rgt - barPad - gearMr;
            var ingranaggio = new R { X = gearR - gearW, Y = t.S.Top + barPad, W = gearW, H = gearH };

            foreach (var c in casi)
            {
                float lnW = Testo(c.Testo, fsLn), gW = Testo("G4", fsG);
                float contenuto = icon + ml + lnW + gap + gW;
                float bordo = contenuto + padX * 2f + border * 2f;
                float usata = Mathf.Clamp(bordo, minPill, maxPill);
                Assert.GreaterOrEqual(maxPill, bordo, t.Nome + " " + c.Nome + " la max-width taglia il saldo");
                Assert.LessOrEqual(usata, corsia + 0.01f, t.Nome + " " + c.Nome + " la pillola esce dalla corsia sinistra");

                float x = t.S.Left + barPad + border + padX;
                float y = t.S.Top + barPad;
                var saldo = new R { X = x + icon + ml, Y = y, W = lnW, H = altezza };
                var giorno = new R { X = saldo.Rgt + gap, Y = y, W = gW, H = altezza };
                var pillola = new R { X = t.S.Left + barPad, Y = y, W = usata, H = altezza };

                Assert.IsFalse(SiSovrappongono(saldo, giorno), t.Nome + " " + c.Nome + " il saldo copre G4");
                Assert.GreaterOrEqual(giorno.X - saldo.Rgt, AriaMin - 0.01f, t.Nome + " " + c.Nome + " niente distacco");
                Assert.LessOrEqual(giorno.Rgt, pillola.Rgt - padX - border + 0.01f, t.Nome + " " + c.Nome + " G4 esce dalla pillola");
                Assert.IsTrue(Contiene(safe, pillola), t.Nome + " " + c.Nome + " pillola fuori dalla safe area");
                Assert.IsFalse(SiSovrappongono(pillola, obiettivo), t.Nome + " " + c.Nome + " pillola sopra l'obiettivo");
                Assert.GreaterOrEqual(obiettivo.X - pillola.Rgt, AriaMin - 0.01f, t.Nome + " " + c.Nome + " aria verso l'obiettivo");
                Assert.IsFalse(SiSovrappongono(pillola, ingranaggio), t.Nome + " " + c.Nome + " pillola sopra l'ingranaggio");
                if (c.Nome == "4 cifre")
                    Assert.Greater(usata, minPill + 0.5f, t.Nome + " con 4 cifre la pillola deve allargarsi");
            }
        }
    }
}
