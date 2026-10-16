using System.Collections.Generic;
using NUnit.Framework;
using UltimoMiglio.Core;

/// <summary>#136/#203: tappe del debito 25/50/75% (spec Game Designer sulla #136).</summary>
public class DebtMilestone136Tests
{
    static EconomyCore Ch1() => new EconomyCore(EconomyConfig.From(TestData.Ch1()));

    [Test] public void OgniSogliaScattaUnaVoltaSola_InOrdine_ConRicompensa()
    {
        var e = Ch1(); var q = new DebtMilestoneQueue(); var shown = new List<int>();
        e.RewardGranted += (k, i, ln) => { if (k == "debt") q.Enqueue(i); };
        double full = e.TrancheDue / (1 - e.Cfg.PlayerSpendShare); double t = 0;
        void Drain() { while (q.HasPending) { if (q.TryNext(t, out int i)) shown.Add(i); t += 0.5; } }
        e.CollectActive(full * 0.26); Drain();
        CollectionAssert.AreEqual(new[] { 0 }, shown); Assert.AreEqual(0, e.S.Reputation);
        e.CollectActive(full * 0.25); Drain();
        CollectionAssert.AreEqual(new[] { 0, 1 }, shown); Assert.AreEqual(1, e.S.Reputation, "+1 Reputazione al 50%");
        e.CollectActive(full * 0.25); Drain();
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, shown);
        e.CollectActive(full * 0.2); Drain();
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, shown, "nessuna tappa ripetuta");
        Assert.AreEqual(7, q.Mask); Assert.AreEqual(1, e.S.Reputation);
    }

    [Test] public void UnVersamentoOltrePiuSoglie_InOrdineAUnSecondo()
    {
        var q = new DebtMilestoneQueue();
        Assert.IsTrue(q.Enqueue(0)); Assert.IsTrue(q.Enqueue(1)); Assert.IsTrue(q.Enqueue(2));
        Assert.IsTrue(q.TryNext(10.0, out int a)); Assert.AreEqual(0, a);
        Assert.IsFalse(q.TryNext(10.9, out _), "1,0 s di distanza");
        Assert.IsTrue(q.TryNext(11.0, out int b)); Assert.AreEqual(1, b);
        Assert.IsFalse(q.TryNext(11.5, out _));
        Assert.IsTrue(q.TryNext(12.0, out int c)); Assert.AreEqual(2, c);
        Assert.IsFalse(q.HasPending);
    }

    [Test] public void BitmaskSalvata_NonRiscattaDopoRiavvio()
    {
        var q = new DebtMilestoneQueue(0b011);
        Assert.IsFalse(q.Enqueue(0)); Assert.IsFalse(q.Enqueue(1)); Assert.IsTrue(q.Enqueue(2));
        Assert.AreEqual(0b111, q.Mask);
        CollectionAssert.AreEqual(new[] { "onb.debt.milestone1", "onb.debt.milestone2", "onb.debt.milestone3" }, DebtMilestoneQueue.LineKeys);
    }

    [Test] public void BitmaskNelSalvataggio_RipristinataDalloStato()
    {
        var s = new EconomyState { debt_ms = 0b001 };
        var j = UnityEngine.JsonUtility.ToJson(s);
        StringAssert.Contains("\"debt_ms\":1", j);
        var q = DebtMilestoneQueue.FromState(UnityEngine.JsonUtility.FromJson<EconomyState>(j));
        Assert.IsFalse(q.Enqueue(0), "dopo il riavvio la tappa 1 non ricompare");
    }

    [Test] public void DopoResetDelSalvataggio_LaTappa1ScattaDiNuovo()
    {
        var vecchio = new EconomyState { debt_ms = 0b111, DebtMilestonesHit = 3 };
        Assert.IsFalse(DebtMilestoneQueue.FromState(vecchio).Enqueue(0));
        var e = Ch1(); var q = DebtMilestoneQueue.FromState(e.S); // partita nuova: stato azzerato
        var seen = new List<int>(); e.RewardGranted += (k, i, ln) => { if (k == "debt" && q.Enqueue(i)) seen.Add(i); };
        e.CollectActive(e.TrancheDue / (1 - e.Cfg.PlayerSpendShare) * 0.26);
        CollectionAssert.AreEqual(new[] { 0 }, seen);
    }
}
