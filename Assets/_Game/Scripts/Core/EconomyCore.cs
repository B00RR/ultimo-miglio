using System;

namespace UltimoMiglio.Core
{
    public enum UpgradeKind { Zaino, Scarpe, Insegna, NonnoHire, NonnoLevel }

    /// <summary>Engine-free world position (converted to/from UnityEngine.Vector3 in Gameplay).</summary>
    [Serializable] public struct WorldPos { public float x, y, z; public WorldPos(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } }

    /// <summary>Serializable game state (save v1). Pure data.</summary>
    [Serializable]
    public sealed class EconomyState
    {
        public int SaveVersion = 1;
        public double Wallet;            // spendable LN (40%)
        public double DebtPaid;          // LN versed on current tranche (60%)
        public double ChapterEarnedActive, ChapterEarnedOffline;
        public int Zaino, Scarpe, Insegna, NonnoLevel = -1; // -1 = not hired
        public double SpentLn;           // #121: LN spese in totale nei pad (esclusi i versamenti alla Cassaforte)
        public double[] PadPaid = new double[5]; // installment progress per UpgradeKind (saved)
        public double Combo = 1.0; public double LastServeTime = -999;
        public double ActiveSec;         // active (foreground) seconds this chapter
        public double CassaLn;           // offline LN waiting in the Cassa
        public long LastSeenUnix;
        public int AdDoublesToday; public long AdDayIndex;
        public int CustomersServed, CustomersLost;
        public bool ChapterComplete;
        public int ParcelMilestonesHit, DebtMilestonesHit, Reputation; public bool CassaTipGiven; // #103 rewards (save)
        /// <summary>#223: true finché il tutorial non arriva alla carta del debito (passo debtCard): tutto l'incasso va nel portafoglio, niente quota al debito.
        /// Default false: salvataggi vecchi e partite senza tutorial restano con lo split attivo.</summary>
        public bool DebtSplitLocked;
        public int debt_ms;              // #136: bitmask delle tappe del debito già mostrate (nel salvataggio: si azzera con la partita nuova)
        public double Bulloni; public int ProrogaTokens = -1; public double DueActiveSec = -1; public bool Mora; public int Day = 1;
    }

    /// <summary>Deterministic, engine-free economy (02_TECH §2). All mutations go through here.</summary>
    public sealed class EconomyCore
    {
        public readonly EconomyConfig Cfg; public EconomyState S;
        public event Action<string, string> OnEvent; // (type, json-fields) -> QA log
        /// <summary>LN credited to the player: (gross, net after Pedaggio, world position of the pickup).</summary>
        public event Action<double, double, WorldPos> OnLnCredited;
        public event Action<bool> OnMoraChanged;
        public event Action<int> DayChanged;          // calendar day 1..YearDays
        public event Action<double> BulloniChanged;    // new total
        /// <summary>#103: ricompensa intermedia (kind = "parcel" | "debt" | "cassa_tip", index soglia, LN accreditati). Hook per coriandoli/timbro/decor/riga di Mamma Rosa.</summary>
        public event Action<string, int, double> RewardGranted;

        /// <summary>#110: sosta del pad aperta. Il costruttore la apre sul livello già in corso (anche da salvataggio).
        /// Si chiude all'acquisto: gli altri tick della stessa sosta non partono sul livello successivo.</summary>
        readonly bool[] _padOpen = new bool[5];

        public EconomyCore(EconomyConfig cfg, EconomyState state = null) { Cfg = cfg ?? throw new ArgumentNullException(nameof(cfg)); S = state ?? new EconomyState();
            if (S.ProrogaTokens < 0) S.ProrogaTokens = Math.Min(Cfg.ProrogaTokensAtActStart, Cfg.ProrogaTokensMax); // cap 1 = start of Act I
            if (S.DueActiveSec < 0) S.DueActiveSec = Cfg.WindowActiveMin * 60;
            for (int i = 0; i < _padOpen.Length; i++) _padOpen[i] = true;
        }

