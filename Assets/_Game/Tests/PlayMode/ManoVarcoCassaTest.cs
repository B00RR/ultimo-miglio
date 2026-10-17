using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#256: al passo cassaPass, con Kaia dalla parte opposta dell'arco della Cassa (linea retta chiusa dal muro), la mano del
/// tutorial punta all'arco e non dritta alla Cassa; seguendo la guida Kaia arriva sulla Cassa. Il marker del varco qui è finto,
/// creato a runtime sull'arco attuale della scena reale: quelli veri li mette il layout (PR del 3D Artist).</summary>
public class ManoVarcoCassaTest
{
    [UnityTest, Timeout(180000)]
    public IEnumerator Mano_PuntaAllArco_ESeguendolaKaiaArrivaAllaCassa()
    {
        PlayerPrefs.DeleteAll(); var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        UltimoMiglio.UI.HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestBloccaPassi = true; Time.timeScale = 1f;
        GameObject finto = null;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var tut = TutorialManager.I; var nina = GameRoot.I.Nina; var cc = nina.GetComponent<CharacterController>();
            for (var s = tut.Current + 1; s <= TutorialManager.Step.CassaPass; s++) { tut.TestEntraNelPasso(s); yield return null; }
            Assert.AreEqual(TutorialManager.Step.CassaPass, tut.Current);
            var cassaZ = Object.FindAnyObjectByType<CassaZone>(); Assert.IsNotNull(cassaZ, "Zona_Cassa mancante");
            var cassa = tut.CurrentTarget; Assert.IsNotNull(cassa, "nessun bersaglio al passo cassaPass");
            var hints = tut.GetComponent<OnboardingHints>(); Assert.IsNotNull(hints, "OnboardingHints mancante");

            // arco attuale della Cassa (layout #97): l'env_wall_arch_4m più vicino alla Cassa, marker finto al centro dell'apertura
            var arco = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("env_wall_arch_4m"))
                .OrderBy(t => Flat(t.position, cassa.position)).FirstOrDefault();
            Assert.IsNotNull(arco, "arco della Cassa non trovato");
            var rends = arco.GetComponentsInChildren<Renderer>(); Assert.Greater(rends.Length, 0, "arco senza mesh"); var bb = rends[0].bounds; foreach (var r in rends) bb.Encapsulate(r.bounds);
            Vector3 varco = new Vector3(bb.center.x, 0f, bb.center.z); // centro dell'apertura (il pivot dell'fbx è a un'estremità del modulo da 4 m)
            finto = new GameObject("Varchi_test");
            Marker(finto, "Varco_Cassa_test", varco, 0, 1.6f); // contratto con il layout: arco della Cassa ordine 0
            // Con il layout attuale, oltre l'arco la Cassa non è ancora dritta (muro del set a z −4,19, #259: il giro passa dal varco ovest).
            // Gli altri marker finti si ricavano dalla fisica: BFS a griglia (capsula di Kaia) dall'arco alla Cassa, angoli del percorso
            // (string pulling con la stessa sonda della guida). Con il nuovo layout del 3D Artist resterà il solo arco.
            var giro = Percorso(varco, cassaZ.transform.position, cc.radius + 0.02f, cassa);
            Assert.IsNotNull(giro, "nessun percorso fisico dall'arco alla Cassa");
            for (int i = 1; i < giro.Count - 1; i++) Marker(finto, "Varco_giro_test_" + i, giro[i], i, 1.6f);
            Debug.Log("[#256] giro arco→Cassa: " + string.Join(" → ", giro.Select(g => $"({g.x:0.00};{g.z:0.00})")));

            // Kaia dalla parte opposta: punto libero a 1,5-3 m dall'arco, dall'altro lato rispetto alla Cassa, con la linea retta verso la Cassa chiusa
            var fwd = arco.forward; fwd.y = 0; fwd.Normalize(); float lato = Mathf.Sign(Vector3.Dot(cassa.position - varco, fwd));
            Vector3 start = Vector3.zero; bool trovato = false; float bestAng = 0f;
            for (float dz = 1.0f; dz <= 4.01f; dz += 0.25f)
                for (float dx = -3f; dx <= 3.01f; dx += 0.25f)
                {
                    var p = varco - fwd * lato * dz + arco.right * dx; p.y = nina.transform.position.y;
                    if (Physics.CheckCapsule(p + Vector3.up * 0.35f, p + Vector3.up * 1.3f, cc.radius + 0.05f, ~0, QueryTriggerInteraction.Ignore)) continue; // non dentro un solido
                    if (!VarcoMarker.Chiuso(p, cassa.position, cassa)) continue; // la linea retta deve essere chiusa
                    if (VarcoMarker.Chiuso(p, varco, null, 0f)) continue; // l'arco deve essere raggiungibile dritto
                    float ang = Vector3.Angle(Flat3(varco - p), Flat3(cassa.position - p));
                    if (ang > bestAng) { bestAng = ang; start = p; trovato = true; }
                }
            Assert.IsTrue(trovato, "nessun punto di partenza con l'arco in mezzo");
            Assert.Greater(bestAng, 25f, "arco e Cassa nella stessa direzione: il test non distinguerebbe");
            Debug.Log($"[#256] arco={varco:F2} cassa={cassa.position:F2} start={start:F2} angolo arco/cassa={bestAng:0}°");

