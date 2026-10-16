using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#188 (scena reale Ch1_Deposito): coda vuota a metà partita con NONNO assunto. Senza pacchi da nessuna parte
/// (pila di Nina e scaffali vuoti, Arrivi vuoti) al ritorno dei clienti non deve esserci nessuna vendita né moneta.</summary>
public class NonnoVenditaSenzaPaccoTest
{
    [UnityTest, Timeout(240000)]
    public IEnumerator CodaVuotaPoiClienti_SenzaPacchi_NienteVendite()
    {
        AnimFacingTest.ResetGameKeys(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestAccelera = true;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var eco = GameRoot.I.Eco; eco.S.Wallet = 100;
            bool hired = eco.S.NonnoLevel >= 0; for (int i = 0; i < 500 && !hired; i++) hired = eco.PadTick(UpgradeKind.NonnoHire, 0.2);
            Assert.IsTrue(hired, "NONNO non assunto");
            var bot = Object.FindAnyObjectByType<NonnoRobot>(); var q = Object.FindAnyObjectByType<CustomerQueue>(); var ban = Object.FindAnyObjectByType<Bancone>();
            var n = GameRoot.I.Nina;
            bot.Arrivals.RefillPerSec = 0f; bot.Arrivals.Stock = 0;
            foreach (var s in bot.Shelves) while (s.Count > 0) s.Take();
            while (n.Stack.Count > 0) n.Stack.TryPop(out _);
            Assert.IsFalse(ban.Occupied, "Nina non deve stare al Bancone");

            // coda vuota a metà partita per ~4 minuti di gioco
            q.enabled = false; while (q.Head != null) q.Dequeue(q.Head, true);
            Time.timeScale = 8f; float t = Time.time; while (Time.time - t < 240f) yield return null;

            int served = eco.S.CustomersServed; double ln = eco.S.Wallet;
            q.enabled = true; t = Time.time;
            while (Time.time - t < 60f) yield return null;
            Time.timeScale = 1f;
            int pacchi = n.Stack.Count; foreach (var s in bot.Shelves) pacchi += s.Count;
            Assert.AreEqual(0, pacchi, "pacchi comparsi dal nulla");
            Assert.AreEqual(served, eco.S.CustomersServed, "vendita senza nessun pacco (NONNO self-service)");
            Assert.AreEqual(ln, eco.S.Wallet, 1e-9, "monete senza pacco consegnato");
        }
        finally { TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f; }
    }
}
