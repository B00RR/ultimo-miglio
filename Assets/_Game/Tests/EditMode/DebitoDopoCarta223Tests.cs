using NUnit.Framework;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#223 (ECO-06, ONB-13): la quota al debito parte solo dopo il passo debtCard, e il passo aspetta un versamento vero.</summary>
public class DebitoDopoCarta223Tests
{
    static EconomyCore Ch1() => new EconomyCore(EconomyConfig.From(TestData.Ch1()));

    [Test] public void PrimaDellaCarta_NienteAlDebito_TuttoNelPortafoglio()
    {
        var e = Ch1(); e.S.DebtSplitLocked = TutorialManager.DebtSplitLockedAt(TutorialManager.Step.Serve);
        double w = e.S.Wallet;
        e.CollectActive(100);
        Assert.AreEqual(0, e.S.DebtPaid, 1e-9, "prima di debtCard nessuna trattenuta sul debito");
        Assert.Greater(e.S.Wallet - w, 0);
    }

    [Test] public void DallaCartaInPoi_SplitAttivo()
    {
        Assert.IsTrue(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.CassaPass));
        Assert.IsFalse(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.DebtCard));
        Assert.IsFalse(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.Chiuso));
        var e = Ch1(); e.S.DebtSplitLocked = false;
        e.CollectActive(100);
        Assert.Greater(e.S.DebtPaid, 0);
    }

    [Test] public void GateDebtCard_NonPassaConDebitoGiaVersatoPrimaDelPasso()
    {
        Assert.IsFalse(TutorialManager.DebtCardGate(537.6, 537.6), "bug QA: DebtPaid 537,6 già all'ingresso del passo");
        Assert.IsTrue(TutorialManager.DebtCardGate(540, 537.6));
    }

    [Test] public void SalvataggioVecchio_SenzaCampo_SplitAttivo()
    {
        var s = UnityEngine.JsonUtility.FromJson<EconomyState>("{\"Wallet\":10}");
        Assert.IsFalse(s.DebtSplitLocked);
    }
}
