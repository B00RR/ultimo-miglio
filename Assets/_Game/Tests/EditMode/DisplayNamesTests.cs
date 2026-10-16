using NUnit.Framework;
using UltimoMiglio.Core;

/// <summary>#129: nomi Bokobay nei testi visibili.</summary>
public class DisplayNamesTests
{
    [Test] public void Pad_AssumiNonno_DiventaAssumiOldo() => Assert.AreEqual("ASSUMI OLDO", DisplayNames.Apply("ASSUMI NONNO"));
    [Test] public void Speaker_NonnaAda_DiventaOmaTilde() => Assert.AreEqual("Oma Tilde", DisplayNames.Apply("Nonna Ada"));
    [Test] public void Titolo_E_Deposito() => Assert.AreEqual("Bokobay · Sunn Depot", DisplayNames.Apply("Ultimo Miglio · Vela & Figli"));
    [Test] public void NomiNuovi_E_Null_Invariati()
    {
        Assert.AreEqual("OLDO +1", DisplayNames.Apply("OLDO +1"));
        Assert.IsNull(DisplayNames.Apply(null));
        Assert.AreEqual("", DisplayNames.Apply(""));
    }
}
