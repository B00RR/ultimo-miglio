using NUnit.Framework;
using UltimoMiglio.Core;

public class EconomyCoreTests
{
    EconomyCore New() => TestData.NewCore();

    [Test] public void StartStackIs5_SpeedDropsAtMax()
    {
        var e = New(); Assert.AreEqual(5, e.StackCapacity);
        Assert.AreEqual(3.0, e.NinaSpeed(0), 1e-9); Assert.AreEqual(2.7, e.NinaSpeed(5), 1e-9);
    }
    [Test] public void ComboGrowsCapsAndResets()
    {
        var e = New(); double t = 0;
        Assert.AreEqual(10, e.Serve(t), 1e-9);
        for (int i = 0; i < 20; i++) e.Serve(t += 1);
        Assert.AreEqual(2.0, e.S.Combo, 1e-9);
        Assert.AreEqual(10, e.Serve(t + 6), 1e-9); // gap > 5 s resets
    }
    [Test] public void CoinsSplit40_60()
    {
        var e = New(); e.CollectActive(100);
        Assert.AreEqual(40, e.S.Wallet, 1e-9); Assert.AreEqual(60, e.S.DebtPaid, 1e-9);
    }
    [Test] public void PadPaysInInstallmentsAndUpgrades()
    {
        var e = New(); e.S.Wallet = 100; bool done = false; int ticks = 0; double dwell = 0;
        while (!done && ticks < 100) { done = e.PadTick(UpgradeKind.Zaino, dwell += 0.1); ticks++; }
        Assert.IsTrue(done); Assert.AreEqual(6, e.StackCapacity); Assert.AreEqual(60, e.S.Wallet, 1e-9);
        Assert.LessOrEqual(ticks, 31, "pad must fill in ≤3 s");
    }
    [Test] public void PadInstallmentPersistsWhenWalletEmpty()
    {
        var e = New(); e.S.Wallet = 20;
        for (int i = 0; i < 30; i++) e.PadTick(UpgradeKind.Zaino, i * 0.1);
        Assert.AreEqual(20, e.S.PadPaid[(int)UpgradeKind.Zaino], 1e-9); Assert.AreEqual(0, e.S.Zaino);
    }
    [Test] public void NonnoHireCosts50_WebBalance()
    {
        var e = New(); Assert.AreEqual(50, e.Cost(UpgradeKind.NonnoHire)); // #75
        e.S.Wallet = 50; for (int i = 0; i < 40; i++) e.PadTick(UpgradeKind.NonnoHire, i * .1);
        Assert.AreEqual(0, e.S.NonnoLevel); Assert.AreEqual(6, e.NonnoRestockPpm, 1e-9);
        Assert.AreEqual(100, e.Cost(UpgradeKind.NonnoLevel), 1e-9);
    }
    [Test] public void OfflineZeroWithoutRobots_AndClockBackwardsNoGain()
    {
        var e = New(); e.S.LastSeenUnix = 1000; e.S.ChapterEarnedActive = 5000;
        Assert.AreEqual(0, e.ApplyOffline(100000));
        e.S.NonnoLevel = 0; e.S.LastSeenUnix = 100000;
        Assert.AreEqual(0, e.ApplyOffline(50000));
    }
    [Test] public void OfflineIsHalfRobotThroughputCappedByCassa()
    {
        var e = New(); e.S.NonnoLevel = 0; e.S.ChapterEarnedActive = 100000; e.S.LastSeenUnix = 1;
        double got = e.ApplyOffline(1 + 600); // 10 min: 0.5 ppm*10 LN*0.5 = 2.5 LN/min
        Assert.AreEqual(25, got, 1e-6);
        e.ApplyOffline(1 + 600 + 7 * 24 * 3600);
        Assert.AreEqual(336, e.S.CassaLn, 1e-6); Assert.IsTrue(e.CassaFull);
    }
    [Test] public void OfflineCappedByShareOfChapter()
    {
        var e = New(); e.S.NonnoLevel = 10; e.S.ChapterEarnedActive = 400; e.S.LastSeenUnix = 1;
        e.ApplyOffline(1 + 36000);
        Assert.AreEqual(100, e.S.CassaLn, 1e-6); // 20% of total => 400*0.2/0.8
    }
    [Test] public void AdDoubleMaxTwicePerDay()
    {
        var e = New(); e.S.ChapterEarnedActive = 1e6;
        for (int i = 0; i < 3; i++) { e.S.CassaLn = 10; double got = e.CollectCassa(true, 7); Assert.AreEqual(i < 2 ? 20 : 10, got, 1e-9); }
        e.S.CassaLn = 10; Assert.AreEqual(20, e.CollectCassa(true, 8), 1e-9);
    }
    [Test] public void ChapterCompletesAtTranche()
    {
        var e = New(); string last = null; e.OnEvent += (t, f) => last = t;
        e.CollectActive(14001); Assert.IsTrue(e.S.ChapterComplete); Assert.AreEqual("chapter_complete", last);
    }
    [Test] public void QaLogLineIsJson()
    {
        var e = New(); var q = new QaEventLog(e, "s1"); string line = null; q.Sink += l => line = l;
        e.Serve(0); StringAssert.StartsWith("{\"t\":", line); StringAssert.Contains("\"ev\":\"serve\"", line); StringAssert.EndsWith("}", line);
    }
    [Test] public void FormatBigNumbers() { Assert.AreEqual("999", EconomyCore.Format(999)); Assert.AreEqual("1.5K", EconomyCore.Format(1500)); Assert.AreEqual("12M", EconomyCore.Format(12e6)); }
}
