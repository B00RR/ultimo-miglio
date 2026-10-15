using System;

namespace UltimoMiglio.Core
{
    public enum UpgradeKind { Zaino, Scarpe, Insegna, NonnoHire, NonnoLevel }

    /// <summary>Serializable game state (save v1). Pure data.</summary>
    [Serializable]
    public sealed class EconomyState
    {
        public int SaveVersion = 1;
        public double Wallet;            // spendable LN (40%)
        public double DebtPaid;          // LN versed on current tranche (60%)
        public double ChapterEarnedActive, ChapterEarnedOffline;
        public int Zaino, Scarpe, Insegna, NonnoLevel = -1; // -1 = not hired
        public double[] PadPaid = new double[5]; // installment progress per UpgradeKind (saved)
        public double Combo = 1.0; public double LastServeTime = -999;
        public double ActiveSec;         // active (foreground) seconds this chapter
        public double CassaLn;           // offline LN waiting in the Cassa
        public long LastSeenUnix;
        public int AdDoublesToday; public long AdDayIndex;
        public int CustomersServed, CustomersLost;
        public bool ChapterComplete;
    }

    /// <summary>Deterministic, engine-free economy (02_TECH §2). All mutations go through here.</summary>
    public sealed class EconomyCore
    {
        public readonly EconomyConfig Cfg; public EconomyState S;
        public event Action<string, string> OnEvent; // (type, json-fields) -> QA log

        public EconomyCore(EconomyConfig cfg, EconomyState state = null) { Cfg = cfg ?? throw new ArgumentNullException(nameof(cfg)); S = state ?? new EconomyState(); }

        void Emit(string type, string fields) => OnEvent?.Invoke(type, fields);
        static string F(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        // ---------- Nina ----------
        public int StackCapacity => Cfg.ZainoCapacity[Math.Min(S.Zaino, Cfg.ZainoCapacity.Length - 1)];
        public double NinaSpeed(int stackCount)
        {
            double v = Cfg.NinaBaseSpeed * (1 + Cfg.ScarpeBonus[Math.Min(S.Scarpe, Cfg.ScarpeBonus.Length - 1)]);
            return stackCount >= StackCapacity ? v * (1 - Cfg.FullStackSlowdown) : v;
        }

        public void Tick(double dt) { S.ActiveSec += dt; }

        // ---------- Customers ----------
        public double CustomerValue => Cfg.ParcelValueLn * (1 + Cfg.InsegnaBonus[Math.Min(S.Insegna, Cfg.InsegnaBonus.Length - 1)]);

        /// <summary>Customer served at time t (active seconds). Returns LN dropped as coins.</summary>
        public double Serve(double t)
        {
            if (t - S.LastServeTime > Cfg.ComboResetGapSec) S.Combo = 1.0;
            double ln = CustomerValue * S.Combo;
            S.Combo = Math.Min(Cfg.ComboMax, S.Combo + Cfg.ComboStep);
            S.LastServeTime = t; S.CustomersServed++;
            Emit("serve", $"\"ln\":{F(ln)},\"combo\":{F(S.Combo)}");
            return ln;
        }
        public void CustomerLost() { S.CustomersLost++; Emit("customer_lost", ""); }

        /// <summary>Coins picked up from the floor: split 40% wallet / 60% debt.</summary>
        public void CollectActive(double ln) { S.ChapterEarnedActive += ln; Split(ln); }

        void Split(double ln)
        {
            S.Wallet += ln * Cfg.PlayerSpendShare;
            double toDebt = ln * (1 - Cfg.PlayerSpendShare);
            S.DebtPaid += toDebt;
            if (!S.ChapterComplete && S.DebtPaid >= Cfg.TrancheLn)
            {
                S.ChapterComplete = true; Emit("vault_deposit", $"\"tranche\":{F(Cfg.TrancheLn)}");
                Emit("chapter_complete", $"\"chapter\":{Cfg.Chapter},\"active_min\":{F(S.ActiveSec / 60)}");
            }
        }

        // ---------- Upgrades / pads (installments) ----------
        public double Cost(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Zaino: return S.Zaino + 1 < Cfg.ZainoCost.Length ? Cfg.ZainoCost[S.Zaino + 1] : double.PositiveInfinity;
                case UpgradeKind.Scarpe: return S.Scarpe + 1 < Cfg.ScarpeCost.Length ? Cfg.ScarpeCost[S.Scarpe + 1] : double.PositiveInfinity;
                case UpgradeKind.Insegna: return S.Insegna + 1 < Cfg.InsegnaCost.Length ? Cfg.InsegnaCost[S.Insegna + 1] : double.PositiveInfinity;
                case UpgradeKind.NonnoHire: return S.NonnoLevel >= 0 ? double.PositiveInfinity : Cfg.NonnoHireCost;
                default: return S.NonnoLevel < 0 || S.NonnoLevel >= Cfg.NonnoMaxLevel ? double.PositiveInfinity : Cfg.NonnoUpgradeCost(S.NonnoLevel + 1);
            }
        }
        public double PadFill(UpgradeKind k) { double c = Cost(k); return double.IsInfinity(c) ? 1 : S.PadPaid[(int)k] / c; }

