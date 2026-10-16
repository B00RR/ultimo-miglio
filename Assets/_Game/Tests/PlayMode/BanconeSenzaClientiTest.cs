using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#188: senza clienti in attesa al Bancone, Nina che resta al Bancone non vende: i pacchi restano nella pila e le LN non cambiano.</summary>
public class BanconeSenzaClientiTest
{
    [UnityTest, Timeout(120000)]
    public IEnumerator SenzaClienti_NienteVendite_PacchiRestano()
    {
        AnimFacingTest.ResetGameKeys(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestAccelera = true;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var q = Object.FindAnyObjectByType<CustomerQueue>(); var ban = Object.FindAnyObjectByType<Bancone>();
            Assert.IsNotNull(q); Assert.IsNotNull(ban);
            q.enabled = false; // niente nuovi clienti
            while (q.Head != null) q.Dequeue(q.Head, true);
            var n = GameRoot.I.Nina; var eco = GameRoot.I.Eco;
            while (!n.Stack.Full && n.Stack.Count < 3) n.Stack.TryPush(ParcelColor.A_Terracotta);
            int pacchi = n.Stack.Count; Assert.Greater(pacchi, 0);

            var p = ban.GetComponent<Collider>().bounds.center; p.y = n.transform.position.y;
            // #82: il muro del set chiude il retro del Bancone: Nina entra dal lato est (come Nina_ReachesZonaBancone_NoPocketBehind)
            var zc = ban.transform.position;
            var cc = n.GetComponent<CharacterController>(); if (cc) cc.enabled = false; n.transform.position = zc + new Vector3(2.2f, 0.05f, -2.4f); if (cc) cc.enabled = true; yield return null;
            var wps = new System.Collections.Generic.List<Vector3> { zc + new Vector3(2.2f, 0, -0.1f), p }; int k = 0;
            float tw = Time.time; // entra camminando (OnTriggerEnter), come nei test esistenti
            while (!ban.Occupied && Time.time - tw < 20f)
            {
                var d = wps[k] - n.transform.position; d.y = 0;
                if (d.sqrMagnitude < 0.04f && k < wps.Count - 1) { k++; continue; }
                if (k == wps.Count - 1) { d = p - n.transform.position; d.y = 0; }
                n.ScriptedInput = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward; yield return null;
            }
            n.ScriptedInput = Vector3.zero;
            Assert.IsTrue(ban.Occupied, "Nina non è entrata nel Bancone");
            int servedPrima = eco.S.CustomersServed; double ln = eco.S.Wallet;
            float t = Time.time; while (Time.time - t < 5f) { n.ScriptedInput = Vector3.zero; yield return null; }

            Assert.AreEqual(pacchi, n.Stack.Count, "senza clienti i pacchi sono spariti dalla pila");
            Assert.AreEqual(servedPrima, eco.S.CustomersServed, "vendita senza cliente");
            Assert.AreEqual(ln, eco.S.Wallet, 1e-9, "monete date senza cliente");

            // cliente presente ma ancora in cammino (fermato lontano dal Bancone): non è "in attesa" → ancora niente vendita
            q.enabled = true; float ts = Time.time;
            while (q.Head == null && Time.time - ts < 30f) { n.ScriptedInput = Vector3.zero; yield return null; }
            var c = q.Head; Assert.IsNotNull(c, "nessun cliente generato");
            float vel = c.Speed; c.Speed = 0f; q.enabled = false;
            Assert.IsFalse(ban.InAttesa(c), "cliente sulla porta considerato in attesa");
            t = Time.time; while (Time.time - t < 3f) { n.ScriptedInput = Vector3.zero; yield return null; }
            Assert.AreEqual(pacchi, n.Stack.Count, "venduto a un cliente non ancora al Bancone");
            Assert.AreEqual(servedPrima, eco.S.CustomersServed, "vendita a un cliente non ancora al Bancone");

            // controllo positivo: lo stesso cliente arriva al Bancone → si vende un pacco
            c.Speed = vel * 3f; t = Time.time;
            while (eco.S.CustomersServed == servedPrima && Time.time - t < 20f) { n.ScriptedInput = Vector3.zero; yield return null; }
            Assert.AreEqual(servedPrima + 1, eco.S.CustomersServed, "il cliente al Bancone non è stato servito");
            Assert.AreEqual(pacchi - 1, n.Stack.Count, "servire deve togliere un solo pacco");
        }
        finally { TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f; }
    }
}
