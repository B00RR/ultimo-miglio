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
        [SerializeField] HudConfigSource configSource;
        [SerializeField] float roomLabelSeconds = 1.5f;   // 04_UI §1.2 (UX timing, not balance)
        [SerializeField] float toastSeconds = 2.5f;       // 04_UI §3.4

        public event Action PhoneButtonTapped;
        /// <summary>Tap on the Pedaggio chip. Expected handler: open safe card + point arrow at Cassaforte.</summary>
        public event Action PedaggioTapped;
        public event Action SafeCardClosed;

        public IHudConfig Config => configSource ? configSource.Config : null;
        public VisualElement Root { get; private set; }
        public VisualElement SafeRoot { get; private set; }

        Label lnLabel, boltLabel, tokenLabel, tollLabel, roomLabel, toastLabel, phoneBadge;
        VisualElement currencyPill, tokenGroup, tollChip, roomPill, toast, phoneButton, safeCard, popupLayer;
        Coroutine roomCo, toastCo;
        Rect lastSafeArea;
        Vector2Int lastScreen;

        void OnEnable()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;
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

            HidePedaggio(true);
            SetVisible(roomPill, false); SetVisible(toast, false); SetVisible(safeCard, false);
            SetPhoneBadge(0);
            Root.RegisterCallback<GeometryChangedEvent>(_ => RefreshSafeArea(true));
        }

        void Update()
        {
            if (Screen.safeArea != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
                RefreshSafeArea(false);
        }

        void RefreshSafeArea(bool force)
        {
            lastSafeArea = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height);
            SafeAreaUtil.ApplyPadding(SafeRoot);
        }

        // ---------------- Permanent overlay ----------------
        /// <summary>Gettoni are shown only when non-null (night, chapter 6+).</summary>
        public void SetCurrencies(double ln, double bulloni, double? gettoni = null)
        {
            if (lnLabel != null) lnLabel.text = HudFormat.Amount(ln);
            if (boltLabel != null) boltLabel.text = HudFormat.Amount(bulloni);
            SetVisible(tokenGroup, gettoni.HasValue);
            if (gettoni.HasValue && tokenLabel != null) tokenLabel.text = HudFormat.Amount(gettoni.Value);
        }

        public void PulseCurrency() => Pulse(currencyPill);

        public void SetPhoneVisible(bool visible) => SetVisible(phoneButton, visible);

        public void SetPhoneBadge(int count)
        {
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
        public void ShowPedaggio(float fraction = float.NaN)
        {
            if (float.IsNaN(fraction) && Config != null && !float.IsNaN(Config.TollMultiplier))
                fraction = 1f - Config.TollMultiplier;
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
            roomLabel.text = roomName;
            if (roomCo != null) StopCoroutine(roomCo);
            roomCo = StartCoroutine(ShowFor(roomPill, roomLabelSeconds));
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
