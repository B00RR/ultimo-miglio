using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI.Data;

namespace UltimoMiglio.UI
{
    /// <summary>Data for the contextual Cassaforte (debt) card. All numbers come from the economy/DayClock, never from the UI.</summary>
    public struct SafeCardData
    {
        public int chapter;
        public double trancheTotal, tranchePaid;
        public int day, daysPerYear;
        public int installmentIndex;      // 1-based
        public double installmentAmount;  // LN
        public int installmentDueDay;
        public bool tollActive;
        public float tollFraction;        // e.g. 0.15 (from TollState / data)
    }

    /// <summary>
    /// Screen-space HUD (UI Toolkit). Permanent: currency pill (top-left) and phone button (top-right).
    /// Contextual: Pedaggio chip, room label, toast, Cassaforte card, collect popups.
    /// Pure view: game code pushes state in through the public API; user taps go out through events.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] float roomLabelSeconds = 1.5f;   // 04_UI §1.2 (UX timing, not balance)
        [SerializeField] float toastSeconds = 2.5f;       // 04_UI §3.4

        public event Action PhoneButtonTapped;
        /// <summary>Tap on the Pedaggio chip. Expected handler: open safe card + point arrow at Cassaforte.</summary>
        public event Action PedaggioTapped;
        public event Action SafeCardClosed;

        /// <summary>Set by the bridge (ChapterDataHudConfig). Optional: every API also takes explicit values.</summary>
        public IHudConfig Config { get; set; }
        public VisualElement Root { get; private set; }
        public VisualElement SafeRoot { get; private set; }

        Label lnLabel, boltLabel, tokenLabel, tollLabel, roomLabel, toastLabel, phoneBadge;
        VisualElement currencyPill, tokenGroup, tollChip, roomPill, toast, phoneButton, safeCard, popupLayer;
        Coroutine roomCo, toastCo;
        Rect lastSafeArea;
        Vector2Int lastScreen;

        bool bound;

        void OnEnable() { bound = false; TryBind(); }

        // UIDocument builds its tree in its own OnEnable; order vs this component is not guaranteed
        // (WebGL showed every contextual panel visible at once). Retry until the tree exists.
        bool TryBind()
        {
            if (bound) return true;
            var doc = GetComponent<UIDocument>();
            Root = doc ? doc.rootVisualElement : null;
            if (Root == null || Root.Q("currency-pill") == null) return false;
            SafeRoot = Root.Q("safe-root") ?? Root;
            currencyPill = Root.Q("currency-pill");
            lnLabel = Root.Q<Label>("ln-value");
            boltLabel = Root.Q<Label>("bolt-value");
            tokenGroup = Root.Q("token-group");
            tokenLabel = Root.Q<Label>("token-value");
            tollChip = Root.Q("toll-chip");
            tollLabel = Root.Q<Label>("toll-value");
            phoneButton = Root.Q("phone-button");
            phoneBadge = Root.Q<Label>("phone-badge");
            roomPill = Root.Q("room-pill");
            roomLabel = Root.Q<Label>("room-name");
            toast = Root.Q("toast");
            toastLabel = Root.Q<Label>("toast-text");
            safeCard = Root.Q("safe-card");
            popupLayer = Root.Q("popup-layer");

            phoneButton?.RegisterCallback<ClickEvent>(_ => PhoneButtonTapped?.Invoke());
            tollChip?.RegisterCallback<ClickEvent>(_ => PedaggioTapped?.Invoke());
            safeCard?.Q("safe-close")?.RegisterCallback<ClickEvent>(_ => HideSafeCard());

            SetVisible(tollChip, false);
            SetVisible(roomPill, false); SetVisible(toast, false); SetVisible(safeCard, false);
            SetVisible(phoneButton, true);
            SetVisible(Root.Q("phone-key-hint"), !Application.isMobilePlatform); // desktop/WebGL: "TAB" hint
            SetPhoneBadge(cBadge);
            SetCurrencies(cLn, cBolt, cTok);
            Root.RegisterCallback<GeometryChangedEvent>(_ => RefreshSafeArea(true));
            bound = true;
            RefreshSafeArea(true);
            Bound?.Invoke();
            return true;
        }

