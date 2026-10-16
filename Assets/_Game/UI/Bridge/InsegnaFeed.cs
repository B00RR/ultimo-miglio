using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.UI.Bridge
{
    /// <summary>Scrive CARICO n/capacità sul pad e il contatore dell'ordine (pacchi verso il tetto del passo Arrivi).
    /// Non modifica HudGameBridge: la PR del telefono di capitolo 1 resta indipendente.</summary>
    public sealed class InsegnaFeed : MonoBehaviour
    {
        /// <summary>Stesso tetto del passo Arrivi in TutorialManager (min(3, capacità)). Non è un costo di bilancio.</summary>
        public const int OrdinePacchi = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            if (!hud || hud.GetComponent<InsegnaFeed>()) return;
            hud.gameObject.AddComponent<InsegnaFeed>();
        }

        void Update()
        {
            var hud = GetComponent<HudController>();
            if (!hud || hud.Root == null || !GameRoot.Ready || GameRoot.I == null) return;
            var nina = GameRoot.I.Nina;
            if (nina == null || nina.Stack == null) return;
            int cap = Mathf.Max(1, nina.Stack.Capacity);
            int n = Mathf.Max(0, nina.Stack.Count);
            Set(hud.Root, "cargo-label", "CARICO " + n + "/" + cap);
            int goal = Mathf.Min(OrdinePacchi, cap);
            string prog = Mathf.Min(n, goal) + "/" + goal;
            Set(hud.Root, "order-count", prog);
            Set(hud.Root, "order-chip-count", prog);
        }

        static void Set(VisualElement root, string name, string text)
        {
            var label = root.Q<Label>(name);
            if (label != null && label.text != text) label.text = text;
        }
    }
}
