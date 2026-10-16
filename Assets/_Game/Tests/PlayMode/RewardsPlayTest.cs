using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#103: freccia nextPadArrow in scena (compare da 480 s sul candidato affordable, sparisce dopo 20 s o all'acquisto).</summary>
public class RewardsPlayTest
{
    [UnityTest, Timeout(120000)]
    public IEnumerator NextPadArrow_ShowsAt480_HidesAfter20s()
    {
        PlayerPrefs.DeleteAll();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        while (!GameRoot.Ready || NextPadArrow.I == null) yield return null;
        var eco = GameRoot.I.Eco; UpgradePad shown = null; int changes = 0;
        System.Action<UpgradePad> h = p => { changes++; shown = p; };
        NextPadArrow.Changed += h;
        try
        {
            eco.S.Scarpe = 1; eco.S.Wallet = 0; eco.S.ActiveSec = 481;
            yield return new WaitForSeconds(0.3f);
            Assert.IsNull(NextPadArrow.I.Target, "freccia senza LN sufficienti");
            eco.S.Wallet = eco.Cost(UpgradeKind.Scarpe) + 1;
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(NextPadArrow.I.Target, "freccia non comparsa a 480 s");
            Assert.AreEqual(UpgradeKind.Scarpe, NextPadArrow.I.Target.Kind);
            Time.timeScale = 10f;
            yield return new WaitForSeconds(21f);
            Assert.IsNull(NextPadArrow.I.Target, "freccia ancora visibile dopo 20 s");
            Assert.IsNull(shown); Assert.AreEqual(2, changes);
        }
        finally { NextPadArrow.Changed -= h; Time.timeScale = 1f; }
    }
}
