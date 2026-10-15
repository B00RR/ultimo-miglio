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

            float paint = 0;
            while ((TutorialManager.I == null || string.IsNullOrEmpty(TutorialManager.I.CurrentObjectiveText)) && paint < 5f)
            { paint += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(TutorialManager.I, "TutorialManager missing");
            Assert.IsFalse(string.IsNullOrEmpty(TutorialManager.I.CurrentObjectiveText), "objective text empty");

            int servesNeeded = 3;
            float t0 = Time.time;
            while (eco.S.CustomersServed < servesNeeded && Time.time - t0 < 180f)
            {
                yield return Go(n, arr.transform.position);
                float a = Time.time;
                while (n.Stack.Count < Mathf.Min(2, n.Stack.Capacity) && Time.time - a < 8f) yield return null;

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

            // Hire NONNO: cammina sul pad e resta in trigger (teleport CC non genera OnTriggerEnter)
            yield return EnterPad(n, padN);
            float h = Time.time;
            double wStart = eco.S.Wallet;
            while (eco.S.NonnoLevel < 0 && Time.time - h < 60f)
            {
                if (!padN.Occupied)
                    yield return EnterPad(n, padN);
                else
                    n.ScriptedInput = Vector3.zero;
                yield return null;
            }
            Assert.GreaterOrEqual(eco.S.NonnoLevel, 0,
                $"NONNO not hired wallet={eco.S.Wallet} (was {wStart}) padPaid={eco.S.PadPaid[(int)UpgradeKind.NonnoHire]} occupied={padN.Occupied}");

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

    /// <summary>Entra nel collider del pad con movimento reale (niente solo teleport).</summary>
    static IEnumerator EnterPad(NinaController n, UpgradePad pad)
    {
        var target = pad.transform.position;
        // avvicinati da fuori, poi entra
        var approach = target + Vector3.back * 1.2f;
        yield return Go(n, approach, allowTeleport: true);
        yield return Go(n, target, allowTeleport: false);
        // micro-passi per garantire OnTriggerEnter del CharacterController
        for (int i = 0; i < 12 && !pad.Occupied; i++)
        {
            var d = target - n.transform.position; d.y = 0;
            n.ScriptedInput = (d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward) * 1.001f;
            yield return null;
        }
        n.ScriptedInput = Vector3.zero;
        float dwell = Time.time;
        while (!pad.Occupied && Time.time - dwell < 2f) yield return null;
    }

    static IEnumerator Go(NinaController n, Vector3 target, bool allowTeleport = true)
    {
        target.y = n.transform.position.y;
        float last = Time.time, best = float.MaxValue;
        while (true)
        {
            var d = target - n.transform.position; d.y = 0;
            if (d.magnitude < 0.25f) break;
            if (d.magnitude < best - 0.05f) { best = d.magnitude; last = Time.time; }
            if (allowTeleport && Time.time - last > 3f)
            {
                var cc = n.GetComponent<CharacterController>();
                cc.enabled = false; n.transform.position = target; cc.enabled = true;
                Physics.SyncTransforms();
                // passo fuori+dentro per riattivare i trigger dopo teleport
                n.ScriptedInput = Vector3.forward * 1.001f;
                yield return null;
                n.ScriptedInput = Vector3.back * 1.001f;
                yield return null;
                break;
            }
            if (!allowTeleport && Time.time - last > 8f)
            {
                // ultimo tentativo: teleport vicino e cammina
                var cc = n.GetComponent<CharacterController>();
                var near = target + Vector3.back * 0.8f; near.y = target.y;
                cc.enabled = false; n.transform.position = near; cc.enabled = true;
                Physics.SyncTransforms();
                last = Time.time; best = float.MaxValue; allowTeleport = true;
            }
            n.ScriptedInput = d.normalized * 1.001f;
            yield return null;
        }
        n.ScriptedInput = Vector3.zero;
    }
}
