using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>Onboarding graduale (#94): stato nascosto/visibile per ogni elemento bloccato (id come in onboarding.json → lockedAtStart).
    /// HUD (VisualElement): classi USS "reveal" / "reveal--hidden" / "reveal--in" (scala 0,6→1 + dissolvenza, 250 ms, Onboarding.uss).
    /// Mondo (GameObject): SetActive + scala 0,6→1 in 250 ms (ease-out-back). Nessun elemento intercetta input mentre è nascosto.
    /// Uso: HudReveal.Register("hud.pillolaValute", el); HudReveal.Hide(id); HudReveal.Show(id); HudReveal.ShowAll().</summary>
    public static class HudReveal
    {
        public const float DurationSec = 0.25f;
        public const string Cls = "reveal", ClsHidden = "reveal--hidden", ClsIn = "reveal--in";

        /// <summary>Dopo ogni comparsa (id, posizione mondo o null per l'HUD): aggancio per camera breve e log.</summary>
        public static event Action<string, Vector3?> Revealed;

        static readonly Dictionary<string, VisualElement> ui = new();
        static readonly Dictionary<string, GameObject> world = new();
        static readonly HashSet<string> hidden = new();
        static Runner runner;

        public static bool IsHidden(string id) => hidden.Contains(id);
        public static IEnumerable<string> HiddenIds => hidden;

        public static void Register(string id, VisualElement e)
        {
            if (string.IsNullOrEmpty(id) || e == null) return;
            ui[id] = e; e.AddToClassList(Cls);
            if (hidden.Contains(id)) ApplyHiddenUi(e);
        }

        public static void RegisterWorld(string id, GameObject go)
        {
            if (string.IsNullOrEmpty(id) || !go) return;
            world[id] = go;
            if (hidden.Contains(id)) go.SetActive(false);
        }

        public static void Hide(string id)
        {
            hidden.Add(id);
            if (ui.TryGetValue(id, out var e) && e != null) ApplyHiddenUi(e);
            if (world.TryGetValue(id, out var go) && go) go.SetActive(false);
        }

        public static void HideAll(IEnumerable<string> ids) { foreach (var id in ids) Hide(id); }

        /// <summary>Comparsa animata. Idempotente: se è già visibile non fa nulla.</summary>
        public static void Show(string id)
        {
            if (!hidden.Remove(id)) return;
            Vector3? at = null;
            if (ui.TryGetValue(id, out var e) && e != null)
            {
                e.style.display = DisplayStyle.Flex; e.pickingMode = PickingMode.Position;
                e.AddToClassList(ClsHidden); e.AddToClassList(ClsIn);
                e.schedule.Execute(() => e.RemoveFromClassList(ClsHidden)).StartingIn(16);          // transizione USS hidden → rest
                e.schedule.Execute(() => e.RemoveFromClassList(ClsIn)).StartingIn((long)(DurationSec * 1000) + 32);
            }
            if (world.TryGetValue(id, out var go) && go)
            {
                go.SetActive(true); at = go.transform.position;
                EnsureRunner().StartCoroutine(ScaleIn(go.transform));
            }
            OnbLog.Write($"reveal {id}");
            Revealed?.Invoke(id, at);
        }

        public static void ShowAll() { foreach (var id in new List<string>(hidden)) Show(id); }

        /// <summary>Solo per test/ricarica scena.</summary>
        public static void ResetAll() { ui.Clear(); world.Clear(); hidden.Clear(); }

        static void ApplyHiddenUi(VisualElement e) { e.AddToClassList(ClsHidden); e.style.display = DisplayStyle.None; }

        static IEnumerator ScaleIn(Transform t)
        {
            var target = t.localScale; if (target == Vector3.zero) target = Vector3.one;
            for (float k = 0; k < DurationSec; k += Time.unscaledDeltaTime)
            {
                if (!t) yield break;
                float x = k / DurationSec, c = 1.70158f; // ease-out-back
                float s = 1f + (c + 1f) * Mathf.Pow(x - 1f, 3) + c * Mathf.Pow(x - 1f, 2);
                t.localScale = target * Mathf.LerpUnclamped(0.6f, 1f, s);
                yield return null;
            }
            if (t) t.localScale = target;
        }

        static Runner EnsureRunner()
        {
            if (runner) return runner;
            var go = new GameObject("HudRevealRunner") { hideFlags = HideFlags.HideAndDontSave };
            return runner = go.AddComponent<Runner>();
        }
        sealed class Runner : MonoBehaviour { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset()
        {
            ResetAll(); Revealed = null; runner = null;
            // P0: lo stato statico non deve sopravvivere al cambio scena (PlayMode senza domain reload):
            // altrimenti un id nascosto in una scena precedente (es. pad.assumiNonno) spegne l'oggetto nuovo.
            SceneManager.sceneUnloaded -= OnSceneUnloaded; SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        static void OnSceneUnloaded(Scene _) => ResetAll();
    }
}
