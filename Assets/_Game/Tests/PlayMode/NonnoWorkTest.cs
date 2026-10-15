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
            // Prefer B: no NavMeshAgent component at runtime
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
}