        /// <summary>Raised once the UXML tree is bound (APIs called earlier are no-ops).</summary>
        public event Action Bound;
        public bool IsBound => bound;

        void Update()
        {
            if (!TryBind()) return;
            EnsureAda(); MirrorTutorialBanner();
            if (Screen.safeArea != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
                RefreshSafeArea(false);
        }

        void RefreshSafeArea(bool force)
        {
            lastSafeArea = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height);
            SafeAreaUtil.ApplyPadding(SafeRoot);
            // HUD v2: room label 76 dp below the safe top (absolute offsets start at the padding-box edge)
            var tc = Root?.Q(className: "top-center");
            if (tc != null && SafeRoot != null) tc.style.top = SafeRoot.resolvedStyle.paddingTop + 76f;
        }

        // ---------------- Permanent overlay ----------------
        /// <summary>Gettoni are shown only when non-null (night, chapter 6+).</summary>
        double cLn, cBolt; double? cTok; int cBadge;
        public void SetCurrencies(double ln, double bulloni, double? gettoni = null)
        {
            cLn = ln; cBolt = bulloni; cTok = gettoni;
            if (lnLabel != null) lnLabel.text = HudFormat.Amount(ln);
            if (boltLabel != null) boltLabel.text = HudFormat.Amount(bulloni);
            SetVisible(tokenGroup, gettoni.HasValue);
            if (gettoni.HasValue && tokenLabel != null) tokenLabel.text = HudFormat.Amount(gettoni.Value);
        }

        public void PulseCurrency() => Pulse(currencyPill);
        /// <summary>Screen position (pixels, origin bottom-left) of the LN icon: coins fly here.</summary>
        public Vector2 CurrencyScreenPos()
        {
            if (currencyPill == null || currencyPill.panel == null) return new Vector2(80, Screen.height - 50);
            var root = currencyPill.panel.visualTree.layout; var wb = currencyPill.worldBound;
            if (root.width <= 0) return new Vector2(80, Screen.height - 50);
            float sx = Screen.width / root.width, sy = Screen.height / root.height;
            return new Vector2((wb.xMin + Mathf.Min(40, wb.width * 0.25f)) * sx, Screen.height - wb.center.y * sy);
        }

        public void SetPhoneVisible(bool visible) => SetVisible(phoneButton, visible);

        public void SetPhoneBadge(int count)
        {
            cBadge = count;
            if (phoneBadge == null) return;
            SetVisible(phoneBadge, count > 0);
            phoneBadge.text = count > 9 ? "9+" : count.ToString();
        }

        /// <summary>Ring animation on the phone icon (pair with a double haptic on the game side).</summary>
        public void RingPhone() { if (phoneButton != null) StartCoroutine(Shake(phoneButton)); }

        /// <summary>Screen rects (panel space) the joystick must ignore.</summary>
        public bool IsOverInteractive(Vector2 panelPos)
        {
            return Contains(currencyPill, panelPos) || Contains(phoneButton, panelPos) ||
                   Contains(tollChip, panelPos) || Contains(safeCard, panelPos);
        }

        // ---------------- Pedaggio ----------------
        /// <summary>Show the OmniFreight toll chip. fraction = toll share (0.15 → "-15%"); NaN → read from config.</summary>
        public void ShowPedaggio(float fraction = float.NaN) => ShowPedaggio(fraction, false);

        /// <summary>preview = onboarding step 10: chip shown greyed and not tappable, toll not active.</summary>
        public void ShowPedaggio(float fraction, bool preview)
        {
            if (tollChip != null) { tollChip.EnableInClassList("toll-chip--preview", preview); tollChip.pickingMode = preview ? PickingMode.Ignore : PickingMode.Position; }
            if (float.IsNaN(fraction) && Config != null) fraction = Config.TollFraction;
            if (tollLabel != null) tollLabel.text = float.IsNaN(fraction) ? "Pedaggio" : "-" + HudFormat.Percent(fraction);
            SetVisible(tollChip, true);
            Pulse(tollChip);
        }

