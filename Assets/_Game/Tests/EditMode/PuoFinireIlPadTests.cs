using NUnit.Framework;
using UltimoMiglio.Gameplay;

/// <summary>Il pad si compra solo se il portafoglio copre il costo ancora da versare.</summary>
public class PuoFinireIlPadTests
{
    [Test]
    public void SottoI40_LoZainoNonSiChiude()
    {
        Assert.IsFalse(TutorialManager.PuoFinireIlPad(39.9, 40, 0));
        Assert.IsTrue(TutorialManager.PuoFinireIlPad(40, 40, 0));
    }

    [Test]
    public void GiaVersato_ContaIlResiduo()
    {
        Assert.IsTrue(TutorialManager.PuoFinireIlPad(10, 40, 30));
        Assert.IsFalse(TutorialManager.PuoFinireIlPad(9, 50, 40));
    }

    [Test]
    public void CostoAssente_NonSiPaga()
    {
        Assert.IsFalse(TutorialManager.PuoFinireIlPad(100, double.PositiveInfinity, 0));
    }
}
