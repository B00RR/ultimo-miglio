using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#256/#262: i VarcoMarker di Ch1_Deposito hanno <see cref="VarcoMarker.ordine"/> tutti diversi (0 = arco della Cassa, poi 1, 2, 3...),
/// così una parità fra varchi nella guida non dipende mai dall'ordine di registrazione; larghezza ≥ 1,2 m.</summary>
public class VarchiOrdine256Tests
{
    [Test]
    public void Varchi_OrdineUnico_ArcoCassaZero_LarghezzaMinima()
    {
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Ch1_Deposito.unity", OpenSceneMode.Single);
        var v = Object.FindObjectsByType<VarcoMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(m => m.ordine).ToArray();
        Assert.GreaterOrEqual(v.Length, 4, "attesi almeno 4 varchi in Ch1_Deposito");
        var doppi = v.GroupBy(m => m.ordine).Where(g => g.Count() > 1).Select(g => g.Key + ": " + string.Join(", ", g.Select(m => m.name))).ToArray();
        Assert.IsEmpty(doppi, "ordine ripetuto fra varchi: " + string.Join("; ", doppi));
        for (int i = 0; i < v.Length; i++) Assert.AreEqual(i, v[i].ordine, $"ordine non consecutivo da 0: {v[i].name} ha {v[i].ordine}");
        Assert.AreEqual("Varco_ArcoBancone", v[0].name, "l'ordine 0 spetta all'arco della Cassa");
        foreach (var m in v) Assert.GreaterOrEqual(m.larghezza, 1.2f, $"{m.name}: larghezza {m.larghezza:0.00} m < 1,2 m");
    }
}
