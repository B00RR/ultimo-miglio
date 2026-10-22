using System.Globalization;

namespace UltimoMiglio.UI
{
    /// <summary>Italian number formatting: 1.240 / 8,5 / 1,2K / 3,4M. Tabular digits come from the USS font settings.</summary>
    public static class HudFormat
    {
        // WebGL/IL2CPP (invariant globalization) may not ship it-IT: a throwing static ctor would kill the whole HUD.
        static readonly CultureInfo It = MakeIt();
        static CultureInfo MakeIt()
        {
            try { return CultureInfo.GetCultureInfo("it-IT"); }
            catch (System.Exception)
            {
                var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                c.NumberFormat.NumberDecimalSeparator = ","; c.NumberFormat.NumberGroupSeparator = ".";
                c.NumberFormat.PercentDecimalSeparator = ","; c.NumberFormat.PercentGroupSeparator = ".";
                return c;
            }
        }

        /// <summary>Taglia alla precisione già mostrata, verso il basso: il saldo in pillola non supera mai il reale.</summary>
        static double Taglia(double v, int decimali)
        {
            double f = System.Math.Pow(10, decimali);
            return System.Math.Floor(v * f) / f;
        }

        public static string Amount(double v)
        {
            double a = System.Math.Abs(v);
            if (a < 10 && a != System.Math.Floor(a)) return Taglia(v, 1).ToString("0.#", It);
            if (a < 100000) return Taglia(v, 0).ToString("#,0", It);
            if (a < 1e6) return Taglia(v / 1e3, 1).ToString("0.#", It) + "K";
            if (a < 1e9) return Taglia(v / 1e6, 1).ToString("0.#", It) + "M";
            if (a < 1e12) return Taglia(v / 1e9, 1).ToString("0.#", It) + "B";
            return Taglia(v / 1e12, 1).ToString("0.#", It) + "T";
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
