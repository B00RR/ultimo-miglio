using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#171: sulla scena generata il bordo esterno della banda di fx_pad_fill_ring coincide col cerchio del pad (POI_Ring) ±0,01 m,
/// e Kaia ha l'ombra a disco sotto i piedi, centrata sul pivot.</summary>
public class FxAnelloPad171Tests
{
    const string Scene = "Assets/_Game/Scenes/Ch1_Deposito.unity";
    [Test]
    public void FxPadFillRing_RaggioMondo_UgualeAnello()
    {
        EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var pads = Object.FindObjectsByType<UpgradePad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.Greater(pads.Length, 0, "nessun pad in scena");
        foreach (var pad in pads)
        {
            var ip = pad.GetComponent<InteractionPoint>(); Assert.IsNotNull(ip, pad.name);
            float anello = ip.RingVisual.lossyScale.x * 0.5f; // POI_Ring: disco Ø 1 a scala 1
            Assert.AreEqual(ip.Radius, anello, 0.01f, $"{pad.name}: POI_Ring");
            Transform quad = null; foreach (var t in pad.GetComponentsInChildren<Transform>(true)) if (t.name == "ring_quad") quad = t;
            Assert.IsNotNull(quad, $"{pad.name}: manca fx_pad_fill_ring/ring_quad");
            var m = quad.GetComponent<MeshRenderer>().sharedMaterial;
            float fx = (m.GetFloat("_Radius") + m.GetFloat("_Thickness") * 0.5f) * quad.lossyScale.x;
            Assert.AreEqual(anello, fx, 0.01f, $"{pad.name}: raggio mondo fx {fx:0.000} ≠ anello {anello:0.000}");
            Assert.Less(Vector2.Distance(new Vector2(quad.position.x, quad.position.z), new Vector2(ip.RingVisual.position.x, ip.RingVisual.position.z)), 0.01f, $"{pad.name}: fx non centrato");
        }
    }
    [Test]
    public void Kaia_HaOmbraPiedi_Centrata()
    {
        EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var nina = Object.FindFirstObjectByType<NinaController>(); Assert.IsNotNull(nina);
        var o = nina.transform.Find("OmbraPiedi"); Assert.IsNotNull(o, "manca OmbraPiedi sotto Kaia");
        var mf = o.GetComponent<MeshFilter>(); Assert.IsNotNull(mf.sharedMesh); Assert.LessOrEqual(mf.sharedMesh.triangles.Length / 3, 32, "non low-poly");
        var b = o.GetComponent<MeshRenderer>().bounds; Assert.AreEqual(0.42f, b.extents.x, 0.02f); Assert.Less(Vector2.Distance(new Vector2(b.center.x, b.center.z), new Vector2(nina.transform.position.x, nina.transform.position.z)), 0.01f);
        Assert.IsNull(nina.transform.Find("fx_contact_shadow"), "doppia ombra su Kaia");
    }
}
