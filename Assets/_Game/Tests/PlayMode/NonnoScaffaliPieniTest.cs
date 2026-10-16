using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#221 (scena reale Ch1_Deposito): con gli scaffali pieni NONNO non deve consumare pacchi degli Arrivi
/// scaricandoli su uno scaffale pieno (giro a vuoto, "restock count=12"); appena si libera posto, rifornisce.</summary>
public class NonnoScaffaliPieniTest
{
    [UnityTest, Timeout(180000)]
    public IEnumerator ScaffaliPieni_NienteGiroAVuoto_PoiRifornisce()
    {
        AnimFacingTest.ResetGameKeys(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestAccelera = true;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && !GameRoot.Ready; i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var eco = GameRoot.I.Eco; eco.S.Wallet = 100;
            bool hired = eco.S.NonnoLevel >= 0; for (int i = 0; i < 500 && !hired; i++) hired = eco.PadTick(UpgradeKind.NonnoHire, 0.2);
            Assert.IsTrue(hired, "NONNO non assunto");
            var bot = Object.FindAnyObjectByType<NonnoRobot>(); Assert.IsNotNull(bot);
            var arr = bot.Arrivals; arr.RefillPerSec = 0f; arr.Stock = 30;
            foreach (var s in bot.Shelves) s.Add(s.Capacity);

            // 1) scaffali pieni: in 15 s nessun pacco deve sparire (Arrivi + carico di NONNO costanti)
            int totale = arr.Stock + bot.CarryCount; float t = 0;
            while (t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(totale, arr.Stock + bot.CarryCount, $"pacchi distrutti con gli scaffali pieni: Arrivi {arr.Stock}, carico {bot.CarryCount}, stato {bot.State}");

            // 2) si libera posto: NONNO carica e rifornisce lo scaffale
            var sh = bot.Shelves[0]; for (int i = 0; i < 3; i++) sh.Take(); int prima = sh.Count;
            t = 0; while (t < 30f && sh.Count <= prima) { t += Time.deltaTime; yield return null; }
            Assert.Greater(sh.Count, prima, $"NONNO non rifornisce lo scaffale liberato (stato {bot.State}, carico {bot.CarryCount})");
            foreach (var s in bot.Shelves) Assert.LessOrEqual(s.Count, s.Capacity);
        }
        finally { TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; }
    }
}
