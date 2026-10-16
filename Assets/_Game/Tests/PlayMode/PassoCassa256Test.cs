using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>#256: scena reale Ch1_Deposito, partita nuova, tutorial graduale acceso (senza Salta). Lo stato viene portato fino al passo 11 padNuovi,
/// poi i passi 11-13 si giocano di fila come un giocatore: Kaia cammina sul pad Scarpe (padNuovi), sulla Cassa (cassaPass) e alla Cassaforte (debtCard).
/// Ogni passo deve chiudersi entro pochi secondi; al passo cassaPass il bersaglio della guida (e della mano del suggerimento) è la Cassa.</summary>
public class PassoCassa256Test
{
    static readonly FieldInfo FSeen = typeof(TutorialManager).GetField("_cassaSeen", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo FCassa = typeof(TutorialManager).GetField("_cassa", BindingFlags.NonPublic | BindingFlags.Instance);

    [UnityTest, Timeout(240000)]
    public IEnumerator Passi11_13_GiocatiDiFila_SiChiudono()
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
            for (var s = tut.Current + 1; s <= TutorialManager.Step.PadNuovi; s++) { tut.TestEntraNelPasso(s); yield return null; }
            eco.S.Wallet = 500; eco.S.Zaino = Mathf.Max(eco.S.Zaino, 1);
            yield return Attendi(0.6f);
            TutorialManager.TestBloccaPassi = false; // da qui i passi avanzano solo con i gate veri

            // passo 11 padNuovi: Kaia sul pad Scarpe
            var scarpe = System.Array.Find(Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None), p => p.Kind == UltimoMiglio.Core.UpgradeKind.Scarpe);
            Assert.IsNotNull(scarpe, "pad Scarpe non visibile al passo padNuovi");
            yield return Cammina(nina, scarpe.transform.position, TutorialManager.Step.CassaPass, 25f, "Scarpe");
            Assert.AreEqual(TutorialManager.Step.CassaPass, tut.Current, "padNuovi non chiuso con Kaia sul pad Scarpe\n" + Stato(tut));
            yield return Esci(nina, scarpe.transform.position);

            // passo 12 cassaPass: bersaglio = Cassa, poi Kaia sulla Cassa
            yield return Attendi(0.5f);
            var cassa = Object.FindAnyObjectByType<CassaZone>(); Assert.IsNotNull(cassa, "CassaZone non in scena");
            var t12 = tut.CurrentTarget;
            Debug.Log($"[#256] cassaPass target={(t12 ? t12.name : "null")} pos={(t12 ? t12.position : Vector3.zero)} cassa={cassa.name} pos={cassa.transform.position} " + Stato(tut));
            Assert.AreSame(cassa.transform, t12, "al passo cassaPass il bersaglio della guida/mano non è la Cassa");
            yield return Cammina(nina, cassa.transform.position, TutorialManager.Step.DebtCard, 20f, "Cassa");
            Assert.AreEqual(TutorialManager.Step.DebtCard, tut.Current, "cassaPass non chiuso con Kaia sulla Cassa\n" + Stato(tut));

            // passo 13 debtCard: Kaia al bersaglio del passo (Cassaforte)
            var t13 = tut.CurrentTarget; Assert.IsNotNull(t13, "debtCard senza bersaglio");
            yield return Cammina(nina, t13.position, TutorialManager.Step.WallPad, 30f, "Cassaforte " + t13.name);
            Assert.AreEqual(TutorialManager.Step.WallPad, tut.Current, "debtCard non chiuso alla Cassaforte\n" + Stato(tut));
        }
        finally { TutorialManager.TestBloccaPassi = false; TutorialManager.TestAccelera = false; nina0(); Time.timeScale = 1f; }
    }

    static void nina0() { if (GameRoot.I && GameRoot.I.Nina) GameRoot.I.Nina.ScriptedInput = Vector3.zero; }

    static string Stato(TutorialManager tut)
    {
        var c = FCassa?.GetValue(tut) as CassaZone; var a = ProximityZone.Active; var n = GameRoot.I.Nina.transform.position;
        return $"step={tut.Current} kaia=({n.x:0.00},{n.z:0.00}) cassaSeen={FSeen?.GetValue(tut)} cacheCassa={(c ? c.name + " occ=" + c.Occupied + " en=" + c.isActiveAndEnabled : "null")} active={(a ? a.name : "null")} target={(tut.CurrentTarget ? tut.CurrentTarget.name : "null")} tmEn={tut.isActiveAndEnabled}";
    }

    /// <summary>Kaia cammina (ScriptedInput) verso p e resta lì finché il tutorial arriva a <paramref name="fino"/> o scade il tempo; stato loggato ogni 0,5 s.</summary>
    static IEnumerator Cammina(NinaController n, Vector3 p, TutorialManager.Step fino, float maxSec, string cosa)
    {
        float t0 = Time.time, log = 0;
        while (Time.time - t0 < maxSec && TutorialManager.I.Current < fino)
        {
            var d = p - n.transform.position; d.y = 0;
            n.ScriptedInput = d.magnitude > 0.15f ? d.normalized * Mathf.Clamp01(d.magnitude * 2f) : Vector3.zero;
            if ((log -= Time.deltaTime) <= 0) { log = 0.5f; Debug.Log($"[#256] {cosa} t={Time.time - t0:0.0} dist={d.magnitude:0.00} " + Stato(TutorialManager.I)); }
            yield return null;
        }
        n.ScriptedInput = Vector3.zero;
        Debug.Log($"[#256] {cosa} fine t={Time.time - t0:0.0} " + Stato(TutorialManager.I));
    }
    static IEnumerator Esci(NinaController n, Vector3 da)
    {
        var dir = n.transform.position - da; dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
        float t0 = Time.time; while (Time.time - t0 < 0.8f) { n.ScriptedInput = dir.normalized; yield return null; }
        n.ScriptedInput = Vector3.zero;
    }
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