            Metti(nina, cc, start); yield return null; yield return new WaitForSeconds(OnboardingHints.TappaSec + 0.1f);
            var cam = Camera.main; Assert.IsNotNull(cam);
            Vector2 versoArco = Pannello(cam, nina.transform.position, varco), versoCassa = Pannello(cam, nina.transform.position, cassa.position);
            float aArco = Vector2.Angle(hints.DirezioneMano, versoArco), aCassa = Vector2.Angle(hints.DirezioneMano, versoCassa);
            Debug.Log($"[#256] mano={hints.DirezioneMano:F2} versoArco={versoArco:F2} ({aArco:0}°) versoCassa={versoCassa:F2} ({aCassa:0}°)");
            Assert.Less(aArco, 10f, "la mano non punta all'arco");
            Assert.Greater(aCassa, 15f, "la mano punta dritta alla Cassa, attraverso il muro");

            // seguendo la guida (direzione verso la tappa, ricalcolata ogni 0,2 s) Kaia arriva sulla Cassa
            float t0 = Time.time; bool arrivata = false;
            while (Time.time - t0 < 45f)
            {
                var d = hints.Tappa - nina.transform.position; d.y = 0;
                nina.ScriptedInput = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.zero;
                yield return null;
                if (cassaZ.Occupied) { arrivata = true; break; }
            }
            nina.ScriptedInput = Vector3.zero;
            Debug.Log($"[#256] arrivata={arrivata} in {Time.time - t0:0.0} s, Kaia={nina.transform.position:F2}");
            Assert.IsTrue(arrivata, $"seguendo la mano Kaia non arriva alla Cassa (ferma a {nina.transform.position:F2})");
        }
        finally
        {
            if (finto) Object.Destroy(finto);
            TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; Time.timeScale = 1f;
        }
    }

    /// <summary>BFS su griglia 0,2 m nel piano (capsula di raggio r libera), poi angoli: si tiene il punto più lontano visibile con la sonda della guida.</summary>
    static System.Collections.Generic.List<Vector3> Percorso(Vector3 a, Vector3 b, float r, Transform bersaglio)
    {
        const float h = 0.2f; float x0 = -9f, z0 = -11f; int nx = 90, nz = 110;
        System.Func<int, int, bool> libero = (i, j) => { var p = new Vector3(x0 + i * h, 0, z0 + j * h); return !Physics.CheckCapsule(p + Vector3.up * (r + 0.1f), p + Vector3.up * 1.4f, r, ~0, QueryTriggerInteraction.Ignore); };
        int ai = Mathf.RoundToInt((a.x - x0) / h), aj = Mathf.RoundToInt((a.z - z0) / h), bi = Mathf.RoundToInt((b.x - x0) / h), bj = Mathf.RoundToInt((b.z - z0) / h);
        var prev = new int[nx * nz]; for (int k = 0; k < prev.Length; k++) prev[k] = -2;
        var q = new System.Collections.Generic.Queue<int>(); prev[ai * nz + aj] = -1; q.Enqueue(ai * nz + aj); int fine = -1;
        int[] di = { 1, -1, 0, 0 }, dj = { 0, 0, 1, -1 };
        while (q.Count > 0)
        {
            int c = q.Dequeue(), ci = c / nz, cj = c % nz;
            if (Mathf.Abs(ci - bi) <= 1 && Mathf.Abs(cj - bj) <= 1) { fine = c; break; }
            for (int k = 0; k < 4; k++) { int ni = ci + di[k], nj = cj + dj[k]; if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) continue; int n = ni * nz + nj; if (prev[n] != -2) continue; if (!libero(ni, nj)) { prev[n] = -3; continue; } prev[n] = c; q.Enqueue(n); }
        }
        if (fine < 0) return null;
        var celle = new System.Collections.Generic.List<Vector3>(); for (int c = fine; c >= 0; c = prev[c]) celle.Add(new Vector3(x0 + (c / nz) * h, 0, z0 + (c % nz) * h)); celle.Reverse(); celle.Add(b);
        var res = new System.Collections.Generic.List<Vector3> { a }; int cur = 0;
        while (cur < celle.Count - 1)
        {
            int far = cur + 1;
            for (int k = celle.Count - 1; k > cur + 1; k--) if (!VarcoMarker.Chiuso(celle[cur], celle[k], bersaglio, 0f)) { far = k; break; }
            res.Add(celle[far]); cur = far;
        }
        return res;
    }
    static void Marker(GameObject root, string nome, Vector3 p, int ordine, float larghezza) { var g = new GameObject(nome); g.transform.SetParent(root.transform); g.transform.position = p; var m = g.AddComponent<VarcoMarker>(); m.ordine = ordine; m.larghezza = larghezza; }
    static Vector2 Pannello(Camera cam, Vector3 a, Vector3 b) { Vector3 sa = cam.WorldToScreenPoint(a), sb = cam.WorldToScreenPoint(b); return new Vector2(sb.x - sa.x, -(sb.y - sa.y)).normalized; }
    static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    static Vector3 Flat3(Vector3 v) { v.y = 0; return v; }
    static void Metti(NinaController n, CharacterController cc, Vector3 p) { cc.enabled = false; n.transform.position = p; cc.enabled = true; Physics.SyncTransforms(); }
}
