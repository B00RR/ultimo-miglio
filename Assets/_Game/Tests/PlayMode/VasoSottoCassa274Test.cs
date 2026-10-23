using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>#274: dalla Cassaforte verso sud, la diagonale contro il vaso non lascia Kaia ferma.</summary>
public class VasoSottoCassa274Test
{
    [UnityTest, Timeout(120000)]
    public IEnumerator DiagonaleSulVaso_SottoLaCassaforte_Scivola()
    {
        PlayerPrefs.DeleteKey(TutorialManager.PrefKey);
        PlayerPrefs.DeleteKey("um_save_v1");
        PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 0);
        TutorialManager.TestSenzaComparse = true;
        TutorialManager.TestAccelera = true;
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 90 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");

        var nina = GameRoot.I.Nina;
        Assert.IsNotNull(nina);
        Collider cassa = null, vaso = null;
        float migliore = 999f;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!c || c.isTrigger || c is CharacterController) continue;
            var n = c.name.ToLowerInvariant();
            if (n.Contains("safe") || n.Contains("cassaforte")) cassa = c;
        }
        Assert.IsNotNull(cassa, "Cassaforte non trovata");
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!c || c.isTrigger || c is CharacterController) continue;
            var n = c.name.ToLowerInvariant();
            if (!n.Contains("pot") && !n.Contains("vaso") && !n.Contains("plant")) continue;
            if (c.bounds.size.y < 0.25f) continue;
            if (c.bounds.center.z > cassa.bounds.center.z - 0.2f) continue;
            float d = Horizontal(c.bounds.center - cassa.bounds.center);
            if (d < migliore) { migliore = d; vaso = c; }
        }
        Assert.IsNotNull(vaso, "nessun vaso a sud della Cassaforte");

        var b = vaso.bounds;
        var angolo = new Vector3(b.min.x, 0.05f, b.min.z);
        var fuori = angolo + new Vector3(-1f, 0f, -1f).normalized * 0.45f;
        var cc = nina.GetComponent<CharacterController>();
        cc.enabled = false;
        nina.transform.position = fuori;
        cc.enabled = true;
        Physics.SyncTransforms();
        var verso = b.center - fuori; verso.y = 0f; verso.Normalize();
        nina.ScriptedInput = verso;
        yield return null;
        var partenza = nina.transform.position;
        float t0 = Time.time;
        while (Time.time - t0 < 1.1f) yield return null;
        nina.ScriptedInput = Vector3.zero;
        var fatto = nina.transform.position - partenza; fatto.y = 0f;
        var vicino = vaso.ClosestPoint(nina.transform.position + Vector3.up * 0.8f);
        float dist = Horizontal(nina.transform.position - vicino);
        Assert.Greater(fatto.magnitude, 0.35f, "Kaia resta ferma contro il vaso " + vaso.name + " a " + b.center);
        Assert.Greater(dist, 0.15f, "Kaia è entrata nel vaso");
    }

    static float Horizontal(Vector3 v) { v.y = 0f; return v.magnitude; }
}
