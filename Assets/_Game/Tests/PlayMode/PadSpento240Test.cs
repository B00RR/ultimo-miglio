using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Gameplay;
using UltimoMiglio.VFX;

/// <summary>#240: dopo l'acquisto il pad è spento (anello desaturato, glow a 0, prezzo grigio, freccia verso l'esterno)
/// finché Kaia non esce; all'uscita torna attivo con il pop di 0,2 s. Lo stato segue il blocco _waitExit della #110.</summary>
public class PadSpento240Test
{
    GameObject _go; UpgradePad _pad; UM_PadFillRing _ring; InteractionPoint _ip;

    [SetUp]
    public void Build()
    {
        _go = new GameObject("Pad_240");
        _go.AddComponent<BoxCollider>().isTrigger = true;
        var ringGo = GameObject.CreatePrimitive(PrimitiveType.Quad); ringGo.name = "FillRing";
        Object.DestroyImmediate(ringGo.GetComponent<Collider>());
        ringGo.transform.SetParent(_go.transform, false);
        _ring = ringGo.AddComponent<UM_PadFillRing>(); _ring.ring = ringGo.GetComponent<Renderer>(); _ring.playCompleteBurst = false;
        _ip = _go.AddComponent<InteractionPoint>(); _ip.Kind = PoiKind.Acquisto;
        _pad = _go.AddComponent<UpgradePad>(); // Awake ricollega FillRing dal figlio e collega l'InteractionPoint
    }

    [TearDown] public void Clean() { if (_go) Object.DestroyImmediate(_go); }

    [Test]
    public void DopoLAcquisto_Spento_PoiAttivoConPopAllUscita()
    {
        Assert.AreSame(_ring, _pad.FillRing);
        Assert.IsFalse(_pad.IsArmedOff); Assert.IsFalse(_ring.IsOff);

        _pad.TestCompletePurchase();
        Assert.IsTrue(_pad.IsArmedOff, "_waitExit (#110) attivo dopo l'acquisto");
        Assert.IsTrue(_ring.IsOff);
        Assert.AreEqual(0f, _ring.GlowIntensity, 1e-6, "da spento glow/emissione a 0");
        var active = UM_PadFillRing.ColorOf(_ring.kind);
        var off = _ring.AppliedColor;
        Assert.AreEqual(UM_PadOffStyle.RingAlpha, off.a, 1e-4);
        var atteso = Color.Lerp(active, UM_PadOffStyle.Crema, UM_PadOffStyle.RingDesat);
        Assert.AreEqual(atteso.r, off.r, 1e-4); Assert.AreEqual(atteso.g, off.g, 1e-4); Assert.AreEqual(atteso.b, off.b, 1e-4);
        Assert.IsTrue(_ip.PadOff); Assert.AreEqual(UM_PadOffStyle.Price, _ip.PriceColor, "prezzo grigio da spento");

        // restando sul pad non torna attivo: niente riarmo a tempo
        for (int i = 0; i < 50; i++) _ring.TickPop(0.1f);
        Assert.IsTrue(_pad.IsArmedOff); Assert.IsTrue(_ring.IsOff); Assert.AreEqual(0f, _ring.GlowIntensity, 1e-6);

        Vector3 s0 = _ring.transform.localScale;
        _pad.TestExit(); // Kaia esce dal raggio
        Assert.IsFalse(_pad.IsArmedOff, "uscita: _waitExit sbloccato");
        Assert.IsFalse(_ring.IsOff); Assert.IsTrue(_ring.IsPopping);
        Assert.IsFalse(_ip.PadOff);
        Assert.AreNotEqual(UM_PadOffStyle.Price, _ip.PriceColor);

        _ring.TickPop(0.04f); // dentro i primi 0,08 s: flash del colore attivo pieno
        Assert.AreEqual(active, _ring.AppliedColor);
        Assert.Greater(_ring.GlowIntensity, 0.99f);

        float max = 0f;
        for (int i = 0; i < 20; i++) { _ring.TickPop(0.01f); max = Mathf.Max(max, _ring.transform.localScale.x / s0.x); }
        Assert.AreEqual(UM_PadOffStyle.PopPeak, max, 0.02f, "pop fino a 1,15");
        Assert.IsFalse(_ring.IsPopping, "pop chiuso entro 0,2 s");
        Assert.AreEqual(s0.x, _ring.transform.localScale.x, 1e-5, "scala tornata a 1");
    }

    [Test]
    public void PopScale_UnoPicco115Uno()
    {
        Assert.AreEqual(1f, UM_PadOffStyle.PopScale(0f), 1e-5);
        Assert.AreEqual(1f, UM_PadOffStyle.PopScale(UM_PadOffStyle.PopTime), 1e-5);
        float max = 0f; for (int i = 0; i <= 400; i++) max = Mathf.Max(max, UM_PadOffStyle.PopScale(i * UM_PadOffStyle.PopTime / 400f));
        Assert.AreEqual(UM_PadOffStyle.PopPeak, max, 1e-3);
    }
}
