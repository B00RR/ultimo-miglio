using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>Real gameplay path (not wallet cheat): Arrivi → shelf → Bancone serves → LN → hire NONNO → working.</summary>
public class TutorialToNonnoTest
{
    [UnityTest, Timeout(300000)]
    public IEnumerator Path_ArriviShelfBancone_EarnsLn_HiresNonno_Works()
    {
        PlayerPrefs.DeleteAll();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);

        var navErr = new System.Collections.Generic.List<string>();
        Application.LogCallback onLog = (c, s, t) =>
        {
            if (t == LogType.Error || t == LogType.Exception)
                if (c != null && (c.Contains("NavMesh") || c.Contains("Failed to create agent")))
                    navErr.Add(c);
        };
        Application.logMessageReceived += onLog;

        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            Time.timeScale = 3f;
            while (!GameRoot.Ready || !GameRoot.I.Nina) yield return null;
            Assert.IsEmpty(navErr, "NavMesh on load");

            var n = GameRoot.I.Nina;
            var eco = GameRoot.I.Eco;
            var arr = Object.FindAnyObjectByType<ArrivalsZone>();
            var shelves = Object.FindObjectsByType<Shelf>(FindObjectsSortMode.None);
            var ban = Object.FindAnyObjectByType<Bancone>();
            var padN = Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)
                .First(p => p.Kind == UpgradeKind.NonnoHire);
            Assert.IsNotNull(arr); Assert.IsNotNull(ban); Assert.IsNotNull(padN);

            // Objective text must be available from TutorialManager (HUD row mirrors it)
            float paint = 0;
            while ((TutorialManager.I == null || string.IsNullOrEmpty(TutorialManager.I.CurrentObjectiveText)) && paint < 5f)
            { paint += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(TutorialManager.I, "TutorialManager missing");
            Assert.IsFalse(string.IsNullOrEmpty(TutorialManager.I.CurrentObjectiveText), "objective text empty");

            int servesNeeded = 3;
            float t0 = Time.time;
            while (eco.S.CustomersServed < servesNeeded && Time.time - t0 < 180f)
            {
                // Pick at Arrivi
                yield return Go(n, arr.transform.position);
                float a = Time.time;
                while (n.Stack.Count < Mathf.Min(2, n.Stack.Capacity) && Time.time - a < 8f) yield return null;

                // Deposit at least one on a shelf (tutorial Scaffali) then go Bancone (auto-pull if empty)
                if (shelves.Length > 0)
                {
                    yield return Go(n, shelves[0].transform.position);
                    yield return Wait(n, 1.2f);
                }

                yield return Go(n, ban.transform.position);
                int before = eco.S.CustomersServed;
                float b = Time.time;
                while (eco.S.CustomersServed <= before && Time.time - b < 20f) yield return null;
                Assert.IsEmpty(navErr);
            }

            Assert.GreaterOrEqual(eco.S.CustomersServed, servesNeeded, "could not serve customers");
            Assert.GreaterOrEqual(eco.S.Wallet, 10.0 - 0.01, $"wallet {eco.S.Wallet} after {eco.S.CustomersServed} serves — need ≥10 LN");

            // Hire NONNO on pad
            yield return Go(n, padN.transform.position);
            float h = Time.time;
            while (eco.S.NonnoLevel < 0 && eco.S.Wallet > 0.01 && Time.time - h < 40f) yield return null;
            Assert.GreaterOrEqual(eco.S.NonnoLevel, 0, "NONNO not hired");

            var bot = Object.FindAnyObjectByType<NonnoRobot>();
            Assert.IsNotNull(bot);
            Assert.IsNull(bot.GetComponent("NavMeshAgent"), "must be Transform-only");
            if (bot.Arrivals) bot.Arrivals.Stock = 20;
            float w = 0; bool working = false;
            while (w < 10f)
            {
                w += Time.deltaTime; yield return null;
                Assert.IsEmpty(navErr);
                if (bot.State != NonnoRobot.WorkState.Idle || bot.CarryCount > 0) { working = true; break; }
            }
            Assert.IsTrue(working, $"NONNO Idle after hire state={bot.State}");
        }
        finally
        {
            Application.logMessageReceived -= onLog;
            Time.timeScale = 1f;
        }
    }

    static IEnumerator Wait(NinaController n, float s)
    {
        n.ScriptedInput = Vector3.zero;
        yield return new WaitForSeconds(s);
    }

    static IEnumerator Go(NinaController n, Vector3 target)
    {
        target.y = n.transform.position.y;
        float start = Time.time, last = Time.time, best = float.MaxValue;
        while (true)
        {
            var d = target - n.transform.position; d.y = 0;
            if (d.magnitude < 0.3f) break;
            if (d.magnitude < best - 0.05f) { best = d.magnitude; last = Time.time; }
            if (Time.time - last > 3f)
            {
                var cc = n.GetComponent<CharacterController>();
                cc.enabled = false; n.transform.position = target; cc.enabled = true;
                break;
            }
            n.ScriptedInput = d.normalized * 1.001f;
            yield return null;
        }
        n.ScriptedInput = Vector3.zero;
    }
}
