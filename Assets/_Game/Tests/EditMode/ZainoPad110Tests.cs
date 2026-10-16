using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#110 (QA v10): sul livello 2 il pad Zaino, dopo l'acquisto, non deve prosciugare gli LN
/// e il prezzo del livello non ancora comprato resta visibile. Si paga una volta sola.</summary>
public class ZainoPad110Tests
{
    static EconomyCore Core() => TestData.NewCore();

    /// <summary>QA v10 (v10_t5): mostrava 6 LN residui e il portafoglio passava da 26 a 5.
    /// I 6 chiudono lo Zaino 1; il dwell già alto non deve versare lo Zaino 2 nella stessa sosta.</summary>
    [Test]
    public void QaV10_SeiResidui_NonProsciugaIlLivello2()
    {
        var e = Core();
        e.S.Wallet = 26;
        e.S.PadPaid[(int)UpgradeKind.Zaino] = 34; // costo 40, ne restano 6
        Assert.AreEqual(6, e.PrezzoResiduo(UpgradeKind.Zaino), 1e-9);

        bool done = false;
        for (int i = 0; i < 40; i++) done |= e.PadTick(UpgradeKind.Zaino, 5); // dwell alto, come restare sul pad
        Assert.IsTrue(done, "i 6 LN residui devono completare lo Zaino 1");
        Assert.AreEqual(1, e.S.Zaino);
        Assert.AreEqual(20, e.S.Wallet, 1e-9, "26 − 6 = 20: niente addebito sullo Zaino 2");
        Assert.AreEqual(0, e.S.PadPaid[(int)UpgradeKind.Zaino], 1e-9);
        Assert.AreEqual(80, e.PrezzoResiduo(UpgradeKind.Zaino), 1e-9, "prezzo intero del livello 2");

        for (int i = 0; i < 20; i++) Assert.IsFalse(e.PadTick(UpgradeKind.Zaino, 8), "sosta chiusa");
        Assert.AreEqual(20, e.S.Wallet, 1e-9);
        Assert.AreEqual(1, e.S.Zaino);
        Assert.AreEqual(80, e.PrezzoResiduo(UpgradeKind.Zaino), 1e-9);
    }

    [Test]
    public void Livello2_DopoLAcquisto_NonScalaPiu_EIlPrezzoTornaIntero()
    {
        var e = Core();
        e.S.Wallet = 40;
        bool z1 = false;
        for (int i = 0; i < 40 && !z1; i++) z1 = e.PadTick(UpgradeKind.Zaino, i * 0.1);
        Assert.IsTrue(z1);
        Assert.AreEqual(0, e.S.Wallet, 1e-9);
        Assert.AreEqual(80, e.Cost(UpgradeKind.Zaino), 1e-9);

        e.BeginPadStay(UpgradeKind.Zaino); // Nina esce e rientra
        e.S.Wallet = 100;
        bool z2 = false;
        for (int i = 0; i < 40 && !z2; i++) z2 = e.PadTick(UpgradeKind.Zaino, i * 0.1);
        Assert.IsTrue(z2, "lo Zaino 2 si compra una volta, a 80 LN");
        Assert.AreEqual(2, e.S.Zaino);
        Assert.AreEqual(20, e.S.Wallet, 1e-9); // 100 − 80
        Assert.AreEqual(0, e.S.PadPaid[(int)UpgradeKind.Zaino], 1e-9);
        Assert.AreEqual(160, e.PrezzoResiduo(UpgradeKind.Zaino), 1e-9, "prezzo del livello successivo, non un residuo vecchio");

        double ferma = e.S.Wallet;
        for (int i = 0; i < 25; i++) Assert.IsFalse(e.PadTick(UpgradeKind.Zaino, 6));
        Assert.AreEqual(ferma, e.S.Wallet, 1e-9);
        Assert.AreEqual(2, e.S.Zaino);
        Assert.AreEqual(160, e.PrezzoResiduo(UpgradeKind.Zaino), 1e-9);
    }

    [Test]
    public void PillolaPrezzo_RestaVisibileConNinaSulPad()
    {
        var go = new GameObject("Pad_Zaino");
        try
        {
            var pad = go.AddComponent<UpgradePad>();
            var ip = go.AddComponent<InteractionPoint>();
            ip.Kind = PoiKind.Acquisto;
            ip.HasCard = true; // scena: la scheda spegne la pillola quando Nina è vicina
            ip.Pad = null;     // collegata dopo l'Awake (reload / riaccensione)
            pad.EnsurePriceVisible();
            Assert.AreSame(pad, ip.Pad);
            Assert.IsFalse(ip.HasCard, "finché non è acquistato la pillola prezzo resta visibile");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
