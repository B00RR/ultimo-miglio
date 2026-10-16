using NUnit.Framework;
using UltimoMiglio.Core;

/// <summary>#103: ricompense intermedie di economy.json (rewards) applicate da EconomyCore.</summary>
public class EconomyRewardsTests
{
    static EconomyCore Ch1() => new EconomyCore(EconomyConfig.From(TestData.Ch1()));

    [Test] public void ParcelMilestones_100_250_450_Give60_120_200()
    {
        var e = Ch1(); int hits = 0; double got = 0; e.RewardGranted += (k, i, ln) => { if (k == "parcel") { hits++; got += ln; } };
        double t = 0;
        for (int i = 0; i < 99; i++) e.Serve(t += 10);
        Assert.AreEqual(0, hits); double w = e.S.Wallet;
        e.Serve(t += 10); Assert.AreEqual(1, hits); Assert.AreEqual(w + 60, e.S.Wallet, 1e-9);
        for (int i = 100; i < 450; i++) e.Serve(t += 10);
        Assert.AreEqual(3, hits); Assert.AreEqual(380, got, 1e-9);
        for (int i = 0; i < 100; i++) e.Serve(t += 10);
        Assert.AreEqual(3, hits, "soglie date una volta sola");
    }
    [Test] public void DebtMilestones_25_50_75()
    {
        var e = Ch1(); var seen = new System.Collections.Generic.List<int>(); e.RewardGranted += (k, i, ln) => { if (k == "debt") seen.Add(i); };
        double perShare = e.TrancheDue / (1 - e.Cfg.PlayerSpendShare); // LN lordi per pagare l'intera tranche
        e.CollectActive(perShare * 0.24); Assert.AreEqual(0, seen.Count);
        e.CollectActive(perShare * 0.02); CollectionAssert.AreEqual(new[] { 0 }, seen); Assert.AreEqual(0, e.S.Reputation);
        e.CollectActive(perShare * 0.25); CollectionAssert.AreEqual(new[] { 0, 1 }, seen); Assert.AreEqual(1, e.S.Reputation);
        e.CollectActive(perShare * 0.25); CollectionAssert.AreEqual(new[] { 0, 1, 2 }, seen);
        Assert.AreEqual("decor_foto_nonna_ada", e.Cfg.DebtMilestoneItemIds[2]);
    }
    [Test] public void CassaTip_30_OnlyFirstVisit()
    {
        var e = Ch1(); Assert.AreEqual(30, e.CassaFirstVisitTip(), 1e-9); Assert.AreEqual(30, e.S.Wallet, 1e-9);
        Assert.AreEqual(0, e.CassaFirstVisitTip(), 1e-9); Assert.AreEqual(30, e.S.Wallet, 1e-9);
    }
    [Test] public void NextPadArrow_From480_CheapestAffordable()
    {
        var e = Ch1(); e.S.Scarpe = 1; e.S.NonnoLevel = 1; e.S.Wallet = 1e7;
        e.S.ActiveSec = 479; Assert.IsNull(e.NextPadArrowTarget());
        e.S.ActiveSec = 480;
        var exp = e.Cost(UpgradeKind.Scarpe) <= e.Cost(UpgradeKind.NonnoLevel) ? UpgradeKind.Scarpe : UpgradeKind.NonnoLevel;
        Assert.AreEqual(exp, e.NextPadArrowTarget());
        e.S.Wallet = 0; Assert.IsNull(e.NextPadArrowTarget(), "non affordable → niente freccia");
        e.S.Wallet = 1e7; e.S.Scarpe = 2; e.S.NonnoLevel = 2; Assert.IsNull(e.NextPadArrowTarget(), "già comprati → niente freccia");
    }
}
