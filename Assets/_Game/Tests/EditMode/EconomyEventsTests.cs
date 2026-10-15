using System.Collections.Generic;
using NUnit.Framework;
using UltimoMiglio.Core;

public class EconomyEventsTests
{
    [Test] public void LnCreditedCarriesGrossNetAndPosition()
    {
        var e = TestData.NewCore(); var got = new List<(double g, double n, WorldPos p)>();
        e.OnLnCredited += (g, n, p) => got.Add((g, n, p));
        e.CollectActive(10, new WorldPos(1, 2, 3));
        Assert.AreEqual(1, got.Count); Assert.AreEqual(10, got[0].g, 1e-9); Assert.AreEqual(10, got[0].n, 1e-9); Assert.AreEqual(3f, got[0].p.z);
    }

    [Test] public void DayChangedFiresOncePerCalendarDay()
    {
        var e = TestData.NewCore(); var days = new List<int>(); e.DayChanged += days.Add;
        double secPerDay = e.Cfg.CycleSec / e.Cfg.CalendarDaysPerCycle;
        for (int i = 0; i < (int)(secPerDay * 3.5 * 10); i++) e.Tick(0.1);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, days);
    }

    [Test] public void DeadlineUsesProrogaThenMoraWithPedaggio()
    {
        var e = TestData.NewCore(); var mora = new List<bool>(); e.OnMoraChanged += mora.Add;
        Assert.AreEqual(1, e.S.ProrogaTokens);
        e.Tick(e.Cfg.WindowActiveMin * 60 + 1);
        Assert.AreEqual(0, e.S.ProrogaTokens); Assert.IsFalse(e.S.Mora); CollectionAssert.IsEmpty(mora);
        e.Tick(e.Cfg.ProrogaExtensionActiveMin * 60);
        Assert.IsTrue(e.S.Mora); CollectionAssert.AreEqual(new[] { true }, mora);
        Assert.AreEqual(e.Cfg.TrancheLn * (1 + e.Cfg.MoraTrancheSurcharge), e.TrancheDue, 1e-6);
        double net = -1; e.OnLnCredited += (g, n, p) => net = n;
        e.CollectActive(100); Assert.AreEqual(100 * (1 - e.Cfg.PedaggioCoinPenalty), net, 1e-9);
        e.CollectActive(e.TrancheDue / 0.6 / (1 - e.Cfg.PedaggioCoinPenalty) + 1);
        Assert.IsTrue(e.S.ChapterComplete); Assert.IsFalse(e.S.Mora); CollectionAssert.AreEqual(new[] { true, false }, mora);
    }

    [Test] public void EarlyPaymentGivesBulloni()
    {
        var e = TestData.NewCore(); var b = new List<double>(); e.BulloniChanged += b.Add;
        e.CollectActive(e.Cfg.TrancheLn / 0.6 + 1);
        Assert.AreEqual(e.Cfg.EarlyPaymentBulloni, e.S.Bulloni, 1e-9); CollectionAssert.AreEqual(new[] { (double)e.Cfg.EarlyPaymentBulloni }, b);
    }

    [Test] public void CassaCollectRaisesLnCredited()
    {
        var e = TestData.NewCore(); e.S.CassaLn = 30; double gross = 0; e.OnLnCredited += (g, n, p) => gross = g;
        e.CollectCassa(false, 1); Assert.AreEqual(30, gross, 1e-9);
    }
}