        /// <summary>#110: Nina è entrata di nuovo sul pad (dopo essere uscita). Riattiva il pagamento del livello corrente.</summary>
        public void BeginPadStay(UpgradeKind k)
        {
            int i = (int)k; if (i >= 0 && i < _padOpen.Length) _padOpen[i] = true;
        }

        void Emit(string type, string fields) => OnEvent?.Invoke(type, fields);
        static string F(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        // ---------- Nina ----------
        public int StackCapacity => Cfg.ZainoCapacity[Math.Min(S.Zaino, Cfg.ZainoCapacity.Length - 1)];
        public double NinaSpeed(int stackCount)
        {
            double v = Cfg.NinaBaseSpeed * (1 + Cfg.ScarpeBonus[Math.Min(S.Scarpe, Cfg.ScarpeBonus.Length - 1)]);
            return stackCount >= StackCapacity ? v * (1 - Cfg.FullStackSlowdown) : v;
        }

        public double TrancheDue => Cfg.TrancheLn * (S.Mora ? 1 + Cfg.MoraTrancheSurcharge : 1);
        public double TollFraction => S.Mora ? Cfg.PedaggioCoinPenalty : 0;
        public int CalendarDay => Math.Min(Cfg.YearDays, 1 + (int)Math.Floor(S.ActiveSec / Cfg.CycleSec * Cfg.CalendarDaysPerCycle));

        /// <summary>Advance active (foreground) time: calendar day, deadline → auto Proroga → mora.</summary>
        public void Tick(double dt)
        {
            S.ActiveSec += dt;
            int d = CalendarDay; if (d != S.Day) { S.Day = d; DayChanged?.Invoke(d); }
            if (S.ChapterComplete || S.Mora || S.ActiveSec <= S.DueActiveSec) return;
            if (S.ProrogaTokens > 0)
            {
                S.ProrogaTokens--; S.DueActiveSec += Cfg.ProrogaExtensionActiveMin * 60;
                Emit("proroga_used", $"\"tokens_left\":{S.ProrogaTokens}");
            }
            else SetMora(true);
        }
        void SetMora(bool on) { if (S.Mora == on) return; S.Mora = on; Emit("mora", $"\"on\":{(on ? "true" : "false")}"); OnMoraChanged?.Invoke(on); }

        public void AddBulloni(double n, string reason)
        {
            if (n == 0) return; S.Bulloni += n; Emit("bulloni", $"\"delta\":{F(n)},\"reason\":\"{reason}\"");
            BulloniChanged?.Invoke(S.Bulloni);
        }

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
            CheckParcelMilestones();
            return ln;
        }
        public void CustomerLost() { S.CustomersLost++; Emit("customer_lost", ""); }

        /// <summary>Coins picked up from the floor: split 40% wallet / 60% debt.</summary>
        public void CollectActive(double ln, WorldPos at = default)
        {
            double net = ln * (1 - TollFraction);
            S.ChapterEarnedActive += net; Split(net); OnLnCredited?.Invoke(ln, net, at);
        }

        void Split(double ln)
        {
            if (S.DebtSplitLocked) { S.Wallet += ln; return; } // #223: trattenuta sul debito solo dopo il passo debtCard
            S.Wallet += ln * Cfg.PlayerSpendShare;
            double toDebt = ln * (1 - Cfg.PlayerSpendShare);
            S.DebtPaid += toDebt;
            CheckDebtMilestones();
            if (!S.ChapterComplete && S.DebtPaid >= TrancheDue)
            {
                S.ChapterComplete = true; Emit("vault_deposit", $"\"tranche\":{F(TrancheDue)}");
                if (S.DueActiveSec - S.ActiveSec >= Cfg.EarlyPaymentMinCycles * Cfg.CycleSec) AddBulloni(Cfg.EarlyPaymentBulloni, "early_payment");
                SetMora(false);
                Emit("chapter_complete", $"\"chapter\":{Cfg.Chapter},\"active_min\":{F(S.ActiveSec / 60)}");
            }
        }

