using System.Globalization;

namespace UltimoMiglio.UI
{
    /// <summary>Italian number formatting: 1.240 / 8,5 / 1,2K / 3,4M. Tabular digits come from the USS font settings.</summary>
    public static class HudFormat
    {
        static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

        public static string Amount(double v)
        {
            double a = System.Math.Abs(v);
            if (a < 10 && a != System.Math.Floor(a)) return v.ToString("0.#", It);
            if (a < 100000) return v.ToString("#,0", It);
            if (a < 1e6) return (v / 1e3).ToString("0.#", It) + "K";
            if (a < 1e9) return (v / 1e6).ToString("0.#", It) + "M";
            if (a < 1e12) return (v / 1e9).ToString("0.#", It) + "B";
            return (v / 1e12).ToString("0.#", It) + "T";
        }

        public static string Percent(float fraction) =>
            (fraction * 100f).ToString("0", It) + "%";

        public static string Duration(float seconds)
        {
            int s = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.RoundToInt(seconds));
            if (s >= 3600) return (s / 3600) + " h " + ((s % 3600) / 60).ToString("00");
            if (s >= 60) return (s / 60) + " min";
            return s + " s";
        }

        public static string Clock(float seconds)
        {
            int s = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