        /// <summary>One 0.1 s pad tick. Accelerating curve: pays cost*tickFraction scaled by dwell so the pad fills in ≤PadFillMaxSec.
        /// Returns true when the upgrade completes.</summary>
        public bool PadTick(UpgradeKind k, double dwellSec)
        {
            double cost = Cost(k); if (double.IsInfinity(cost) || S.Wallet <= 0) return false;
            int i = (int)k; double remaining = cost - S.PadPaid[i];
            // accelerating: rate grows linearly with dwell; integral over PadFillMaxSec = full cost
            double T = Cfg.PadFillMaxSec, dt = Cfg.ZoneTickSec;
            double frac = Math.Max(dt / T, (2 * dwellSec * dt) / (T * T));
            double pay = Math.Min(Math.Min(remaining, S.Wallet), Math.Max(1, Math.Ceiling(cost * frac)));
            S.Wallet -= pay; S.PadPaid[i] += pay;
            Emit("pad_progress", $"\"id\":\"{k}\",\"ln\":{F(pay)},\"paid\":{F(S.PadPaid[i])},\"cost\":{F(cost)}");
            if (S.PadPaid[i] + 1e-9 < cost) return false;
            S.PadPaid[i] = 0; Apply(k);
            Emit("pad_done", $"\"id\":\"{k}\",\"ln\":{F(cost)}");
            return true;
        }
        void Apply(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Zaino: S.Zaino++; break;
                case UpgradeKind.Scarpe: S.Scarpe++; break;
                case UpgradeKind.Insegna: S.Insegna++; break;
                case UpgradeKind.NonnoHire: S.NonnoLevel = 0; break;
                case UpgradeKind.NonnoLevel: S.NonnoLevel++; break;
            }
        }

        // ---------- NONNO ----------
        public double NonnoRestockPpm => S.NonnoLevel < 0 ? 0 : Cfg.NonnoRestockPpm[S.NonnoLevel];
        public double NonnoSelfServicePpm => S.NonnoLevel < 0 ? 0 : Cfg.NonnoSelfServicePpm[S.NonnoLevel];

        // ---------- Offline = Cassa (02 §4) ----------
        /// <summary>Robot automatic-sales LN per second (the offline basis).</summary>
        public double RobotLnPerSec => NonnoSelfServicePpm / 60.0 * CustomerValue;
        public double OfflineRatePerSec => RobotLnPerSec * Cfg.OfflineRateShare;

        /// <summary>Called on resume. Returns LN added to the Cassa. Clock going backwards = no gain.</summary>
        public double ApplyOffline(long nowUnix)
        {
            if (S.LastSeenUnix == 0 || nowUnix <= S.LastSeenUnix) { S.LastSeenUnix = Math.Max(S.LastSeenUnix, nowUnix); return 0; }
            double delta = Math.Min(nowUnix - S.LastSeenUnix, Cfg.MaxOfflineSec);
            S.LastSeenUnix = nowUnix;
            return AddToCassa(delta * OfflineRatePerSec);
        }
        double OfflineHeadroom()
        {
            // offline ≤ share of the chapter's coins: offline/(active+offline) ≤ m  ⇒ offline ≤ active*m/(1-m)
            double m = Cfg.OfflineShareOfChapterMax;
            double cap = S.ChapterEarnedActive * m / (1 - m) - S.ChapterEarnedOffline - S.CassaLn;
            return Math.Max(0, Math.Min(cap, Cfg.CassaCapacity - S.CassaLn));
        }
        public bool CassaFull => OfflineHeadroom() <= 0;
        double AddToCassa(double ln) { double add = Math.Max(0, Math.Min(ln, OfflineHeadroom())); S.CassaLn += add; return add; }

        /// <summary>Nina walks over the Cassa. x2 (ad/Bulloni) max AdDoubleMaxPerDay per server-day; still capped.</summary>
        public double CollectCassa(bool doubled, long dayIndex)
        {
            if (S.AdDayIndex != dayIndex) { S.AdDayIndex = dayIndex; S.AdDoublesToday = 0; }
            double ln = S.CassaLn; S.CassaLn = 0;
            if (doubled && S.AdDoublesToday < Cfg.AdDoubleMaxPerDay)
            {
                S.AdDoublesToday++;
                double m = Cfg.OfflineShareOfChapterMax;
                double cap = Math.Max(0, S.ChapterEarnedActive * m / (1 - m) - S.ChapterEarnedOffline);
                ln = Math.Min(ln * 2, Math.Max(ln, cap));
            }
            if (ln <= 0) return 0;
            S.ChapterEarnedOffline += ln; Split(ln);
            Emit("cassa_collect", $"\"ln\":{F(ln)},\"x2\":{(doubled ? "true" : "false")}");
            return ln;
        }

        public static string Format(double v)
        {
            string[] u = { "", "K", "M", "B", "T" }; int i = 0;
            while (Math.Abs(v) >= 1000 && i < u.Length - 1) { v /= 1000; i++; }
            return i == 0 ? Math.Floor(v).ToString(System.Globalization.CultureInfo.InvariantCulture)
                          : v.ToString(v < 10 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture) + u[i];
        }
    }
}
