using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#243 (P0): al passo hireNonno con il tutorial GRADUALE acceso il pad OLDO deve incassare.
/// Ripresa come un giocatore che ricarica al passo hireNonno; Nina cammina sul pad (niente teleport). Log: chi tiene ProximityZone.Active, enter/exit/rifiuti.</summary>
public class PadOldoTutorialGraduale243Test
{
    readonly List<string> _trace = new List<string>();
    [TearDown] public void TearDown() { ProximityZone.Trace = null; Time.timeScale = 1f; }

    [UnityTest, Timeout(240000)]
    public IEnumerator HireNonno_GradualeAcceso_PadOldoIncassa()
    {
        PlayerPrefs.DeleteAll();
        TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; TutorialManager.TestBloccaPassi = false;
        PlayerPrefs.SetInt(TutorialManager.PrefKey, (int)TutorialManager.Step.HireNonno); PlayerPrefs.SetInt(TutorialManager.PrefVer, TutorialManager.Versione);
        PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        string last = null;
        ProximityZone.Trace = m => { if (m == last) return; last = m; _trace.Add($"t={Time.time:0.00} {m}"); Debug.Log("[243] " + m); };
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(TutorialManager.Step.HireNonno, TutorialManager.I.Current, "non siamo al passo hireNonno");
        var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina;
        if (eco.S.Zaino < 1) eco.S.Zaino = 1; // come dopo il passo zainoPad
        eco.S.Wallet = 70;
        UpgradePad padN = null; foreach (var p in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) if (p.Kind == UpgradeKind.NonnoHire) padN = p;
        Assert.IsNotNull(padN, "pad OLDO non attivo al passo hireNonno (graduale)");
        Debug.Log($"[243] start nina={nina.transform.position} pad={padN.transform.position} col={padN.GetComponent<Collider>()?.enabled} active={(ProximityZone.Active ? ProximityZone.Active.name : "null")}");
        float t0 = Time.time;
        while (Time.time - t0 < 20f && eco.S.NonnoLevel < 0)
        {
            var d = padN.transform.position - nina.transform.position; d.y = 0;
            nina.ScriptedInput = d.magnitude > 0.1f ? d.normalized : Vector3.zero;
            yield return null;
        }
        nina.ScriptedInput = Vector3.zero;
        string diag = $"wallet={eco.S.Wallet} paid={eco.S.PadPaid[(int)UpgradeKind.NonnoHire]} occupied={padN.Occupied} active={(ProximityZone.Active ? ProximityZone.Active.name : "null")} " +
                      $"nina={nina.transform.position} step={TutorialManager.I.Current}\n" + string.Join("\n", _trace);
        Debug.Log("[243] fine " + diag);
        Assert.GreaterOrEqual(eco.S.NonnoLevel, 0, "pad OLDO non ha incassato al passo hireNonno (graduale): " + diag);
    }
}
