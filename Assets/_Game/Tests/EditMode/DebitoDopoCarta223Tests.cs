using NUnit.Framework;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#223 (ECO-06, ONB-13, regole GD): nessuna trattenuta prima del primo versamento alla Cassaforte nel passo debtCard;
/// il versamento è il 60% del portafoglio, poi parte lo split 40/60; il passo aspetta un versamento fatto dopo il suo inizio.</summary>
public class DebitoDopoCarta223Tests
{
    static EconomyCore Ch1() => new EconomyCore(EconomyConfig.From(TestData.Ch1()));

    [Test] public void PrimaDelVersamento_NienteAlDebito_TuttoNelPortafoglio()
    {
        var e = Ch1(); e.S.DebtSplitLocked = TutorialManager.DebtSplitLockedAt(TutorialManager.Step.Serve);
        double w = e.S.Wallet;
        e.CollectActive(100);
        Assert.AreEqual(0, e.S.DebtPaid, 1e-9, "prima del versamento alla Cassaforte nessuna trattenuta");
        Assert.AreEqual(100 * (1 - e.TollFraction), e.S.Wallet - w, 1e-9, "100% delle LN nel portafoglio");
    }

    [Test] public void PrimoVersamento_60PercentoDelPortafoglio_PoiSplit40_60()
    {
        var e = Ch1(); e.S.DebtSplitLocked = true; e.S.Wallet = 200;
        Assert.AreEqual(120, e.FirstDebtDeposit(), 1e-9);
        Assert.AreEqual(80, e.S.Wallet, 1e-9); Assert.AreEqual(120, e.S.DebtPaid, 1e-9); Assert.IsFalse(e.S.DebtSplitLocked);
        Assert.AreEqual(0, e.FirstDebtDeposit(), 1e-9, "il primo versamento avviene una volta sola");
        double d = e.S.DebtPaid; e.CollectActive(100);
        Assert.AreEqual(100 * (1 - e.TollFraction) * (1 - e.Cfg.PlayerSpendShare), e.S.DebtPaid - d, 1e-9, "dopo il versamento: 60% al debito");
    }

    [Test] public void BloccoPerPasso()
    {
        Assert.IsTrue(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.ColdOpen), "la lettera del cold open non avvia la trattenuta");
        Assert.IsTrue(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.CassaPass));
        Assert.IsFalse(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.DebtCard)); // al passo debtCard il blocco resta finché non si versa
        Assert.IsFalse(TutorialManager.DebtSplitLockedAt(TutorialManager.Step.Chiuso));
        var e = Ch1(); e.S.DebtSplitLocked = false;
        e.CollectActive(100);
        Assert.Greater(e.S.DebtPaid, 0, "tutorial saltato o finito: split attivo");
    }

    [Test] public void Cassaforte_VersamentoSoloVicino()
    {
        var c = new UnityEngine.Vector3(3, 0, 2);
        Assert.IsTrue(TutorialManager.VicinoAllaCassaforte(c + new UnityEngine.Vector3(1.5f, 0.8f, 0), c), "entro 1,6 m (l'altezza non conta)");
        Assert.IsFalse(TutorialManager.VicinoAllaCassaforte(c + new UnityEngine.Vector3(1.2f, 0, 1.2f), c), "1,7 m: fuori");
        Assert.AreEqual("prop_safe", TutorialManager.SafeObjectName);
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
