using System.Collections;
using System.Linq;
using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Scripted chapter-1 loop check: drives Nina via ScriptedInput (real movement + triggers).
    /// Arrivals -> shelf -> pickup -> Bancone (serve) -> repeat until Zaino affordable -> Zaino pad. Logs "[LoopTest]" lines; final PASS/FAIL.
    /// Active in WebGL when URL contains "autotest", or added by the PlayMode test.</summary>
    public sealed class LoopAutoTest : MonoBehaviour
    {
        public static string Result; public static readonly System.Collections.Generic.List<int> TutSteps = new(); public float TimeoutSec = 900f; public int Teleports;
        NinaController N; EconomyCore E;

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool auto = Application.absoluteURL.Contains("autotest") || System.Array.IndexOf(args, "-autotest") >= 0;
            if (!auto) return;
            var go = new GameObject("LoopAutoTest"); go.AddComponent<LoopAutoTest>();
            int ci = System.Array.IndexOf(args, "-captureDir"); if (ci >= 0 && ci + 1 < args.Length) go.AddComponent<FrameCapture>().Dir = args[ci + 1];
            AudioManager.Unlocked = true;
        }
#endif
        void Log(string m) => Debug.Log($"[LoopTest] t={Time.time:0.0} {m} | stack={N?.Stack.Count} wallet={E?.S.Wallet:0.##} served={E?.S.CustomersServed} zaino={E?.S.Zaino} padPaid={E?.S.PadPaid[(int)UpgradeKind.Zaino]:0.##} cost={E?.Cost(UpgradeKind.Zaino):0.##}");

        IEnumerator Start()
        {
            Result = null;
            while (!GameRoot.Ready || !GameRoot.I.Nina) yield return null;
            N = GameRoot.I.Nina; E = GameRoot.I.Eco;
            if (E.S.Zaino > 0) { Log("save already has Zaino>0: resetting test target to next level"); }
            int zainoStart = E.S.Zaino;
            TutSteps.Clear(); if (TutorialManager.I) TutSteps.Add((int)TutorialManager.I.Current); TutorialManager.StepChanged += s => { TutSteps.Add((int)s); Log("TUTORIAL -> " + s); };
            Log("START");
            var arr = FindAnyObjectByType<ArrivalsZone>(); var shelves = FindObjectsByType<Shelf>(FindObjectsSortMode.None).OrderBy(s => s.Color).ToArray();
            var picks = FindObjectsByType<ShelfPickup>(FindObjectsSortMode.None); var ban = FindAnyObjectByType<Bancone>();
            var pads = FindObjectsByType<UpgradePad>(FindObjectsSortMode.None);
            var padZ = pads.First(p => p.Kind == UpgradeKind.Zaino); var padN = pads.FirstOrDefault(p => p.Kind == UpgradeKind.NonnoHire);
            float t0 = Time.time; int cycle = 0;
            bool Goal() => E.S.Zaino > zainoStart && (padN == null || E.S.NonnoLevel >= 0);
            while (!Goal() && Time.time - t0 < TimeoutSec)
            {
                cycle++;
                var kind = E.S.Zaino == zainoStart ? UpgradeKind.Zaino : UpgradeKind.NonnoHire; var pad = kind == UpgradeKind.Zaino ? padZ : padN;
                if (pad.gameObject.activeInHierarchy && (E.S.Wallet + E.S.PadPaid[(int)kind] >= E.Cost(kind) || (cycle > 1 && E.S.Wallet > 0))) // #101: pad nascosto dal tutorial graduale = non ancora comprabile
                {
                    yield return Go(pad.transform.position, kind + " pad");
                    float w = Time.time; while (!Goal() && E.S.Wallet > 0.001 && Time.time - w < 20) { if (kind == UpgradeKind.Zaino && E.S.Zaino > zainoStart) break; yield return null; }
                    Log("after pad " + kind); if (Goal()) break; continue;
                }
                yield return Go(arr.transform.position, "Arrivals");
                float a = Time.time; while (!N.Stack.Full && Time.time - a < 8) yield return null;
                Log("picked at Arrivals");
                foreach (var s in shelves) { yield return Go(s.transform.position, "Shelf " + s.Color); yield return Wait(1.5f); }
                Log($"deposited, shelves=[{string.Join(",", shelves.Select(s => s.Count))}]");
                var pk = picks.OrderBy(p => (p.transform.position - N.transform.position).sqrMagnitude).First();
                yield return Go(pk.transform.position, "Pickup " + pk.Shelf.Color); yield return Wait(2f);
                Log("picked from shelf");
                yield return Go(ban.transform.position, "Bancone");
                int served = E.S.CustomersServed; float b = Time.time;
                while (N.Stack.Count > 0 && Time.time - b < 25) yield return null;
                Log($"bancone done, served +{E.S.CustomersServed - served}");
                double w0 = E.S.Wallet; yield return Go(ban.transform.position + Vector3.right * 0.8f, "Coins"); yield return Wait(2.5f);
                Log($"coins collected, wallet +{E.S.Wallet - w0:0.##}");
            }
            // #101: passi 11-15 dopo NONNO (pad nuovi, Cassa, debito, muro, gioco libero)
            if (TutorialManager.I)
            {
                float te = Time.time;
                while (TutorialManager.I.Current != TutorialManager.Step.Chiuso && Time.time - te < 120)
                {
                    var st = TutorialManager.I.Current;
                    if (st == TutorialManager.Step.PadNuovi)
                    {
                        var np = pads.Where(p => (p.Kind == UpgradeKind.Scarpe || p.Kind == UpgradeKind.Insegna) && p.gameObject.activeInHierarchy && !double.IsInfinity(E.Cost(p.Kind)))
                                     .OrderBy(p => E.Cost(p.Kind)).FirstOrDefault();
                        if (np) { if (E.S.Wallet < E.Cost(np.Kind)) { E.S.Wallet = E.Cost(np.Kind); Log("test: LN per " + np.Kind); } yield return Go(np.transform.position, np.Kind + " pad"); yield return Wait(8f); continue; }
                    }
                    if (st == TutorialManager.Step.CassaPass) { var cz = FindAnyObjectByType<CassaZone>(); if (cz) { yield return Go(cz.transform.position, "Cassa"); yield return Wait(1.5f); continue; } }
                    yield return null;
                }
            }
            Result = Goal() ? "PASS" : "FAIL";
            Log("tutorial steps: " + string.Join(",", TutSteps));
            Log($"{Result} cycles={cycle} teleports={Teleports}");
            N.ScriptedInput = Vector3.zero;
        }

        IEnumerator Wait(float s) { N.ScriptedInput = Vector3.zero; yield return new WaitForSeconds(s); }

        IEnumerator Go(Vector3 target, string name)
        {
            target.y = N.transform.position.y; float start = Time.time, lastProg = Time.time; float best = float.MaxValue;
            while (true)
            {
                var d = target - N.transform.position; d.y = 0;
                if (d.magnitude < 0.25f) break;
                if (d.magnitude < best - 0.05f) { best = d.magnitude; lastProg = Time.time; }
                if (Time.time - lastProg > 3f)
                { // stuck on geometry: teleport (counted in the report)
                    var cc = N.GetComponent<CharacterController>(); cc.enabled = false; N.transform.position = target; cc.enabled = true; Teleports++; Log("TELEPORT to " + name); break;
                }
                N.ScriptedInput = d.normalized * 1.001f;
                yield return null;
            }
            N.ScriptedInput = Vector3.zero; Log("at " + name);
        }
    }
}
