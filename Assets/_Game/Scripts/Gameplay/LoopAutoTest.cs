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
        public static string Result; public float TimeoutSec = 400f; public int Teleports;
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
            Log("START");
            var arr = FindAnyObjectByType<ArrivalsZone>(); var shelves = FindObjectsByType<Shelf>(FindObjectsSortMode.None).OrderBy(s => s.Color).ToArray();
            var picks = FindObjectsByType<ShelfPickup>(FindObjectsSortMode.None); var ban = FindAnyObjectByType<Bancone>();
            var pad = FindObjectsByType<UpgradePad>(FindObjectsSortMode.None).First(p => p.Kind == UpgradeKind.Zaino);
            float t0 = Time.time; int cycle = 0;
            while (E.S.Zaino == zainoStart && Time.time - t0 < TimeoutSec)
            {
                cycle++;
                if (E.S.Wallet + E.S.PadPaid[(int)UpgradeKind.Zaino] >= E.Cost(UpgradeKind.Zaino) || (cycle > 1 && E.S.Wallet > 0))
                {
                    yield return Go(pad.transform.position, "Zaino pad");
                    float w = Time.time; while (E.S.Zaino == zainoStart && E.S.Wallet > 0.001 && Time.time - w < 20) yield return null;
                    Log("after pad"); if (E.S.Zaino != zainoStart) break;
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
            Result = E.S.Zaino > zainoStart ? "PASS" : "FAIL";
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
