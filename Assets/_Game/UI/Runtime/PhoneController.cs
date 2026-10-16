using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    public enum PhoneApp { Home, Ordini, Messaggi, Flotta, Quartiere, Bottega, Razzo }

    [Serializable] public struct PhoneHomeData { public int day, daysPerYear; public string time; public string installmentLine; public string notification; public string questLine; }
    [Serializable] public struct OrderItem { public string id, customer, title, reward; public float secondsLeft; public string comboText; }
    [Serializable] public struct ChatMessage { public bool fromNina; public string text; }
    [Serializable] public struct ChatThread { public string contact; public string accentClass; public List<ChatMessage> messages; public List<string> quickReplies; }
    [Serializable] public struct RobotStatus { public string name; public string state; public int level; public float parcelsPerMin; public float offlinePerMin; }
    [Serializable] public struct FleetData { public List<RobotStatus> robots; public double cassaAmount, cassaCapacity; public float cassaFullInSeconds; public string nextPadTitle, nextPadId; }

    /// <summary>
    /// Nina's phone overlay (UI Toolkit). Back stack: app → Home → closed. Android Back (Escape key in the
    /// Input System) pops the stack; when the phone is closed Back is NOT consumed (let the game ask to quit).
    /// The game is never paused; listen to Opened/Closed to stop Nina.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PhoneController : MonoBehaviour
    {
        public event Action Opened, Closed;
        public event Action<PhoneApp> AppShown;
        /// <summary>"Mostra la strada". Arg = order id (Ordini) or pad id (Flotta). Phone already closed.</summary>
        public event Action<PhoneApp, string> ShowTheWayRequested;
        public event Action<string, int> QuickReplyChosen; // contact, reply index

        public bool IsOpen { get; private set; }
        public PhoneApp Current => stack.Count > 0 ? stack.Peek() : PhoneApp.Home;

        readonly Stack<PhoneApp> stack = new Stack<PhoneApp>();
        readonly HashSet<PhoneApp> unlocked = new HashSet<PhoneApp> { PhoneApp.Home };
        readonly Dictionary<PhoneApp, int> lockChapter = new Dictionary<PhoneApp, int>
        { { PhoneApp.Quartiere, 2 }, { PhoneApp.Razzo, 12 } };
        readonly Dictionary<PhoneApp, int> badges = new Dictionary<PhoneApp, int>();

        VisualElement root, scrim, phone, header;
        Label title;
        string currentOrderId, nextPadId;

        bool bound;
        [SerializeField, Tooltip("Max share of the panel height used by the phone (pillarboxed 9:16 column)")] float maxHeightShare = 0.9f;
        [SerializeField] float maxWidthShare = 0.94f;

        void OnEnable() { bound = false; TryBind(); }

        bool TryBind()
        {
            if (bound) return true;
            var doc = GetComponent<UIDocument>();
            root = doc ? doc.rootVisualElement : null;
            if (root == null || root.Q("phone") == null) return false;
            scrim = root.Q("phone-scrim");
            phone = root.Q("phone");
            header = root.Q("phone-header");
            title = root.Q<Label>("phone-title");
            scrim?.RegisterCallback<PointerDownEvent>(e => { if (e.target == scrim) Close(); });
            root.Q("phone-back")?.RegisterCallback<ClickEvent>(_ => Back());
            foreach (PhoneApp app in Enum.GetValues(typeof(PhoneApp)))
            {
                var tile = root.Q("tile-" + app.ToString().ToLowerInvariant());
                var a = app;
                tile?.RegisterCallback<ClickEvent>(_ => { if (unlocked.Contains(a)) Push(a); });
            }
            root.Q("ordini-cta")?.RegisterCallback<ClickEvent>(_ => ShowTheWay(PhoneApp.Ordini, currentOrderId));
            root.Q("flotta-cta")?.RegisterCallback<ClickEvent>(_ => ShowTheWay(PhoneApp.Flotta, nextPadId));
            root.RegisterCallback<GeometryChangedEvent>(_ => LayoutPhone());
            SetDisplay(root.Q("phone-layer"), IsOpen);
            bound = true;
            RefreshTiles();
            LayoutPhone();
            return true;
        }

        /// <summary>Phone is authored at a fixed 900x1600 (9:16) inside #phone-frame; the frame is uniformly scaled
        /// to fit maxHeightShare of the panel height and maxWidthShare of its width → centred 9:16 pillarbox on
        /// landscape desktop, near full-bleed column on portrait phones. Text keeps its proportions.</summary>
        public const float PhoneW = 900f, PhoneH = 1600f;
        void LayoutPhone()
        {
            var frame = root?.Q("phone-frame");
            if (frame == null) return;
            float W = root.layout.width, H = root.layout.height;
            if (float.IsNaN(W) || W <= 0 || H <= 0) return;
            float k = Mathf.Min(H * maxHeightShare / PhoneH, W * maxWidthShare / PhoneW);
            frame.style.scale = new Scale(new Vector3(k, k, 1f));
        }

        void Update()
        {
            if (!TryBind()) return;
            // Android Back maps to Keyboard.escapeKey in the Input System. Desktop/WebGL: TAB toggles, ESC backs out.
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame) { if (IsOpen) Close(); else OpenPhone(); }
            else if (IsOpen && kb.escapeKey.wasPressedThisFrame) Back();
        }

        /// <summary>Returns true if the Back press was consumed (phone was open).</summary>
        public bool HandleBack() { if (!IsOpen) return false; Back(); return true; }

        public void OpenPhone(PhoneApp app = PhoneApp.Home)
        {
            if (!IsOpen)
            {
                IsOpen = true; stack.Clear(); stack.Push(PhoneApp.Home);
                if (!TryBind()) { IsOpen = false; return; }
                var layer = root.Q("phone-layer");
                SetDisplay(layer, true);
                phone?.RemoveFromClassList("phone--open");
                phone?.schedule.Execute(() => phone.AddToClassList("phone--open")).StartingIn(16); // spring via USS transition
                Opened?.Invoke();
            }
            if (app != PhoneApp.Home) Push(app); else Show(PhoneApp.Home);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false; stack.Clear();
            if (root == null) { Closed?.Invoke(); return; }
            phone?.RemoveFromClassList("phone--open");
            root.Q("phone-layer")?.schedule.Execute(() => { if (!IsOpen) SetDisplay(root.Q("phone-layer"), false); }).StartingIn(250);
            Closed?.Invoke();
        }

        public void Back()
        {
            if (!IsOpen) return;
            if (stack.Count <= 1) { Close(); return; }
            stack.Pop(); Show(stack.Peek());
        }

        void Push(PhoneApp app) { if (Current != app) stack.Push(app); Show(app); }

        void Show(PhoneApp app)
        {
            foreach (PhoneApp a in Enum.GetValues(typeof(PhoneApp)))
                SetDisplay(root.Q("screen-" + a.ToString().ToLowerInvariant()), a == app);
            if (title != null) title.text = app == PhoneApp.Home ? "" : app.ToString();
            SetDisplay(root.Q("phone-back"), true);
            SetBadge(app, 0);
            AppShown?.Invoke(app);
        }

        void ShowTheWay(PhoneApp from, string id) { Close(); ShowTheWayRequested?.Invoke(from, id); }

        // ---------------- unlocks & badges ----------------
        public void UnlockApp(PhoneApp app) { unlocked.Add(app); RefreshTiles(); }
        public bool IsUnlocked(PhoneApp app) => unlocked.Contains(app);
        public void SetLockChapter(PhoneApp app, int chapter) { lockChapter[app] = chapter; RefreshTiles(); }
        public void SetBadge(PhoneApp app, int count) { badges[app] = count; RefreshTiles(); }
        public int TotalBadges() { int n = 0; foreach (var kv in badges) n += kv.Value; return n; }

        void RefreshTiles()
        {
            if (root == null) return;
            foreach (PhoneApp a in Enum.GetValues(typeof(PhoneApp)))
            {
                if (a == PhoneApp.Home) continue;
                var tile = root.Q("tile-" + a.ToString().ToLowerInvariant());
                if (tile == null) continue;
                bool on = unlocked.Contains(a);
                tile.EnableInClassList("tile--locked", !on);
                var lockLbl = tile.Q<Label>(className: "tile-lock");
                if (lockLbl != null) lockLbl.text = !on && lockChapter.TryGetValue(a, out int ch) ? "Cap. " + ch : "";
                var badge = tile.Q<Label>(className: "badge");
                int b = badges.TryGetValue(a, out int v) ? v : 0;
                if (badge != null) { SetDisplay(badge, on && b > 0); badge.text = b.ToString(); }
            }
        }

        // ---------------- content ----------------
        public void SetHome(PhoneHomeData d)
        {
            SetText("home-day", "Giorno " + d.day + "/" + (d.daysPerYear > 0 ? d.daysPerYear : 365) + " · Solara");
            SetText("home-time", d.time);
            SetText("home-installment", d.installmentLine);
            SetDisplay(root.Q("home-installment"), !string.IsNullOrEmpty(d.installmentLine)); // #71: niente barre/righe vuote
            SetText("home-notification", d.notification);
            SetDisplay(root.Q("home-notification-card"), !string.IsNullOrEmpty(d.notification));
            SetText("home-quest", d.questLine);
            SetDisplay(root.Q("home-quest-card"), !string.IsNullOrEmpty(d.questLine)); // #71
        }

        public void SetOrders(IList<OrderItem> orders)
        {
            var list = root.Q("ordini-list"); if (list == null) return;
            list.Clear();
            currentOrderId = orders.Count > 0 ? orders[0].id : null;
            foreach (var o in orders)
            {
                var card = Card("order-card");
                card.Add(Lbl(o.customer, "order-customer"));
                card.Add(Lbl(o.title, "order-title"));
                var row = new VisualElement(); row.AddToClassList("row");
                row.Add(Lbl(HudFormat.Clock(o.secondsLeft), "chip"));
                row.Add(Lbl(o.reward, "chip chip--sun"));
                if (!string.IsNullOrEmpty(o.comboText)) row.Add(Lbl(o.comboText, "chip chip--mint"));
                card.Add(row);
                list.Add(card);
            }
            SetDisplay(root.Q("ordini-cta"), orders.Count > 0);
        }

        public void SetChat(ChatThread t)
        {
            var list = root.Q("messaggi-list"); if (list == null) return;
            list.Clear();
            SetText("messaggi-contact", t.contact);
            if (t.messages != null)
                foreach (var m in t.messages)
                {
                    var b = Lbl(m.text, m.fromNina ? "bubble bubble--nina" : "bubble bubble--them");
                    if (!m.fromNina && !string.IsNullOrEmpty(t.accentClass)) b.AddToClassList(t.accentClass);
                    list.Add(b);
                }
            var replies = root.Q("messaggi-replies"); if (replies == null) return;
            replies.Clear();
            if (t.quickReplies == null) return;
            for (int i = 0; i < t.quickReplies.Count; i++)
            {
                int idx = i; string contact = t.contact;
                var btn = new Button(() => QuickReplyChosen?.Invoke(contact, idx)) { text = t.quickReplies[i] };
                btn.AddToClassList("quick-reply");
                replies.Add(btn);
            }
        }

        /// <summary>Flotta is read-only: no purchase controls, only "Mostra la strada" to the next pad.</summary>
        public void SetFleet(FleetData f)
        {
            var list = root.Q("flotta-list"); if (list == null) return;
            list.Clear();
            if (f.robots != null)
                foreach (var r in f.robots)
                {
                    var card = Card("robot-card");
                    var row = new VisualElement(); row.AddToClassList("row");
                    row.Add(Lbl(r.name, "robot-name"));
                    row.Add(Lbl("Lv " + r.level, "chip chip--mint"));
                    row.Add(Lbl(r.state, "chip"));
                    card.Add(row);
                    card.Add(Lbl(HudFormat.Amount(r.parcelsPerMin) + " pacchi/min · offline " + HudFormat.Amount(r.offlinePerMin) + " LN/min", "robot-stats"));
                    list.Add(card);
                }
            string full = f.cassaAmount >= f.cassaCapacity ? "PIENA" : "piena tra " + HudFormat.Duration(f.cassaFullInSeconds);
            SetText("flotta-cassa", "Cassa " + HudFormat.Amount(f.cassaAmount) + "/" + HudFormat.Amount(f.cassaCapacity) + " LN · " + full);
            var fill = root.Q("flotta-cassa-fill");
            if (fill != null) fill.style.width = Length.Percent(f.cassaCapacity > 0 ? (float)(100.0 * Math.Min(1.0, f.cassaAmount / f.cassaCapacity)) : 0f);
            nextPadId = f.nextPadId;
            SetText("flotta-nextpad", string.IsNullOrEmpty(f.nextPadTitle) ? "" : "Prossimo pad consigliato: " + f.nextPadTitle);
            SetDisplay(root.Q("flotta-cta"), !string.IsNullOrEmpty(f.nextPadId));
        }

        /// <summary>Panel-space hit test so the joystick ignores touches on the phone.</summary>
        public bool IsOverPhone(Vector2 panelPos) => IsOpen; // whole screen belongs to the phone (scrim) while open

        // ---------------- helpers ----------------
        void SetText(string name, string text) { var l = root.Q<Label>(name); if (l != null) l.text = text ?? ""; }
        static void SetDisplay(VisualElement e, bool v) { if (e != null) e.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }
        static VisualElement Card(string cls) { var c = new VisualElement(); c.AddToClassList("phone-card"); c.AddToClassList(cls); return c; }
        static Label Lbl(string text, string classes)
        {
            var l = new Label(text);
            foreach (var c in classes.Split(' ')) l.AddToClassList(c);
            return l;
        }
    }
}
