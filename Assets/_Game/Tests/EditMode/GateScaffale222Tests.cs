using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#222: il gate shelve non deve passare quando lo scaffale terracotta compare (SetActive) con i suoi pacchi iniziali.
/// Regola GD: la base si calcola dopo il reveal e la cache degli scaffali; serve 1 pacco depositato dopo l'inizio del passo.</summary>
public class GateScaffale222Tests
{
    readonly List<GameObject> _go = new List<GameObject>();
    [TearDown] public void Pulisci() { foreach (var g in _go) Object.DestroyImmediate(g); _go.Clear(); }

    Shelf Scaffale(int count, bool attivo)
    {
        var g = new GameObject("Shelf222"); _go.Add(g); g.AddComponent<BoxCollider>();
        var sh = g.AddComponent<Shelf>(); sh.Count = count; g.SetActive(attivo);
        return sh;
    }
    // come AggiornaCache: solo gli scaffali attivi (FindObjectsByType salta gli inattivi); filtrati sui GameObject del test
    static void Cache(List<Shelf> c) { c.Clear(); foreach (var sh in Object.FindObjectsByType<Shelf>(FindObjectsSortMode.None)) if (sh.name == "Shelf222") c.Add(sh); }

    [Test] public void ScaffaleCheCompareAlPasso_NonChiudeIlPasso()
    {
        var sh = Scaffale(4, attivo: false); // nascosto dalla comparsa graduale, 4 pacchi iniziali
        var cache = new List<Shelf>(); Cache(cache);
        Assert.AreEqual(0, TutorialManager.ShelfTotal(cache), "prima del reveal lo scaffale nascosto non è in cache (causa del bug)");
        int baseInizio = TutorialManager.BaseScaffaliDopoComparsa(() => { sh.gameObject.SetActive(true); Cache(cache); }, cache);
        Assert.AreEqual(4, baseInizio);
        Assert.IsFalse(TutorialManager.ShelfTotal(cache) > baseInizio, "bug QA: gate shelve passato senza deposito");
        sh.Add(1); // 1 pacco depositato dopo l'inizio del passo
        Assert.IsTrue(TutorialManager.ShelfTotal(cache) > baseInizio);
    }
}
