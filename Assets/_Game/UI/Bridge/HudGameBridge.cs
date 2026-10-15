using UnityEngine;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI.Data;
using UltimoMiglio.UI.World;

namespace UltimoMiglio.UI.Bridge
{
    /// <summary>
    /// Glue between the Tech Lead's gameplay (GameRoot / EconomyCore / PackageStack) and the HUD, kept outside
    /// Gameplay so no TL file has to change. Reads state only (polling GameRoot.Changed), never writes economy.
    /// Things the core doesn't expose yet (Bulloni, mora/TollState, DayClock, orders, chats) are left to
    /// explicit HUD API calls; see README_HUD.md §Hooks.
    /// </summary>
    public sealed class HudGameBridge : MonoBehaviour
    {
        public HudController Hud;
        public PhoneController Phone;
        public FloatingJoystick Joystick;
        public StackCounter StackCounter;   // world-anchored above Nina
        SettingsPanel _settings; bool _settingsWired;
        public CassaCard CassaCard;         // world-anchored above the Cassa
        public EdgeArrow EdgeArrow;
        public Transform Cassa;             // for the offline edge arrow "Cassa · X LN"

        double lastWallet = double.NaN, lastCassa = double.NaN;
        int lastCount = -1, lastCap = -1;
        bool toastShown;

        void Start()
        {
            var gr = GameRoot.I; if (!GameRoot.Ready || !Hud) { enabled = false; return; }
            var data = Resources.Load<ChapterDataAsset>("Ch1Data");
            if (data) Hud.Config = new ChapterDataHudConfig(data.Data);
            gr.Changed += Refresh;
            if (Phone) Hud.PhoneButtonTapped += () => Phone.OpenPhone();
            WireSettings();
            var eco = gr.Eco;
            eco.OnLnCredited += OnLn; eco.OnMoraChanged += OnMora; eco.DayChanged += OnDay; eco.BulloniChanged += OnBulloni;
            Hud.PedaggioTapped += ShowSafe;
            if (eco.S.Mora) OnMora(true);
            Refresh();
        }

        void OnDestroy()
        {
            var gr = GameRoot.I; if (!gr) return;
            gr.Changed -= Refresh;
            var eco = gr.Eco; if (eco == null) return;
            eco.OnLnCredited -= OnLn; eco.OnMoraChanged -= OnMora; eco.DayChanged -= OnDay; eco.BulloniChanged -= OnBulloni;
        }

        // ---- core events (EconomyCore, pure C#) ----
        void OnLn(double gross, double net, UltimoMiglio.Core.WorldPos at)
        {
            var cam = Camera.main; if (!cam) return;
            Vector3 sp = cam.WorldToScreenPoint(at.ToV3() + Vector3.up * 1.2f);
            Hud.ShowCollectPopup(new Vector2(sp.x, sp.y), net, (float)GameRoot.I.Eco.TollFraction);
        }
        void OnMora(bool on) { if (on) Hud.ShowPedaggio((float)GameRoot.I.Eco.TollFraction); else Hud.HidePedaggio(); }
        void OnDay(int day) { if (safeOpen) ShowSafe(); }
        void OnBulloni(double total) { lastWallet = double.NaN; Refresh(); }

        bool safeOpen;
        void ShowSafe()
        {
            var eco = GameRoot.I.Eco; var c = eco.Cfg;
            safeOpen = true; Hud.SafeCardClosed -= SafeClosed; Hud.SafeCardClosed += SafeClosed;
            Hud.ShowSafeCard(new SafeCardData
            {
                chapter = c.Chapter, trancheTotal = eco.TrancheDue, tranchePaid = eco.S.DebtPaid,
                day = eco.CalendarDay, daysPerYear = c.YearDays, installmentIndex = c.Chapter,
                installmentAmount = eco.TrancheDue, installmentDueDay = c.DueCalendarDay,
                tollActive = eco.S.Mora, tollFraction = (float)c.PedaggioCoinPenalty,
            });
        }
        void SafeClosed() { safeOpen = false; }

        void WireSettings()
        {
            if (_settingsWired || !Hud || Hud.Root == null) return;
            _settings = gameObject.GetComponent<SettingsPanel>() ?? gameObject.AddComponent<SettingsPanel>();
            _settings.Bind(Hud, Hud.Root);
            Hud.OnSettingsRequested -= _settings.Toggle;
            Hud.OnSettingsRequested += _settings.Toggle;
            _settingsWired = true;
        }

        void Update() { if (!_settingsWired) WireSettings(); }

        void Refresh()
        {
            var gr = GameRoot.I; var s = gr.Eco.S;
            if (s.Wallet != lastWallet) { if (!double.IsNaN(lastWallet) && s.Wallet > lastWallet) Hud.PulseCurrency(); lastWallet = s.Wallet; Hud.SetCurrencies(s.Wallet, s.Bulloni); }
            Hud.ShowRoomLabel("Giorno 1");

            if (!toastShown) { toastShown = true; if (s.CassaLn >= 1) Hud.ShowOfflineToast(System.Math.Floor(s.CassaLn)); }

            if (s.CassaLn != lastCassa)
            {
                lastCassa = s.CassaLn;
                double cap = gr.Eco.Cfg.CassaCapacity;
                if (CassaCard) CassaCard.SetAmount(System.Math.Floor(s.CassaLn), cap);
                if (EdgeArrow && Cassa)
                {
                    if (s.CassaLn >= 1 && (EdgeArrow.Target == null || EdgeArrow.Target == Cassa)) EdgeArrow.SetTarget(Cassa, "Cassa · " + HudFormat.Amount(System.Math.Floor(s.CassaLn)) + " LN", "icon-cash");
                    else if (s.CassaLn < 1 && EdgeArrow.Target == Cassa) EdgeArrow.Clear();
                }
            }

            var nina = gr.Nina;
            if (StackCounter && nina && nina.Stack)
            {
                int c = nina.Stack.Count, cap = nina.Stack.Capacity;
                if (c != lastCount || cap != lastCap) { lastCount = c; lastCap = cap; StackCounter.SetCount(c, cap); }
            }
        }
    }
}