        // ---------- Ricompense intermedie (#103, economy.json → rewards) ----------
        /// <summary>Pacchi consegnati = clienti serviti (1 pacco per cliente). Bonus LN diretto nel portafoglio (non diviso col debito).</summary>
        void CheckParcelMilestones()
        {
            while (S.ParcelMilestonesHit < Cfg.ParcelMilestones.Length && S.CustomersServed >= Cfg.ParcelMilestones[S.ParcelMilestonesHit])
            {
                int i = S.ParcelMilestonesHit++; double b = i < Cfg.ParcelMilestoneBonusLn.Length ? Cfg.ParcelMilestoneBonusLn[i] : 0;
                S.Wallet += b;
                Emit("reward", $"\"kind\":\"parcel\",\"parcels\":{Cfg.ParcelMilestones[i]},\"ln\":{F(b)}");
                RewardGranted?.Invoke("parcel", i, b);
            }
        }
        public double DebtShare => TrancheDue <= 0 ? 0 : S.DebtPaid / TrancheDue;
        /// <summary>25% timbro (solo visivo, hook), 50% +1 reputazione, 75% decor (hook con itemId in Cfg).</summary>
        void CheckDebtMilestones()
        {
            while (S.DebtMilestonesHit < Cfg.DebtMilestoneShares.Length && DebtShare + 1e-9 >= Cfg.DebtMilestoneShares[S.DebtMilestonesHit])
            {
                int i = S.DebtMilestonesHit++;
                string r = i < Cfg.DebtMilestoneRewards.Length ? Cfg.DebtMilestoneRewards[i] : "";
                if (r == "reputation") S.Reputation += i < Cfg.DebtMilestoneAmounts.Length ? Math.Max(1, Cfg.DebtMilestoneAmounts[i]) : 1;
                Emit("reward", $"\"kind\":\"debt\",\"share\":{F(Cfg.DebtMilestoneShares[i])},\"reward\":\"{r}\"");
                RewardGranted?.Invoke("debt", i, 0);
            }
        }
        /// <summary>Mancia della Cassa alla prima visita (una volta per partita). Ritorna i LN dati.</summary>
        public double CassaFirstVisitTip()
        {
            if (S.CassaTipGiven || Cfg.CassaFirstVisitTipLn <= 0) return 0;
            S.CassaTipGiven = true; S.Wallet += Cfg.CassaFirstVisitTipLn;
            Emit("reward", $"\"kind\":\"cassa_tip\",\"ln\":{F(Cfg.CassaFirstVisitTipLn)}");
            RewardGranted?.Invoke("cassa_tip", 0, Cfg.CassaFirstVisitTipLn);
            return Cfg.CassaFirstVisitTipLn;
        }
        static readonly System.Collections.Generic.Dictionary<string, UpgradeKind> PadIds = new System.Collections.Generic.Dictionary<string, UpgradeKind>
        { { "zaino", UpgradeKind.Zaino }, { "scarpe", UpgradeKind.Scarpe }, { "insegna", UpgradeKind.Insegna }, { "nonno", UpgradeKind.NonnoLevel } };
        /// <summary>Livello che il prossimo acquisto del pad porterebbe ("scarpe:2" = Scarpe livello 2; "nonno:2" = NONNO livello 2).</summary>
        public int NextLevel(UpgradeKind k) { switch (k) { case UpgradeKind.Zaino: return S.Zaino + 1; case UpgradeKind.Scarpe: return S.Scarpe + 1; case UpgradeKind.Insegna: return S.Insegna + 1; case UpgradeKind.NonnoHire: return 0; default: return S.NonnoLevel + 1; } }
        /// <summary>#103 nextPadArrow: dal fromSec di gioco attivo, il candidato più economico ancora da comprare e coperto dal portafoglio; null se nessuno.</summary>
        public UpgradeKind? NextPadArrowTarget()
        {
            if (Cfg.NextPadArrowFromSec <= 0 || S.ActiveSec < Cfg.NextPadArrowFromSec) return null;
            UpgradeKind? best = null; double bestCost = double.PositiveInfinity;
            foreach (var c in Cfg.NextPadArrowCandidates)
            {
                var parts = (c ?? "").Split(':'); if (parts.Length != 2 || !PadIds.TryGetValue(parts[0].Trim().ToLowerInvariant(), out var k)) continue;
                if (!int.TryParse(parts[1], out int lvl) || NextLevel(k) != lvl) continue;
                double cost = Cost(k); if (double.IsInfinity(cost) || cost - S.PadPaid[(int)k] > S.Wallet + 1e-9) continue;
                if (cost < bestCost) { bestCost = cost; best = k; }
            }
            return best;
        }