        public void HidePedaggio(bool instant = false)
        {
            if (tollChip == null) return;
            if (instant || !tollChip.visible) { SetVisible(tollChip, false); return; }
            StartCoroutine(PopOut(tollChip));
        }

        // ---------------- Contextual ----------------
        public void ShowRoomLabel(string roomName)
        {
            if (roomLabel == null) return;
            roomLabel.text = FormatRoom(roomName);
            if (roomCo != null) StopCoroutine(roomCo);
            roomCo = StartCoroutine(ShowFor(roomPill, roomLabelSeconds));
        }

        /// <summary>"Bancone · 12" → "BANCONE · 12"; over 22 chars the part after " · " wraps to a 2nd line (never an ellipsis).</summary>
        public static string FormatRoom(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string t = name.ToUpperInvariant();
            int i = t.LastIndexOf(" · ", System.StringComparison.Ordinal);
            if (t.Length > 22 && i > 0) t = t.Substring(0, i) + "\n" + t.Substring(i + 3);
            return t;
        }

        // ---------------- Nonna Ada notes (HUD v2) ----------------
        AdaNoteView adaNote; ResponsivePanel responsive; VisualElement legacyBanner; string lastAdaText;
        /// <summary>One note at a time, queued (UI_SPEC_BIGLIETTI). sticky = stays until CompleteAdaNote().</summary>
        public void ShowAdaNote(string text, bool sticky = true, float seconds = 0f) { EnsureAda(); adaNote?.Show(text, sticky, seconds); lastAdaText = text; }
        public void EnqueueAdaNote(string text, float seconds) { EnsureAda(); adaNote?.Enqueue(text, seconds); }
        public void CompleteAdaNote() { adaNote?.Complete(); lastAdaText = null; }
        void EnsureAda() { if (adaNote == null && Root != null) { adaNote = AdaNoteView.Attach(Root); responsive = GetComponent<ResponsivePanel>(); } }

        /// <summary>TutorialManager (Gameplay) still builds an inline-styled "tutorial-banner" at the bottom: hide it and
        /// mirror its text into the v2 note (upper band). No Gameplay file changes needed.</summary>
        void MirrorTutorialBanner()
        {
            if (legacyBanner == null) legacyBanner = Root.Q("tutorial-banner");
            if (legacyBanner == null) { adaNote?.Tick(responsive ? responsive.Scale : 1f); return; }
            bool on = legacyBanner.resolvedStyle.display != DisplayStyle.None && legacyBanner.style.display != DisplayStyle.None;
            string txt = null;
            if (on) { var labels = legacyBanner.Query<Label>().ToList(); if (labels.Count > 0) txt = labels[labels.Count - 1].text; }
            legacyBanner.style.visibility = Visibility.Hidden; // keep its display logic, never draw it
            if (txt != lastAdaText)
            {
                if (string.IsNullOrEmpty(txt)) CompleteAdaNote();
                else { if (lastAdaText != null) adaNote?.Complete(); ShowAdaNote(txt); }
            }
            adaNote?.Tick(responsive ? responsive.Scale : 1f);
        }

