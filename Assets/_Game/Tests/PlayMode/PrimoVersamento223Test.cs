using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>#223: scena reale Ch1_Deposito, partita nuova, tutorial graduale (senza Salta). Prima della Cassaforte il 100% delle LN va nel portafoglio;
/// al passo debtCard Kaia (chr_nina_vela) arriva entro 1,6 m da prop_safe: versa una sola volta il 60% del portafoglio, poi parte la ripartizione 40/60.</summary>
public class PrimoVersamento223Test
{
    const double Eps = 1e-6;

    [UnityTest, Timeout(180000)]
    public IEnumerator PrimoVersamento_AllaCassaforte_60PerCento_PoiSplit40_60()
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        UltimoMiglio.UI.HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = false; TutorialManager.TestBloccaPassi = true; TutorialManager.TestAccelera = true; Time.timeScale = 1f;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            yield return null;
            var tut = TutorialManager.I; var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina;
            for (var s = tut.Current + 1; s <= TutorialManager.Step.CassaPass; s++) { tut.TestEntraNelPasso(s); yield return null; }

            // prima della Cassaforte: niente trattenuta, il 100% va nel portafoglio
            Assert.IsTrue(eco.S.DebtSplitLocked, "split già attivo prima del passo debtCard");
            eco.S.Wallet = 500; double debito0 = eco.S.DebtPaid;
            eco.CollectActive(100);
            Assert.AreEqual(600, eco.S.Wallet, Eps, "prima del versamento il 100% deve andare nel portafoglio");
            Assert.AreEqual(debito0, eco.S.DebtPaid, Eps, "prima del versamento nessuna quota al debito");
            yield return Attendi(0.5f);
            TutorialManager.TestBloccaPassi = false; // da qui i passi avanzano solo con i gate veri

            var cassa = Object.FindAnyObjectByType<CassaZone>(); Assert.IsNotNull(cassa, "CassaZone non in scena");
            yield return Cammina(nina, cassa.transform.position, () => tut.Current >= TutorialManager.Step.DebtCard, 20f);
            Assert.AreEqual(TutorialManager.Step.DebtCard, tut.Current, "cassaPass non chiuso con Kaia sulla Cassa");
            Assert.IsTrue(eco.S.DebtSplitLocked, "all'ingresso di debtCard lo split deve restare bloccato fino alla Cassaforte");

            var safe = GameObject.Find(TutorialManager.SafeObjectName); // una volta sola
            Assert.IsNotNull(safe, "prop_safe non in scena");
            Assert.AreSame(safe.transform, tut.CurrentTarget, "al passo debtCard il bersaglio non è la Cassaforte");
            double portafoglio = eco.S.Wallet, debito = eco.S.DebtPaid;

            // Kaia cammina verso la Cassaforte: nessun versamento finché è oltre 1,6 m
            yield return Cammina(nina, safe.transform.position, () => !eco.S.DebtSplitLocked, 30f, () =>
            {
                if (!TutorialManager.VicinoAllaCassaforte(nina.transform.position, safe.transform.position))
                    Assert.AreEqual(debito, eco.S.DebtPaid, Eps, "versamento oltre 1,6 m dalla Cassaforte");
            });
            Assert.IsFalse(eco.S.DebtSplitLocked, "nessun versamento con Kaia alla Cassaforte");
            Assert.IsTrue(TutorialManager.VicinoAllaCassaforte(nina.transform.position, safe.transform.position), "versamento fuori dal raggio di 1,6 m");
            // eventuali incassi durante la camminata vanno al 100% nel portafoglio: versato = 60% e resto = 40% del portafoglio al momento del versamento
            double versato = eco.S.DebtPaid - debito;
            Assert.Greater(versato, 0, "nessuna LN versata");
            Assert.GreaterOrEqual(versato + eco.S.Wallet, portafoglio - Eps, "il portafoglio è calato prima del versamento");
            Assert.AreEqual(0.6, versato / (versato + eco.S.Wallet), 1e-6, "il primo versamento deve essere il 60% del portafoglio");

            // una sola volta: restare sulla Cassaforte non versa ancora
            double d1 = eco.S.DebtPaid, w1 = eco.S.Wallet;
            yield return Attendi(1f);
            Assert.IsFalse(eco.S.DebtSplitLocked, "lo split si è ribloccato");
            d1 = eco.S.DebtPaid; w1 = eco.S.Wallet;
            Assert.AreEqual(0, eco.FirstDebtDeposit(), Eps, "il primo versamento deve avvenire una sola volta");
            Assert.AreEqual(d1, eco.S.DebtPaid, Eps, "secondo versamento non atteso");
            Assert.AreEqual(TutorialManager.Step.WallPad, tut.Current, "debtCard non chiuso dopo il versamento");

            // poi ripartizione 40/60
            eco.CollectActive(100);
            Assert.AreEqual(w1 + 40, eco.S.Wallet, Eps, "dopo il versamento il 40% va nel portafoglio");
            Assert.AreEqual(d1 + 60, eco.S.DebtPaid, Eps, "dopo il versamento il 60% va al debito");
        }
        finally { TutorialManager.TestBloccaPassi = false; TutorialManager.TestAccelera = false; if (GameRoot.I && GameRoot.I.Nina) GameRoot.I.Nina.ScriptedInput = Vector3.zero; Time.timeScale = 1f; }
    }

    static IEnumerator Cammina(NinaController n, Vector3 p, System.Func<bool> fatto, float maxSec, System.Action ogniFrame = null)
    {
        float t0 = Time.time;
        while (Time.time - t0 < maxSec && !fatto())
        {
            ogniFrame?.Invoke();
            var d = p - n.transform.position; d.y = 0;
            n.ScriptedInput = d.magnitude > 0.15f ? d.normalized * Mathf.Clamp01(d.magnitude * 2f) : Vector3.zero;
            yield return null;
        }
        n.ScriptedInput = Vector3.zero;
    }
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
