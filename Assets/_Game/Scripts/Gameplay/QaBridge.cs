using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>
    /// Ponte di lettura per il playtest nel browser. Si accende solo se l'URL contiene <c>qa=1</c>.
    /// Pubblica posizione di Kaia, passo del tutorial e bersaglio. Non cambia il gioco.
    /// </summary>
    public sealed class QaBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void UmQaSet(string json);
#endif
        bool _attivo;
        bool _deciso;
        float _t;
        readonly StringBuilder _sb = new StringBuilder(512);

        void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!_deciso)
            {
                var url = Application.absoluteURL;
                if (string.IsNullOrEmpty(url)) return;
                _deciso = true;
                _attivo = url.IndexOf("qa=1", System.StringComparison.Ordinal) >= 0;
                if (!_attivo) { enabled = false; return; }
            }
            if (!_attivo || !GameRoot.Ready) return;
            _t += Time.unscaledDeltaTime;
            if (_t < 0.2f) return;
            _t = 0f;
            UmQaSet(Stato());
#else
            enabled = false;
#endif
        }

        string Stato()
        {
            var nina = GameRoot.I.Nina;
            var eco = GameRoot.I.Eco;
            var tut = TutorialManager.I;
            var cam = Camera.main;
            float yaw = cam ? cam.transform.eulerAngles.y : 45f;
            Vector3 kp = nina ? nina.transform.position : Vector3.zero;
            var target = tut ? tut.CurrentTarget : null;
            Vector3 tp = target ? target.position : Vector3.zero;
            // La freccia del tutorial non va dritta: punta al prossimo varco (TappaGuida).
            Vector3 via = tut && target ? tut.TappaVerso(target) : tp;
            if (via.sqrMagnitude < 1e-6f) via = tp;
            var set = FindAnyObjectByType<SettingsController>();
            var coda = FindAnyObjectByType<CustomerQueue>();
            _sb.Clear();
            _sb.Append("{\"ready\":true");
            JStr("step", tut ? tut.Current.ToString() : "");
            JNum("stepN", tut ? (int)tut.Current : -1);
            JStr("objective", tut ? tut.CurrentObjectiveText : "");
            JStr("text", tut ? tut.CurrentText : "");
            JStr("hint", tut ? tut.CurrentHint() : "");
            JNum("wallet", eco != null ? eco.S.Wallet : 0);
            JNum("zaino", eco != null ? eco.S.Zaino : 0);
            JNum("nonno", eco != null ? eco.S.NonnoLevel : -1);
            JNum("served", eco != null ? eco.S.CustomersServed : 0);
            JNum("stack", nina && nina.Stack != null ? nina.Stack.Count : 0);
            JNum("active", eco != null ? eco.S.ActiveSec : 0);
            JNum("yaw", yaw);
            JBool("impostazioni", set && set.IsOpen);
            JNum("coda", coda ? coda.Waiting : 0);
            JBool("skip", SkipAcceso());
            _sb.Append(",\"kaia\":{\"x\":").Append(F(kp.x)).Append(",\"z\":").Append(F(kp.z)).Append('}');
            _sb.Append(",\"target\":{\"name\":");
            Quote(target ? target.name : "");
            _sb.Append(",\"x\":").Append(F(tp.x)).Append(",\"z\":").Append(F(tp.z)).Append('}');
            _sb.Append(",\"tappa\":{\"name\":");
            Quote(target ? "tappa" : "");
            _sb.Append(",\"x\":").Append(F(via.x)).Append(",\"z\":").Append(F(via.z)).Append('}');
            _sb.Append('}');
            return _sb.ToString();
        }

        static bool SkipAcceso()
        {
            var tut = TutorialManager.I;
            if (!tut) return false;
            var hud = FindAnyObjectByType<HudController>();
            var b = hud && hud.Root != null ? hud.Root.Q("skip-tutorial") : null;
            return b != null && b.ClassListContains("skip-tut--on") && b.resolvedStyle.display != DisplayStyle.None;
        }

        void JStr(string k, string v) { _sb.Append(",\"").Append(k).Append("\":"); Quote(v); }
        void JNum(string k, double v) { _sb.Append(",\"").Append(k).Append("\":").Append(F(v)); }
        void JBool(string k, bool v) { _sb.Append(",\"").Append(k).Append("\":").Append(v ? "true" : "false"); }
        void Quote(string v)
        {
            _sb.Append('"');
            if (!string.IsNullOrEmpty(v))
                foreach (var c in v)
                {
                    if (c == '\\' || c == '"') _sb.Append('\\');
                    if (c == '\n') { _sb.Append("\\n"); continue; }
                    if (c == '\r') continue;
                    _sb.Append(c);
                }
            _sb.Append('"');
        }
        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
