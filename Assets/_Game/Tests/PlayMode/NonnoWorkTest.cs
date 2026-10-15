using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

public class NonnoWorkTest
{
    [UnityTest] public IEnumerator HireNonno_StartsWorkingWithin10Seconds()
    {
        PlayerPrefs.DeleteAll();
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 30 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot not ready");
        var eco = GameRoot.I.Eco;
        eco.S.Wallet = 100;
        Assert.AreEqual(-1, eco.S.NonnoLevel);
        bool done = false;
        for (int i = 0; i < 500 && !done; i++)
            done = eco.PadTick(UpgradeKind.NonnoHire, i * 0.1);
        Assert.IsTrue(done, "hire pad did not complete");
        Assert.AreEqual(0, eco.S.NonnoLevel, "NONNO should be hired");

        var bot = Object.FindAnyObjectByType<NonnoRobot>();
        Assert.IsNotNull(bot, "NonnoRobot missing");
        // give arrivals stock
        if (bot.Arrivals) bot.Arrivals.Stock = 20;

        float t = 0; bool working = false;
        while (t < 10f)
        {
            t += Time.deltaTime;
            yield return null;
            if (bot.State != NonnoRobot.WorkState.Idle || bot.CarryCount > 0) { working = true; break; }
        }
        Assert.IsTrue(working, $"NONNO still Idle after {t:F1}s hire. state={bot.State} carry={bot.CarryCount}");
    }
}