        // ---------- Upgrades / pads (installments) ----------
        public double Cost(UpgradeKind k)
        {
            switch (k)
            {
                case UpgradeKind.Zaino: return S.Zaino < Cfg.ZainoMaxLevel && S.Zaino + 1 < Cfg.ZainoCost.Length ? Cfg.ZainoCost[S.Zaino + 1] : double.PositiveInfinity;
                case UpgradeKind.Scarpe: return S.Scarpe + 1 < Cfg.ScarpeCost.Length ? Cfg.ScarpeCost[S.Scarpe + 1] : double.PositiveInfinity;
                case UpgradeKind.Insegna: return S.Insegna + 1 < Cfg.InsegnaCost.Length ? Cfg.InsegnaCost[S.Insegna + 1] : double.PositiveInfinity;
                case UpgradeKind.NonnoHire: return S.NonnoLevel >= 0 ? double.PositiveInfinity : Cfg.NonnoHireCost;
                default: return S.NonnoLevel < 0 || S.NonnoLevel >= Cfg.NonnoMaxLevel ? double.PositiveInfinity : Cfg.NonnoUpgradeCost(S.NonnoLevel + 1);
            }
        }
        public double PadFill(UpgradeKind k) { double c = Cost(k); return double.IsInfinity(c) ? 1 : S.PadPaid[(int)k] / c; }

        /// <summary>#110: LN ancora da pagare sul livello corrente. Infinito se il pad è al massimo (niente prezzo).</summary>
        public double PrezzoResiduo(UpgradeKind k)
        {
            double c = Cost(k); if (double.IsInfinity(c)) return double.PositiveInfinity;
            return Math.Max(0, c - S.PadPaid[(int)k]);
        }

        /// <summary>One 0.1 s pad tick. Accelerating curve: pays cost*tickFraction scaled by dwell so the pad fills in ≤PadFillMaxSec.
        /// Returns true when the upgrade completes. #110: dopo l'acquisto la sosta si chiude — il livello successivo
        /// non viene addebitato finché non si richiama BeginPadStay (Nina esce e rientra).</summary>
        public bool PadTick(UpgradeKind k, double dwellSec)
        {
            int i = (int)k; if (i < 0 || i >= _padOpen.Length || !_padOpen[i]) return false;
            double cost = Cost(k); if (double.IsInfinity(cost) || S.Wallet <= 0) return false;
            double remaining = cost - S.PadPaid[i];
            // accelerating: rate grows linearly with dwell; integral over PadFillMaxSec = full cost
            double T = Cfg.PadFillMaxSec, dt = Cfg.ZoneTickSec;
            double frac = Math.Max(dt / T, (2 * dwellSec * dt) / (T * T));
            double pay = Math.Min(Math.Min(remaining, S.Wallet), Math.Max(1, Math.Ceiling(cost * frac)));
            S.Wallet -= pay; S.PadPaid[i] += pay; S.SpentLn += pay; // #121: LN spese in pad/assunzioni (log [ONB] spent)
            Emit("pad_progress", $"\"id\":\"{k}\",\"ln\":{F(pay)},\"paid\":{F(S.PadPaid[i])},\"cost\":{F(cost)}");
            if (S.PadPaid[i] + 1e-9 < cost) return false;
            S.PadPaid[i] = 0; Apply(k); _padOpen[i] = false;
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
        public double CollectCassa(bool doubled, long dayIndex, WorldPos at = default)
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
            double gross = ln; if (Cfg.PedaggioAppliesToOffline) ln *= 1 - TollFraction;
            S.ChapterEarnedOffline += ln; Split(ln); OnLnCredited?.Invoke(gross, ln, at);
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
