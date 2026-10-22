using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#224 (ALT1, OK del Game Designer): pad Zaino, Scarpe e Insegna in colonna a banPos+(−4,5; +1,9 / 0 / −1,9).
/// Sulla scena reale e con i collider veri (capsula di Kaia r 0,3, griglia 10 cm, Dijkstra):
/// 1) nessun collider solido e nessun altro anello entro 0,4 m dal disco (r 0,70) di ogni pad;
/// 2) Kaia arriva a piedi (fisica, ScriptedInput) su ogni pad, partendo dallo spawn e dal Bancone, e il pad si attiva;
/// 3) camminando sui percorsi principali (Prendi A/B/C e Arrivi → varco ovest → Bancone) nessuno dei tre pad si attiva;
/// 4) a 390x844, con Kaia su ciascun pad, le pillole prezzo dei tre pad non si sovrappongono.</summary>
public class PadRaggiungibili224Test
{
    const float R = 0.70f, Gap = 0.4f, KaiaR = 0.3f, H = 0.1f, X0 = -9f, Z0 = -11f; const int NX = 181, NZ = 221;
    static bool[,] _free;

    static UpgradePad[] Pads()
    {
        var res = new UpgradePad[3];
        foreach (var p in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None))
        { if (p.Kind == UpgradeKind.Zaino) res[0] = p; else if (p.Kind == UpgradeKind.Scarpe) res[1] = p; else if (p.Kind == UpgradeKind.Insegna) res[2] = p; }
        return res;
    }

    static IEnumerator Carica()
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        UltimoMiglio.UI.HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestBloccaPassi = true; TutorialManager.TestAccelera = true; Time.timeScale = 1f;
        DepotCameraFit.TestScreenW = 390; DepotCameraFit.TestScreenH = 844;
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
        var tut = TutorialManager.I;
        for (var s = tut.Current + 1; s <= TutorialManager.Step.PadNuovi; s++) { tut.TestEntraNelPasso(s); yield return null; }
        GameRoot.I.Eco.S.Wallet = 0; // niente acquisti: conta solo l'attivazione (Kaia nel trigger)
        float t = Time.time; while (Time.time - t < 0.5f) yield return null;
        // griglia di passaggio con i collider veri (Kaia esclusa)
        var cc = GameRoot.I.Nina.GetComponent<CharacterController>(); cc.enabled = false;
        _free = new bool[NX, NZ];
        for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
        { var p = new Vector3(X0 + i * H, 0, Z0 + j * H); _free[i, j] = !Physics.CheckCapsule(p + Vector3.up * 0.38f, p + Vector3.up * 1.22f, KaiaR, ~0, QueryTriggerInteraction.Ignore); }
        cc.enabled = true;
    }

    static void Pulisci()
    {
        TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false;
        DepotCameraFit.TestScreenW = DepotCameraFit.TestScreenH = 0;
        if (GameRoot.I && GameRoot.I.Nina) GameRoot.I.Nina.ScriptedInput = Vector3.zero; Time.timeScale = 1f;
    }

    static (int, int) Ix(Vector3 p) => (Mathf.Clamp(Mathf.RoundToInt((p.x - X0) / H), 0, NX - 1), Mathf.Clamp(Mathf.RoundToInt((p.z - Z0) / H), 0, NZ - 1));
    static Vector3 W((int, int) c) => new Vector3(X0 + c.Item1 * H, 0, Z0 + c.Item2 * H);

    /// <summary>Percorso più breve (8 direzioni) fra due punti sulla griglia dei collider veri; null se non c'è.</summary>
    static List<Vector3> Percorso(Vector3 a, Vector3 b)
    {
        var s = Ix(a); var g = Ix(b); var dist = new Dictionary<(int, int), float> { [s] = 0 }; var prev = new Dictionary<(int, int), (int, int)>();
        var q = new SortedSet<(float, int, int)> { (0, s.Item1, s.Item2) };
        while (q.Count > 0)
        {
            var (d, i, j) = q.Min; q.Remove(q.Min);
            if ((i, j) == g) break;
            for (int di = -1; di <= 1; di++) for (int dj = -1; dj <= 1; dj++)
            {
                if (di == 0 && dj == 0) continue; int x = i + di, z = j + dj;
                if (x < 0 || z < 0 || x >= NX || z >= NZ || !_free[x, z]) continue;
                float nd = d + H * Mathf.Sqrt(di * di + dj * dj);
                if (!dist.TryGetValue((x, z), out var od) || nd < od) { if (dist.ContainsKey((x, z))) q.Remove((od, x, z)); dist[(x, z)] = nd; prev[(x, z)] = (i, j); q.Add((nd, x, z)); }
            }
        }
        if (!dist.ContainsKey(g)) return null;
        var pts = new List<Vector3>(); var u = g; while (prev.ContainsKey(u)) { pts.Add(W(u)); u = prev[u]; }
        pts.Reverse(); var tappe = new List<Vector3>(); for (int k = 4; k < pts.Count; k += 5) tappe.Add(pts[k]); tappe.Add(b); return tappe;
    }

    static float Lunghezza(List<Vector3> p, Vector3 a) { float l = 0; var c = a; foreach (var x in p) { l += Vector3.Distance(c, x); c = x; } return l; }

    static void Teletrasporta(NinaController n, Vector3 p) { var cc = n.GetComponent<CharacterController>(); cc.enabled = false; n.transform.position = p + Vector3.up * 0.05f; cc.enabled = true; Physics.SyncTransforms(); }

    /// <summary>Kaia segue le tappe con la fisica vera; <paramref name="ogniFrame"/> viene chiamato a ogni frame.</summary>
    static IEnumerator Segui(NinaController n, List<Vector3> tappe, System.Action ogniFrame)
    {
        foreach (var w in tappe)
        {
            float t0 = Time.time;
            while (Time.time - t0 < 6f)
            {
                var d = w - n.transform.position; d.y = 0; if (d.magnitude < 0.12f) break;
                n.ScriptedInput = d.normalized * Mathf.Clamp(d.magnitude * 3f, 0.35f, 1f); ogniFrame?.Invoke(); yield return null;
            }
        }
        n.ScriptedInput = Vector3.zero;
    }

    [UnityTest, Timeout(600000)]
    public IEnumerator Pad224_LiberiRaggiungibiliEFuoriDaiPercorsi()
    {
        try
        {
            yield return Carica();
            var nina = GameRoot.I.Nina; var pads = Pads(); var log = new StringBuilder();
            foreach (var p in pads) Assert.IsNotNull(p, "pad mancante");

            // 1) niente collider solidi e niente altri anelli entro 0,4 m dal disco
            foreach (var p in pads)
            {
                var c = p.transform.position;
                foreach (var col in Physics.OverlapCapsule(c + Vector3.up * 0.05f, c + Vector3.up * 2.5f, R + Gap, ~0, QueryTriggerInteraction.Ignore))
                    if (!col.GetComponentInParent<NinaController>() && col.name != "Floor") Assert.Fail($"{p.name}: collider solido {col.name} entro {R + Gap:0.00} m dal centro");
                foreach (var ip in Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None))
                {
                    if (ip.transform == p.transform || ip.GetComponentInParent<UpgradePad>() == p) continue;
                    var o = ip.transform.position; o.y = c.y; float gap = Vector3.Distance(c, o) - R - ip.Radius;
                    Assert.GreaterOrEqual(gap, Gap - 0.01f, $"{p.name}: anello di {ip.name} a {gap:0.00} m");
                }
            }

            // 2) raggiungibili a piedi da spawn e Bancone, e il pad si attiva
            var bancone = GameObject.Find("Zona_Bancone").transform.position;
            foreach (var (nome, da) in new[] { ("spawn", NinaController.SafeSpawn), ("Bancone", bancone) })
                foreach (var p in pads)
                {
                    Teletrasporta(nina, da); yield return null; yield return null;
                    var tappe = Percorso(nina.transform.position, p.transform.position); Assert.IsNotNull(tappe, $"{p.name} irraggiungibile da {nome}");
                    log.AppendLine($"{nome} → {p.name}: {Lunghezza(tappe, da):0.00} m");
                    yield return Segui(nina, tappe, null);
                    float t0 = Time.time; while (Time.time - t0 < 0.3f && !p.Occupied) yield return null;
                    var dk = p.transform.position - nina.transform.position; dk.y = 0;
                    Assert.Less(dk.magnitude, 0.3f, $"{nome} → {p.name}: Kaia ferma a {dk.magnitude:0.00} m dal centro");
                    Assert.IsTrue(p.Occupied, $"{nome} → {p.name}: Kaia sul pad ma il pad non si attiva");
                    nina.ScriptedInput = Vector3.back; t0 = Time.time; while (Time.time - t0 < 0.5f) yield return null; nina.ScriptedInput = Vector3.zero;
                }

            // 3) percorsi principali: nessun pad si attiva
            foreach (var z in new[] { "Zona_Prendi_A", "Zona_Prendi_B", "Zona_Prendi_C", "Zona_Arrivi" })
            {
                var a = GameObject.Find(z).transform.position; Teletrasporta(nina, a); yield return null; yield return null;
                var tappe = Percorso(nina.transform.position, bancone); Assert.IsNotNull(tappe, $"{z} → Bancone irraggiungibile");
                float minD = 99f; UpgradePad attivo = null;
                yield return Segui(nina, tappe, () =>
                {
                    foreach (var p in pads)
                    {
                        var d = p.transform.position - nina.transform.position; d.y = 0; minD = Mathf.Min(minD, d.magnitude);
                        if (p.Occupied) attivo = p;
                    }
                });
                log.AppendLine($"{z} → Bancone: {Lunghezza(tappe, a):0.00} m, distanza minima dai pad {minD:0.00} m");
                Assert.IsNull(attivo, $"{z} → Bancone: attraversando si attiva {(attivo ? attivo.name : "")}");
                Assert.Greater(minD, 1.0f, $"{z} → Bancone: il percorso passa a {minD:0.00} m da un pad (serve > 1 m, OK GD)");
            }

            // 4) pillole prezzo a 390x844 (formula di InteractionPoint): con Kaia su ogni pad non si sovrappongono
            var cam = Camera.main; var fit = Object.FindAnyObjectByType<DepotCameraFit>();
            var rt = new RenderTexture(390, 844, 24); var prevRt = cam.targetTexture;
            foreach (var su in pads)
            {
                Teletrasporta(nina, su.transform.position); if (fit) fit.SnapView();
                float t0 = Time.time; while (Time.time - t0 < 1.2f) yield return null;
                cam.targetTexture = rt; var rects = new List<(string, Rect)>();
                foreach (var p in pads)
                {
                    var sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * 1.55f);
                    if (sp.y > 844 * 0.72f) sp = cam.WorldToScreenPoint(p.transform.position + Vector3.up * 1.05f);
                    if (sp.z <= 0 || sp.x < 0 || sp.x > 390 || sp.y < 0 || sp.y > 844) continue;
                    float x = Mathf.Clamp(sp.x, 80f, 310f), y = 844f - sp.y; rects.Add((p.name, new Rect(x - 70f, y - 92f, 140f, 92f))); // freccia + barra LN + pillola
                }
                cam.targetTexture = prevRt;
                for (int i = 0; i < rects.Count; i++) for (int j = i + 1; j < rects.Count; j++)
                    Assert.IsFalse(rects[i].Item2.Overlaps(rects[j].Item2), $"Kaia su {su.name}: pillole di {rects[i].Item1} e {rects[j].Item1} sovrapposte ({rects[i].Item2} / {rects[j].Item2})");
                log.AppendLine($"Kaia su {su.name}: pillole a schermo {rects.Count}");
            }
            Object.Destroy(rt);
            Debug.Log("[#224]\n" + log);
        }
        finally { Pulisci(); }
    }
}
