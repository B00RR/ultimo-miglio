using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#274: contro lo spigolo del vaso (BoxCollider) la diagonale deve scivolare, la spinta dritta su una faccia no.</summary>
public class VasoSpigolo274Tests
{
    [Test]
    public void FacciaSud_SpintaDritta_SiFerma()
    {
        // Normale verso sud (il vaso sta a nord): la spinta dritta va a nord, dentro la faccia.
        var v = NinaController.Sblocca(new Vector3(0f, 0f, 3f), Vector3.back, Vector3.zero);
        Assert.Less(v.magnitude, 0.05f, "dritta contro la faccia non deve scivolare di lato");
    }

    [Test]
    public void FacciaSud_Diagonale_ScivolaAEst()
    {
        var v = NinaController.Sblocca(new Vector3(1f, 0f, 1f), Vector3.back, Vector3.zero);
        Assert.Greater(v.x, 0.9f, "la diagonale scivola verso est");
        Assert.AreEqual(0f, v.z, 0.05f, "non entra nella faccia sud");
    }

    [Test]
    public void Spigolo_DiagonaleNellaNormale_NonRestaFerma()
    {
        var normale = new Vector3(0.7f, 0f, -0.7f).normalized;
        var v = NinaController.Sblocca(normale * -3f, normale, Vector3.zero);
        Assert.Greater(v.magnitude, 2.5f, "nello spigolo Kaia prende un lato invece di restare incastrata");
        Assert.Less(Mathf.Abs(Vector3.Dot(v.normalized, normale)), 0.2f, "il lato è lungo il muro, non dentro");
    }

    [Test]
    public void DueFacce_Diagonale_ScivolaSullaSeconda()
    {
        var wish = new Vector3(1f, 0f, 1f);
        var v = NinaController.Sblocca(wish, (-wish).normalized, Vector3.back);
        Assert.Greater(v.x, 0.9f, "la seconda faccia (sud) lascia scivolare a est");
        Assert.AreEqual(0f, v.z, 0.05f, "non entra nella faccia sud");
    }
}
