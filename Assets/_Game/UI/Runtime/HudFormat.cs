using System.Globalization;

namespace UltimoMiglio.UI
{
    /// <summary>Numeri in italiano: 1.240 / 8,5 / 1,2K / 3,4M. Le cifre tabulari arrivano dalle impostazioni del font nella USS.</summary>
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

        /// <summary>Taglia alla precisione già mostrata, verso lo zero.</summary>
        static double Taglia(double v, int decimali)
        {
            double f = System.Math.Pow(10, decimali);
            return System.Math.Truncate(v * f) / f;
        }

        public static string Amount(double v)
        {
            // Segno a parte: si tronca il valore assoluto, e un negativo che arriva a zero resta «0».
            if (v < 0)
            {
                string s = Amount(-v);
                return s == "0" ? s : "-" + s;
            }
            if (v < 10 && v != System.Math.Floor(v)) return Taglia(v, 1).ToString("0.#", It);
            if (v < 100000) return Taglia(v, 0).ToString("#,0", It);
            if (v < 1e6) return Taglia(v / 1e3, 1).ToString("0.#", It) + "K";
            if (v < 1e9) return Taglia(v / 1e6, 1).ToString("0.#", It) + "M";
            if (v < 1e12) return Taglia(v / 1e9, 1).ToString("0.#", It) + "B";
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
