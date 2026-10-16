using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UltimoMiglio.Core
{
    /// <summary>#91 / docs/design/13_CAP1_EVENTI_ONBOARDING.md §1: righe di log dell'onboarding per il QA (console del browser).
    /// Formato: <c>[ONB] &lt;tipo&gt; id=&lt;idPasso&gt; n=&lt;indice&gt; t=&lt;s, 1 decimale&gt; [chiave=valore ...]</c>.</summary>
    public static class OnbLog
    {
        public const string Pattern = @"^\[ONB\] (start|gate|hint|softgate|nolnp|skip|end) id=[A-Za-z]+ n=\d{1,2} t=\d+\.\d( [a-zA-Z]+=[^ ]+)*$";
        public static readonly Regex Rx = new Regex(Pattern);
        static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

        public static string S1(double v) => System.Math.Max(0, v).ToString("0.0", Ci);

        /// <summary>extras: coppie chiave, valore (gli spazi nei valori diventano "_").</summary>
        public static string Line(string type, string id, int n, double t, params object[] extras)
        {
            var sb = new StringBuilder(64).Append("[ONB] ").Append(type).Append(" id=").Append(id).Append(" n=").Append(n).Append(" t=").Append(S1(t));
            for (int i = 0; i + 1 < extras.Length; i += 2)
            {
                var v = extras[i + 1]; string s = v is double d ? S1(d) : v is float f ? S1(f) : System.Convert.ToString(v, Ci);
                sb.Append(' ').Append(extras[i]).Append('=').Append(string.IsNullOrEmpty(s) ? "-" : s.Replace(' ', '_'));
            }
            return sb.ToString();
        }
        public static bool IsValid(string line) => line != null && Rx.IsMatch(line);
    }
}