        public void ShowToast(string text, float seconds = -1)
        {
            if (toastLabel == null) return;
            toastLabel.text = text;
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ShowFor(toast, seconds > 0 ? seconds : toastSeconds));
        }

        /// <summary>Offline return toast (04_UI §3.4): "La Cassa ha X LN".</summary>
        public void ShowOfflineToast(double lnInCassa) => ShowToast("La Cassa ha " + HudFormat.Amount(lnInCassa) + " LN");

        public void ShowSafeCard(SafeCardData d)
        {
            if (safeCard == null) return;
            SetVisible(safeCard.Q("safe-toll"), d.tollActive);
            SetText(safeCard, "safe-toll-text", "PEDAGGIO OMNIFREIGHT -" + HudFormat.Percent(d.tollFraction) + " · salda la rata per toglierlo");
            SetText(safeCard, "safe-title", "Cassaforte · Cap. " + d.chapter);
            SetText(safeCard, "safe-day", "Giorno " + d.day + "/" + (d.daysPerYear > 0 ? d.daysPerYear : 365));
            string amount = double.IsNaN(d.installmentAmount) ? "" : " · " + HudFormat.Amount(d.installmentAmount) + " LN";
            SetText(safeCard, "safe-installment", "Rata " + d.installmentIndex + amount + (d.installmentDueDay > 0 ? " · scade Giorno " + d.installmentDueDay : ""));
            float pct = d.trancheTotal > 0 ? Mathf.Clamp01((float)(d.tranchePaid / d.trancheTotal)) : 0f;
            SetText(safeCard, "safe-progress-text", HudFormat.Amount(d.tranchePaid) + " / " + HudFormat.Amount(d.trancheTotal) + " LN · " + HudFormat.Percent(pct));
            var fill = safeCard.Q("safe-progress-fill");
            if (fill != null) fill.style.width = Length.Percent(pct * 100f);
            SetVisible(safeCard, true);
        }

        public void HideSafeCard() { SetVisible(safeCard, false); SafeCardClosed?.Invoke(); }

        /// <summary>"+8,5 LN" popup at a screen position; adds the teal "-15%" tag when tollFraction &gt; 0.</summary>
        public void ShowCollectPopup(Vector2 screenPx, double netLn, float tollFraction = 0f)
        {
            if (popupLayer == null || popupLayer.panel == null) return;
            var row = new VisualElement(); row.AddToClassList("collect-popup");
            var amt = new Label("+" + HudFormat.Amount(netLn)); amt.AddToClassList("collect-amount");
            row.Add(amt);
            if (tollFraction > 0f) { var t = new Label("-" + HudFormat.Percent(tollFraction)); t.AddToClassList("toll-tag"); row.Add(t); }
            var p = SafeAreaUtil.ScreenToPanel(popupLayer.panel, screenPx);
            row.style.left = p.x; row.style.top = p.y;
            popupLayer.Add(row);
            StartCoroutine(FloatAndRemove(row));
        }

        // ---------------- helpers ----------------
        static void SetVisible(VisualElement e, bool v) { if (e != null) e.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }
        static void SetText(VisualElement root, string name, string text) { var l = root.Q<Label>(name); if (l != null) l.text = text; }
        static bool Contains(VisualElement e, Vector2 p) => e != null && e.resolvedStyle.display != DisplayStyle.None && e.worldBound.Contains(p);

        static void Pulse(VisualElement e)
        {
            if (e == null) return;
            e.RemoveFromClassList("pulse"); e.AddToClassList("pulse");
            e.schedule.Execute(() => e.RemoveFromClassList("pulse")).StartingIn(180);
        }

        IEnumerator ShowFor(VisualElement e, float seconds)
        {
            SetVisible(e, true); e.RemoveFromClassList("faded");
            yield return new WaitForSecondsRealtime(seconds);
            e.AddToClassList("faded");               // USS transition on opacity
            yield return new WaitForSecondsRealtime(0.3f);
            SetVisible(e, false); e.RemoveFromClassList("faded");
        }

        IEnumerator PopOut(VisualElement e)
        {
            e.AddToClassList("pop-out");
            yield return new WaitForSecondsRealtime(0.25f);
            SetVisible(e, false); e.RemoveFromClassList("pop-out");
        }

        IEnumerator Shake(VisualElement e)
        {
            for (float t = 0; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                e.style.rotate = new Rotate(new Angle(Mathf.Sin(t * 60f) * 12f * (1f - t / 0.8f)));
                yield return null;
            }
            e.style.rotate = new Rotate(new Angle(0));
        }

        IEnumerator FloatAndRemove(VisualElement e)
        {
            for (float t = 0; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                e.style.translate = new Translate(0, -60f * t, 0);
                e.style.opacity = 1f - Mathf.Clamp01((t - 0.5f) / 0.4f);
                yield return null;
            }
            e.RemoveFromHierarchy();
        }
    }
}
