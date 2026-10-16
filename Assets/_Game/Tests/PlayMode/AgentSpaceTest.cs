using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#111 (checklist C12/C13): NONNO e i clienti non entrano nello spazio di Nina né si sovrappongono tra loro.
/// Fallisce se la distanza piana tra i centri scende sotto la somma dei raggi (tolleranza 5 cm).</summary>
public class AgentSpaceTest
{
    const float Tol = 0.05f;
    static float NinaR(NinaController n) => n.GetComponent<CharacterController>().radius;
    static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    static IEnumerator Load()
    {
        PlayerPrefs.DeleteAll(); Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 60 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready);
    }
    static void Place(NinaController n, Vector3 p)
    {
        var cc = n.GetComponent<CharacterController>(); cc.enabled = false; n.transform.position = new Vector3(p.x, n.transform.position.y, p.z); cc.enabled = true;
    }

    [UnityTest] public IEnumerator Nonno_NonPassaSopraNinaSuArrivi()
    {
        yield return Load();
        var eco = GameRoot.I.Eco; eco.S.Wallet = 100;
        bool done = eco.S.NonnoLevel >= 0; for (int i = 0; i < 500 && !done; i++) done = eco.PadTick(UpgradeKind.NonnoHire, i * 0.1);
        var bot = Object.FindAnyObjectByType<NonnoRobot>(); var n = GameRoot.I.Nina; Assert.IsNotNull(bot); Assert.IsNotNull(n);
        if (bot.Arrivals) bot.Arrivals.Stock = 30;
        float sum = NinaR(n) + 0.45f, min = float.MaxValue, t = 0; bool worked = false;
        while (t < 15f)
        {
            Place(n, bot.Arrivals.transform.position); // Nina ferma sulla meta di NONNO (passo QA v9)
            yield return null; t += Time.deltaTime;
            min = Mathf.Min(min, Flat(bot.transform.position, n.transform.position));
            worked |= bot.CarryCount > 0;
        }
        Debug.Log($"[AgentSpace] NONNO-Nina min={min:F2} somma raggi={sum:F2} carico={worked}");
        Assert.GreaterOrEqual(min, sum - Tol, $"NONNO entra nello spazio di Nina: {min:F2} < {sum:F2}");
        Assert.IsTrue(worked, "NONNO non carica più con Nina su Arrivi");
    }

    [UnityTest] public IEnumerator Clienti_NonCompenetranoNinaNeTraLoro()
    {
        yield return Load();
        var q = Object.FindAnyObjectByType<CustomerQueue>(); var n = GameRoot.I.Nina; Assert.IsNotNull(q); Assert.IsNotNull(n);
        // Nina sul percorso della coda, tra la porta e il bancone (passo QA v9: lungo la coda all'angolo SE)
        var spot = q.CounterSlot.position + q.LineStep * 2f; // dentro la fila
        float minN = float.MaxValue, minC = float.MaxValue, t = 0, maxJump = 0; int seen = 0;
        var prev = new System.Collections.Generic.Dictionary<Customer, Vector3>();
        while (t < 20f)
        {
            Place(n, spot);
            yield return null; t += Time.deltaTime;
            var cs = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToArray(); seen = Mathf.Max(seen, cs.Length);
            foreach (var c in cs) minN = Mathf.Min(minN, Flat(c.transform.position, n.transform.position));
            // niente teletrasporti: spostamento per frame entro la velocità massima (+ separazione 3 cm); i clienti appena (ri)spawnati alla porta esclusi
            var next = new System.Collections.Generic.Dictionary<Customer, Vector3>();
            foreach (var c in cs) { if (prev.TryGetValue(c, out var p0)) maxJump = Mathf.Max(maxJump, Flat(c.transform.position, p0) - (c.Speed * Time.deltaTime + 0.03f)); next[c] = c.transform.position; }
            prev = next;
            for (int i = 0; i < cs.Length; i++) for (int j = i + 1; j < cs.Length; j++) minC = Mathf.Min(minC, Flat(cs[i].transform.position, cs[j].transform.position));
        }
        Debug.Log($"[AgentSpace] salto oltre velocità max={maxJump:F3} m");
        Assert.LessOrEqual(maxJump, 0.01f, $"cliente teletrasportato: salto di {maxJump:F2} m oltre la velocità massima per frame");
        Debug.Log($"[AgentSpace] clienti={seen} Nina-cliente min={minN:F2} cliente-cliente min={minC:F2}");
        Assert.Greater(seen, 1, "servono almeno 2 clienti per il test");
        Assert.GreaterOrEqual(minN, NinaR(n) + Customer.BodyRadius - Tol, $"un cliente entra nello spazio di Nina: {minN:F2}");
        Assert.GreaterOrEqual(minC, 2 * Customer.BodyRadius - Tol, $"clienti sovrapposti: {minC:F2}");
    }
}
