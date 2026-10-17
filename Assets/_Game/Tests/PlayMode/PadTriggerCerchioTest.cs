using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#171: l'area attiva di ogni UpgradePad coincide con il disco visibile, misurata dal pivot (piedi) di Kaia.
/// Scena reale, partita nuova (niente salvataggio), tutorial NON saltato: lo stato viene portato passo per passo fino al pad di OLDO
/// (compaiono Zaino e AssumiNonno), poi fino a PadNuovi (Scarpe e Insegna). Kaia ferma in 8 direzioni (N, NE, E, SE, S, SO, O, NO)
/// all'80% e al 95% del raggio del disco fa caricare il pad; ferma al 120% no. Nessun pad può essere saltato.</summary>
public class PadTriggerCerchioTest
{
    static readonly string[] Dir = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
    static readonly float[] Dentro = { 0.8f, 0.95f }; const float Fuori = 1.2f;

    [UnityTest, Timeout(300000)]
    public IEnumerator OgniPad_SiCaricaDentroIlCerchio_NonFuori()
    {
        // Partita nuova vera: in Editor il salvataggio è un file (persistentDataPath), non PlayerPrefs. Un save_v1.json rimasto
        // con NONNO già assunto rendeva il pad di OLDO non disponibile (costo infinito, collider spento) e il vecchio test lo saltava.
        PlayerPrefs.DeleteAll(); var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        UltimoMiglio.UI.HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = false; TutorialManager.TestBloccaPassi = true; Time.timeScale = 1f;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto"); Assert.IsNotNull(TutorialManager.I, "TutorialManager mancante");
            yield return null;
            var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina; var cc = nina.GetComponent<CharacterController>();
            Assert.Less(eco.S.NonnoLevel, 0, "partita non nuova: NONNO già assunto");
            var pads = Object.FindObjectsByType<UpgradePad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsTrue(pads.Any(p => p.Kind == UpgradeKind.NonnoHire), "pad AssumiNonno (OLDO) non in scena");
            var provati = new System.Collections.Generic.List<string>();

            // fino al passo "assumi NONNO" (#243): Zaino e OLDO visibili
            yield return Porta(TutorialManager.Step.HireNonno);
            foreach (var pad in pads.Where(p => p.Kind == UpgradeKind.Zaino || p.Kind == UpgradeKind.NonnoHire)) { yield return Prova(pad, eco, nina, cc); provati.Add(pad.name); }
            // fino a PadNuovi: Scarpe e Insegna
            yield return Porta(TutorialManager.Step.PadNuovi);
            foreach (var pad in pads.Where(p => p.Kind == UpgradeKind.Scarpe || p.Kind == UpgradeKind.Insegna)) { yield return Prova(pad, eco, nina, cc); provati.Add(pad.name); }

            Debug.Log("[#171] provati: " + string.Join(", ", provati));
            Assert.AreEqual(pads.Length, provati.Count, "non tutti i pad sono stati provati");
        }
        finally { TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; Time.timeScale = 1f; }
    }

    static IEnumerator Porta(TutorialManager.Step fino)
    {
        for (var s = TutorialManager.I.Current + 1; s <= fino; s++) { TutorialManager.I.TestEntraNelPasso(s); yield return null; }
        Assert.AreEqual(fino, TutorialManager.I.Current);
        float t = Time.time; while (Time.time - t < 0.5f) yield return null; // comparsa animata (scala 0,6→1) finita
    }

    static IEnumerator Prova(UpgradePad pad, EconomyCore eco, NinaController nina, CharacterController cc)
    {
        Assert.IsTrue(pad.gameObject.activeInHierarchy, $"{pad.name}: pad non visibile al passo {TutorialManager.I.Current}");
        Assert.IsFalse(double.IsInfinity(eco.Cost(pad.Kind)), $"{pad.name}: pad non acquistabile (costo infinito)");
        var ip = pad.GetComponent<InteractionPoint>(); Assert.IsNotNull(ip, $"{pad.name}: InteractionPoint mancante"); Assert.IsNotNull(ip.RingVisual, $"{pad.name}: anello mancante");
        Vector3 c = ip.RingVisual.position; float r = 0.5f * Mathf.Min(ip.RingVisual.lossyScale.x, ip.RingVisual.lossyScale.z); // disco visibile (mesh Ø 1 m)
        Assert.AreEqual(ip.Radius * InteractionPoint.VisualScale, r, 0.01f, $"{pad.name}: anello non a scala");
        Debug.Log($"[#171] {pad.name} disco=({c.x:0.00},{c.z:0.00}) r={r:0.00} Kaia r={cc.radius:0.00}");
        Vector3 lontano = new Vector3(0f, nina.transform.position.y, -6f);
        var fr = Dentro.Concat(new[] { Fuori }).ToArray();
        foreach (var f in fr)
            for (int k = 0; k < 8; k++)
            {
                bool dentro = f < 1f;
                float a = k * 45f * Mathf.Deg2Rad; // #171 (Producer): verso la camera il pad accetta fino a TolleranzaCamera in più; il caso "fuori" è al 120% del raggio d'attivazione in quella direzione
                var off = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * ((dentro ? r : pad.RaggioVerso(Mathf.Sin(a), Mathf.Cos(a))) * f); // N = +z
                yield return Metti(nina, lontano); yield return Attendi(0.3f);
                eco.S.Wallet = 1e6; eco.S.PadPaid[(int)pad.Kind] = 0;
                double prima = eco.S.Wallet;
                yield return Metti(nina, new Vector3(c.x + off.x, nina.transform.position.y, c.z + off.z)); yield return Attendi(1.2f);
                bool carica = eco.S.Wallet < prima;
                string chi = $"{pad.name} {Dir[k]} {f * 100:0}% (Kaia {nina.transform.position:F2})";
                if (dentro) Assert.IsTrue(carica && pad.Occupied, $"{chi}: il pad non si carica dentro il disco");
                else Assert.IsFalse(carica || pad.Occupied, $"{chi}: il pad si carica fuori dal disco");
            }
        // #171 (Producer, review Tech Lead): caso esplicito sul lato della camera reale (proiettata sul piano, rotazione #205 compresa):
        // carica entro raggio + TolleranzaCamera (95%), non carica oltre (raggio + TolleranzaCamera + 0,1).
        var cam = Camera.main; Assert.IsNotNull(cam, "Camera.main mancante");
        var fw = cam.transform.forward; fw.y = 0; if (fw.sqrMagnitude < 1e-4f) { fw = cam.transform.up; fw.y = 0; }
        var vc = -fw.normalized;
        Assert.AreEqual(r, pad.RaggioVerso(-vc.x, -vc.z), 1e-4f, $"{pad.name}: lato opposto alla camera, il raggio deve restare quello del disco");
        Assert.AreEqual(r, pad.RaggioVerso(vc.z, -vc.x), 1e-4f, $"{pad.name}: di lato rispetto alla camera, il raggio deve restare quello del disco");
        foreach (var caso in new[] { (d: r + pad.TolleranzaCamera * 0.95f, dentro: true), (d: r + pad.TolleranzaCamera + 0.1f, dentro: false) })
        {
            yield return Metti(nina, lontano); yield return Attendi(0.3f);
            eco.S.Wallet = 1e6; eco.S.PadPaid[(int)pad.Kind] = 0; double prima = eco.S.Wallet;
            yield return Metti(nina, new Vector3(c.x + vc.x * caso.d, nina.transform.position.y, c.z + vc.z * caso.d)); yield return Attendi(1.2f);
            bool carica = eco.S.Wallet < prima;
            string chi = $"{pad.name} lato camera {caso.d:0.00} m (Kaia {nina.transform.position:F2})";
            if (caso.dentro) Assert.IsTrue(carica && pad.Occupied, $"{chi}: il pad non si carica entro raggio + tolleranza verso la camera");
            else Assert.IsFalse(carica || pad.Occupied, $"{chi}: il pad si carica oltre raggio + tolleranza verso la camera");
        }
        yield return Metti(nina, lontano); yield return Attendi(0.3f);
        eco.S.PadPaid[(int)pad.Kind] = 0;
    }

    static IEnumerator Metti(NinaController n, Vector3 p)
    {
        // CharacterController acceso: il collider si sposta con il transform come camminando
        n.ScriptedInput = Vector3.zero; n.transform.position = p; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
    }
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
