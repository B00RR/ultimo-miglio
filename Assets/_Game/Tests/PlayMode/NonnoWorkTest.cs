using System.Collections;
using System.Collections.Generic;
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
        // instabile da solo: un save_v1.json di un run precedente (NONNO già assunto) dava NonnoLevel 0 invece di -1
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        var navMeshErrors = new List<string>();
        Application.LogCallback onLog = (cond, stack, type) =>
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (cond != null && (cond.Contains("NavMesh") || cond.Contains("no valid NavMesh") || cond.Contains("Failed to create agent")))
                navMeshErrors.Add(cond);
        };
        Application.logMessageReceived += onLog;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 30 && !GameRoot.Ready; i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot not ready");
            Assert.IsEmpty(navMeshErrors, "NavMesh agent error on load: " + string.Join(" | ", navMeshErrors));

            var eco = GameRoot.I.Eco;
            eco.S.Wallet = 100;
            Assert.AreEqual(-1, eco.S.NonnoLevel);
            Assert.LessOrEqual(eco.Cost(UpgradeKind.NonnoHire), 10.01, "NONNO hire should be ≤10 LN");
            bool done = false;
            for (int i = 0; i < 500 && !done; i++)
                done = eco.PadTick(UpgradeKind.NonnoHire, i * 0.1);
            Assert.IsTrue(done, "hire pad did not complete");
            Assert.AreEqual(0, eco.S.NonnoLevel, "NONNO should be hired");

            var bot = Object.FindAnyObjectByType<NonnoRobot>();
            Assert.IsNotNull(bot, "NonnoRobot missing");
            var leftover = bot.GetComponent("NavMeshAgent");
            Assert.IsNull(leftover, "NavMeshAgent must not exist on NONNO (Transform-only)");

            if (bot.Arrivals) bot.Arrivals.Stock = 20;

            float t = 0; bool working = false;
            while (t < 10f)
            {
                t += Time.deltaTime;
                yield return null;
                Assert.IsEmpty(navMeshErrors, "NavMesh error while NONNO working: " + string.Join(" | ", navMeshErrors));
                if (bot.State != NonnoRobot.WorkState.Idle || bot.CarryCount > 0) { working = true; break; }
            }
            Assert.IsTrue(working, $"NONNO still Idle after {t:F1}s hire. state={bot.State} carry={bot.CarryCount}");
            Assert.IsEmpty(navMeshErrors, "NavMesh console error after hire");
        }
        finally
        {
            Application.logMessageReceived -= onLog;
        }
    }

    /// <summary>P0 QA v3: after hire, NONNO must leave Arrivi and increase shelf stock within 20 s (Transform-only).</summary>
    [UnityTest, Timeout(120000)]
    public IEnumerator HireNonno_RestocksShelfWithin20Seconds()
    {
        PlayerPrefs.DeleteAll();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);

        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 60 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready);

        var eco = GameRoot.I.Eco;
        eco.S.Wallet = 100;
        bool hired = false;
        for (int i = 0; i < 500 && !hired; i++)
            hired = eco.PadTick(UpgradeKind.NonnoHire, 0.2);
        Assert.IsTrue(hired);
        Assert.AreEqual(0, eco.S.NonnoLevel);

        var bot = Object.FindAnyObjectByType<NonnoRobot>();
        Assert.IsNotNull(bot);
        Assert.IsNull(bot.GetComponent("NavMeshAgent"), "Transform-only");
        Assert.IsNotNull(bot.Arrivals);
        Assert.IsNotNull(bot.Shelves);
        Assert.Greater(bot.Shelves.Length, 0);

        bot.Arrivals.Stock = 30;
        // Zero shelves so any Add from NONNO is visible (Bancone self-serve is slow)
        foreach (var s in bot.Shelves) if (s) { while (s.Count > 0) s.Take(); }
        int shelfBefore = 0;
        foreach (var s in bot.Shelves) if (s) shelfBefore += s.Count;
        Assert.AreEqual(0, shelfBefore, "shelves should start empty for restock assert");

        float t = 0f;
        int shelfAfter = shelfBefore;
        bool leftLoading = false;
        bool delivered = false;
        while (t < 20f)
        {
            t += Time.deltaTime;
            yield return null;
            if (bot.State == NonnoRobot.WorkState.ToShelf || bot.State == NonnoRobot.WorkState.Unloading || bot.State == NonnoRobot.WorkState.Idle && bot.CarryCount == 0 && t > 2f)
                leftLoading = true;
            if (bot.State == NonnoRobot.WorkState.ToShelf || bot.State == NonnoRobot.WorkState.Unloading)
                leftLoading = true;

            int sum = 0;
            foreach (var s in bot.Shelves) if (s) sum += s.Count;
            shelfAfter = sum;
            if (shelfAfter > shelfBefore) { delivered = true; break; }
        }

        Assert.IsTrue(leftLoading || bot.State != NonnoRobot.WorkState.Loading,
            $"NONNO stuck Loading after {t:F1}s carry={bot.CarryCount} stock={bot.Arrivals.Stock} state={bot.State}");
        Assert.IsTrue(delivered || shelfAfter > shelfBefore,
            $"Shelf stock did not increase via NONNO within 20s. before={shelfBefore} after={shelfAfter} state={bot.State} carry={bot.CarryCount} loadStock={bot.Arrivals.Stock}");
    }
}
