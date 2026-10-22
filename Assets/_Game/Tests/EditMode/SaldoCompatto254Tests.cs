using NUnit.Framework;
using UltimoMiglio.UI;

/// <summary>#254: il saldo compatto si tronca alla precisione già in uso (un decimale, separatori it-IT).
/// 999999 resta «999,9K», mai «1000K». M da 1.000.000, B da 1.000.000.000.</summary>
public class SaldoCompatto254Tests
{
    [TestCase(9999d, "9.999")]
    [TestCase(99999d, "99.999")]
    [TestCase(100000d, "100K")]
    [TestCase(999999d, "999,9K")]
    [TestCase(1000000d, "1M")]
    [TestCase(1999999d, "1,9M")]
    [TestCase(999999999d, "999,9M")]
    [TestCase(0d, "0")]
    [TestCase(999950d, "999,9K")]
    [TestCase(-999999d, "-999,9K")]
    [TestCase(-9.99d, "-9,9")]
    [TestCase(9.99d, "9,9")]
    [TestCase(-0.05d, "0")]
    public void NonSuperaIlSaldoReale(double saldo, string atteso) =>
        Assert.AreEqual(atteso, HudFormat.Amount(saldo));
}
