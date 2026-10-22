using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UltimoMiglio.Gameplay;

/// <summary>#275 (ONB-13): al passo 13 debtCard la barra mostra anche la lettera GrayBox sulla Cassaforte (onb.viktor.call1 → onb.viktor.call2),
/// non solo onb.debt.adaExplain. Prima CurrentHint dava un solo testo per passo e la lettera non compariva mai.</summary>
public class LetteraGrayBox275Tests
{
    static Dictionary<string, string> Hints() => TutorialManager.HintsFrom(File.ReadAllText(Path.Combine(TestData.Dir, "onboarding_testi.json")));

    [Test] public void DebtCard_Passo13()
    {
        Assert.AreEqual(13, (int)TutorialManager.Step.DebtCard);
        Assert.AreEqual("debtCard", TutorialManager.StepIds[13]);
    }

    [Test] public void DebtCard_BarraInOrdine_SpiegazionePoiLettera()
    {
        float r = TutorialManager.DebtCardRigaSec;
        Assert.AreEqual("onb.debt.adaExplain", TutorialManager.DebtCardHintKey(0f), "all'inizio del passo la spiegazione di Oma Tilde");
        Assert.AreEqual("onb.viktor.call1", TutorialManager.DebtCardHintKey(r + 0.1f), "poi la busta GrayBox");
        Assert.AreEqual("onb.viktor.call2", TutorialManager.DebtCardHintKey(2 * r + 0.1f), "poi la firma di M. Vane");
        Assert.AreEqual("onb.debt.adaExplain", TutorialManager.DebtCardHintKey(3 * r + 0.1f), "e si ricomincia");
        Assert.AreEqual("onb.debt.adaExplain", TutorialManager.DebtCardHintKey(-1f));
    }

    [Test] public void DebtCard_LetteraGrayBox_RestituitaConITestiDelJson()
    {
        var h = Hints(); var visti = new HashSet<string>();
        for (float t = 0f; t < 70f; t += 0.5f) // durata osservata dal QA (dur=69.5 s); golden path ~40 s
        {
            var k = TutorialManager.DebtCardHintKey(t);
            Assert.IsTrue(h.TryGetValue(k, out var txt) && !string.IsNullOrEmpty(txt), $"chiave {k} senza testo in onboarding_testi.json");
            visti.Add(txt);
        }
        Assert.Contains(h["onb.viktor.call1"], new List<string>(visti), "la busta GrayBox (onb.viktor.call1) non compare mai al passo debtCard");
        Assert.Contains(h["onb.viktor.call2"], new List<string>(visti), "la firma GrayBox (onb.viktor.call2) non compare mai al passo debtCard");
        StringAssert.Contains("GrayBox", h["onb.viktor.call1"]);
        Assert.Less(TutorialManager.DebtCardRigaSec * 3, 40f, "la lettera completa deve passare entro il golden path del passo");
    }

    [Test] public void LetteraGrayBox_ChiaviNeiTextKeysDiDebtCard_E_Max70Caratteri()
    {
        var step = TestData.Ch1().onboarding.steps[(int)TutorialManager.Step.DebtCard];
        Assert.AreEqual("debtCard", step.id);
        var h = Hints();
        foreach (var k in TutorialManager.LetteraGrayBox)
        {
            CollectionAssert.Contains(step.textKeys, k, $"{k} non è nei textKeys di debtCard (onboarding.json)");
            Assert.LessOrEqual(h[k].Length, 70, $"{k} oltre 70 caratteri");
        }
    }
}
