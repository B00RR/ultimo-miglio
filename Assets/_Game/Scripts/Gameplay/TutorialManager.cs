using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Onboarding cap.1 a 16 passi (#101, onboarding.json della PR #106): un passo alla volta, Zaino prima di NONNO, passo combo.
    /// L'ordine e gli id arrivano da onboarding.json (Ch1Data.asset); i gate si valutano dallo stato di gioco. Progresso in PlayerPrefs "um_tut_step".
    /// Ogni passo scrive le righe [ONB] di docs/design/13_CAP1_EVENTI_ONBOARDING.md (#91).</summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        public enum Step { ColdOpen, Move, PickUp, Shelve, Serve, Coins, ZainoPad, Combo, NonnoIntro, HireNonno, NonnoConfirm, PadNuovi, CassaPass, DebtCard, WallPad, FreePlay, Chiuso,
            Fine = FreePlay }
        /// <summary>id dei passi in onboarding.json (PR #106), nello stesso ordine di <see cref="Step"/>.</summary>
        public static readonly string[] StepIds = { "coldOpen", "move", "pickUp", "shelve", "serve", "coins", "zainoPad", "combo", "nonnoIntro", "hireNonno", "nonnoConfirm", "padNuovi", "cassaPass", "debtCard", "wallPad", "freePlay" };
        public const int StepCount = 16;
        /// <summary>Passi senza escalation di aiuti (spec #91 §2).</summary>
        public static bool HasHints(Step s) => !(s == Step.ColdOpen || s == Step.NonnoIntro || s == Step.NonnoConfirm || s == Step.WallPad || s >= Step.FreePlay);
        public const string PrefKey = "um_tut_step";
        public static TutorialManager I { get; private set; }
        public static System.Action<Step> StepChanged;

        public string Speaker = "Nonna Ada";
        [TextArea] public string[] Texts = new string[StepCount]; [TextArea] public string EndText;
        public Transform Arrivi, Scaffale, Bancone, Monete, PadZaino, PadNonno;
        [Tooltip("Durate dei passi 'solo guardare' (coldOpen, nonnoIntro, wallPad), s")] public float ColdOpenSec = 2f, NonnoIntroSec = 2.5f, WallPadSec = 3f, NonnoConfirmMaxSec = 20f, ComboSoftGateSec = 40f;
        public float EndSeconds = 6f;
        [Tooltip("onboarding_testi.json (PR #62): passi per id + suggerimenti onb.*. Facoltativo: senza, valgono Texts/EndText.")]
        public TextAsset TestiJson;
        [Header("Onboarding graduale (#94)")]
        [Tooltip("Nuova partita: nasconde all'avvio gli elementi bloccati e li rivela passo per passo (HudReveal). Salvataggi esistenti o tutorial finito: tutto visibile.")]
        public bool RivelaGraduale = true;
        public const string PrefGraduale = "um_onb_graduale";
        [Tooltip("Aiuti a 10/20/40 s di inattività (OnboardingHints). La freccia a bordo schermo parte solo al livello 2.")]
        public bool AiutiProgressivi = true;
        [Tooltip("'Salta tutorial' compare dopo questo passo o alla prima combo (NotifyCombo)")]
        public Step SaltaDopo = Step.Combo;

        public Step Current { get; private set; }
        public string CurrentObjectiveText => Current < Step.Fine && (int)Current < Objectives.Length ? Objectives[(int)Current] : (Current == Step.Fine ? "Deposito aperto!" : "");
        Vector3 _startPos; int _shelfBase, _servedBase, _padBase; double _walletBase, _debtBase; float _endT, _stepStart; bool _cassaSeen;
        int _hintCount, _lvl3; float _nolnpAt = -99f;
        /// <summary>Passi letti da onboarding.json (null = asset non importato: valgono <see cref="StepIds"/>).</summary>
        public UltimoMiglio.Core.Data.OnboardingStep[] Steps { get; private set; }
        /// <summary>Tempo di gioco attivo (s) usato nelle righe [ONB]: non scorre in pausa/menu.</summary>
        public static double OnbTime => GameRoot.I && GameRoot.Ready ? GameRoot.I.ActiveTime : Time.timeSinceLevelLoad;
        public static string IdOf(Step s) => (int)s < StepIds.Length ? StepIds[(int)s] : "freePlay";
        /// <summary>#91: una riga [ONB] in console (formato OnbLog).</summary>
        public static void Onb(string type, Step s, params object[] extras)
        {
            int n = Mathf.Min((int)s, StepCount - 1);
            Debug.Log(OnbLog.Line(type, IdOf((Step)n), n, OnbTime, extras));
        }
        /// <summary>Chiamato da OnboardingHints a ogni livello di aiuto (1/2/3 = 10/20/40 s).</summary>
        public void NotifyHint(int lvl, float idle, string kind)
        {
            if (Current >= Step.Fine || !HasHints(Current)) return;
            _hintCount++; if (lvl >= 3) _lvl3++;
            Onb("hint", Current, "lvl", lvl, "idle", (double)idle, "kind", kind);
        }
        Transform _marker, _markerArrow; VisualElement _banner; Label _bannerText, _objective;
        static readonly string[] Objectives = { "Benvenuta al Deposito", "Muoviti", "Vai agli Arrivi", "Metti sullo scaffale", "Porta al Bancone", "Raccogli le monete", "Compra lo Zaino",
            "Servi di fila", "Ecco NONNO", "Assumi NONNO", "NONNO lavora", "Nuovi pad", "Passa dalla Cassa", "Versa il debito", "Prossimo obiettivo" };
        UltimoMiglio.UI.EdgeArrow _edge; UltimoMiglio.UI.HudController _hud;

        void Awake() { I = this; }

        void Start()
        {
            LoadTesti();
            LoadSteps();
            Current = (Step)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, (int)Step.Chiuso);
            _hud = FindAnyObjectByType<UltimoMiglio.UI.HudController>();
            _edge = FindAnyObjectByType<UltimoMiglio.UI.EdgeArrow>(FindObjectsInactive.Include);
            BuildMarker();
            if (AiutiProgressivi && !GetComponent<OnboardingHints>()) gameObject.AddComponent<OnboardingHints>();
            foreach (var nr in FindObjectsByType<NonnoRobot>(FindObjectsSortMode.None)) if (!nr.GetComponent<NonnoRateBadge>()) nr.gameObject.AddComponent<NonnoRateBadge>();
            RegisterWorldReveals();
            // Graduale solo per una partita NUOVA (nessun passo salvato) o iniziata già in graduale; salvataggi precedenti = tutto visibile
            bool nuova = !PlayerPrefs.HasKey(PrefKey);
            // #101: nelle nuove partite RivelaGraduale è sempre acceso (anche se la scena serializzava false); i salvataggi esistenti restano come sono
            if (nuova) { RivelaGraduale = true; PlayerPrefs.SetInt(PrefGraduale, 1); PlayerPrefs.Save(); }
            bool graduale = Current < Step.Fine && (nuova ? RivelaGraduale : PlayerPrefs.GetInt(PrefGraduale, 0) == 1);
            if (graduale) UltimoMiglio.UI.HudReveal.HideAll(LockedAtStart);
            for (var k = Step.ColdOpen; k < Current; k++) RevealFor(k); // ripresa da PlayerPrefs: ciò che è già stato rivelato resta visibile
            Enter(Current);
        }

        void BuildMarker()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(sh); mat.SetColor("_BaseColor", new Color(1f, 0.78f, 0.2f));
            _marker = new GameObject("TutorialMarker").transform;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(_marker, false); ring.transform.localScale = new Vector3(1.4f, 0.01f, 1.4f); ring.transform.localPosition = new Vector3(0, 0.04f, 0);
            ring.GetComponent<Renderer>().sharedMaterial = mat;
            _markerArrow = new GameObject("Arrow").transform; _markerArrow.SetParent(_marker, false); _markerArrow.gameObject.SetActive(false); // QA #44: no floating mid-screen waypoint
            for (int s = -1; s <= 1; s += 2)
            {   // chevron pointing down made of two bars
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(_markerArrow, false); bar.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);
                bar.transform.localPosition = new Vector3(s * 0.17f, 0.2f, 0); bar.transform.localRotation = Quaternion.Euler(0, 0, s * 35f);
                bar.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }


        void EnsureObjective()
        {
            if (_objective != null || !_hud || _hud.Root == null) return;
            _objective = _hud.Root.Q<Label>("tutorial-objective");
            if (_objective != null) return; // HUD v3 (#39)
            _objective = new Label { name = "tut-objective", pickingMode = PickingMode.Ignore };
            var st = _objective.style;
            st.position = Position.Absolute; st.left = Length.Percent(50); st.top = 8; st.translate = new Translate(Length.Percent(-50), 0);
            st.fontSize = 18; st.unityFontStyleAndWeight = FontStyle.Bold; st.color = new Color(0.23f, 0.16f, 0.12f);
            st.backgroundColor = new Color(1f, 0.96f, 0.88f, 0.92f);
            st.paddingLeft = 14; st.paddingRight = 14; st.paddingTop = 6; st.paddingBottom = 6;
            st.borderTopLeftRadius = st.borderTopRightRadius = st.borderBottomLeftRadius = st.borderBottomRightRadius = 14;
            st.borderLeftWidth = st.borderRightWidth = st.borderTopWidth = st.borderBottomWidth = 3;
            st.borderLeftColor = st.borderRightColor = st.borderTopColor = st.borderBottomColor = new Color(0.23f, 0.16f, 0.12f);
            st.unityTextAlign = TextAnchor.MiddleCenter; st.maxHeight = 40;
            _hud.Root.Add(_objective);
        }
        void EnsureBanner()
        {
            if (_banner != null || !_hud || _hud.Root == null) return;
            if (UseAdaNote) return; // issue tutorial-HUD: fumetto compatto AdaNoteView, niente banner grande inline
            _banner = new VisualElement { name = "tutorial-banner", pickingMode = PickingMode.Ignore };
            var st = _banner.style;
            st.position = Position.Absolute; st.left = Length.Percent(50); st.top = 56; st.translate = new Translate(Length.Percent(-50), 0); // B4: sotto obiettivo, leggibile
            st.width = Length.Percent(92); st.maxWidth = 900; st.maxHeight = 120; st.overflow = Overflow.Hidden; st.flexDirection = FlexDirection.Row; st.alignItems = Align.Center;
            st.paddingLeft = 22; st.paddingRight = 28; st.paddingTop = 14; st.paddingBottom = 14;
            st.backgroundColor = new Color(1f, 0.96f, 0.88f, 0.97f);
            st.borderTopLeftRadius = st.borderTopRightRadius = st.borderBottomLeftRadius = st.borderBottomRightRadius = 28;
            st.borderLeftWidth = st.borderRightWidth = st.borderTopWidth = st.borderBottomWidth = 4;
            st.borderLeftColor = st.borderRightColor = st.borderTopColor = st.borderBottomColor = new Color(0.27f, 0.18f, 0.12f);
            var face = new Label("ADA") { pickingMode = PickingMode.Ignore };
            var fs = face.style; fs.width = 80; fs.height = 80; fs.marginRight = 16; fs.backgroundColor = new Color(0.78f, 0.33f, 0.24f);
            fs.borderTopLeftRadius = fs.borderTopRightRadius = fs.borderBottomLeftRadius = fs.borderBottomRightRadius = 40; fs.color = Color.white;
            fs.unityTextAlign = TextAnchor.MiddleCenter; fs.fontSize = 24; fs.unityFontStyleAndWeight = FontStyle.Bold; fs.flexShrink = 0;
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.style.flexShrink = 1;
            var who = new Label(Speaker) { pickingMode = PickingMode.Ignore }; who.style.color = new Color(0.78f, 0.33f, 0.24f); who.style.fontSize = 16; who.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bannerText = new Label { pickingMode = PickingMode.Ignore }; _bannerText.style.color = new Color(0.23f, 0.16f, 0.12f); _bannerText.style.fontSize = 22; _bannerText.style.whiteSpace = WhiteSpace.Normal;
            col.Add(who); col.Add(_bannerText); _banner.Add(face); _banner.Add(col);
            _hud.Root.Add(_banner);
        }

        const bool UseAdaNote = true;
        static readonly System.Collections.Generic.Dictionary<string, Step> IdToStep = new()
        {   // id dei testi (onboarding_testi.json "step:<id>") → passo
            { "move", Step.Move }, { "pickup", Step.PickUp }, { "shelve", Step.Shelve }, { "serve", Step.Serve }, { "coins", Step.Coins },
            { "unlock_nonno", Step.HireNonno }, { "upgrade_zaino", Step.ZainoPad }
        };
        /// <summary>Testi serializzati nella scena col vecchio ordine a 7 passi (Muoviti, Arrivi, Scaffali, Bancone, Monete, Nonno, Zaino) → nuovi indici.</summary>
        static void MigrateTexts(string[] old, string[] nu)
        {
            var map = new[] { Step.Move, Step.PickUp, Step.Shelve, Step.Serve, Step.Coins, Step.HireNonno, Step.ZainoPad };
            for (int i = 0; i < old.Length && i < map.Length; i++) nu[(int)map[i]] = old[i];
        }
        void LoadSteps()
        {
            var d = DataLoader.LoadOnboarding();
            if (d?.steps == null || d.steps.Length == 0) { Debug.LogWarning("[Onb] onboarding.json non importato in Ch1Data.asset: uso l'ordine predefinito a 16 passi"); return; }
            if (d.steps.Length != StepCount) Debug.LogError($"[Onb] onboarding.json ha {d.steps.Length} passi, attesi {StepCount}");
            for (int i = 0; i < Mathf.Min(d.steps.Length, StepCount); i++)
                if (d.steps[i].id != StepIds[i]) Debug.LogError($"[Onb] passo {i}: id '{d.steps[i].id}' in onboarding.json, atteso '{StepIds[i]}'");
            Steps = d.steps;
        }
        readonly System.Collections.Generic.Dictionary<string, string> _hints = new();
        VisualElement _hint; Label _hintText; Step _adaShown = (Step)(-1);

        void LoadTesti()
        {
            if (Texts == null || Texts.Length < StepCount) { var t = new string[StepCount]; if (Texts != null) MigrateTexts(Texts, t); Texts = t; }
            if (!TestiJson) return;
            string j = TestiJson.text;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(j, "\\{\\s*\"id\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"text\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                if (IdToStep.TryGetValue(m.Groups[1].Value, out var st) && (int)st < Texts.Length) Texts[(int)st] = System.Text.RegularExpressions.Regex.Unescape(m.Groups[2].Value);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(j, "\"(onb\\.[a-zA-Z.]+)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                _hints[m.Groups[1].Value] = System.Text.RegularExpressions.Regex.Unescape(m.Groups[2].Value);
        }

        /// <summary>Suggerimento onb.* del passo (CTA/hint terracotta in alto nella fascia bassa). null = nessuno.</summary>
        public string CurrentHint()
        {
            string k = Current switch
            {
                Step.Move => "onb.move.card",
                Step.PickUp => GameRoot.I && GameRoot.I.Nina && GameRoot.I.Nina.Stack.Count >= GameRoot.I.Nina.Stack.Capacity ? "onb.pick.max" : null,
                Step.Shelve => "onb.deposit.hint", Step.Serve => "onb.cust.first", Step.Coins => "onb.coins.walk",
                Step.ZainoPad => "onb.zaino.hint", Step.Combo => "onb.combo", Step.NonnoIntro => "onb.nonno.intro", Step.HireNonno => "onb.nonno.cost",
                Step.NonnoConfirm => "onb.nonno.working", Step.PadNuovi => "onb.pads.new", Step.CassaPass => "onb.cassa.tip", Step.DebtCard => "onb.debt.adaExplain",
                Step.WallPad => "onb.wall.goal", _ => null
            };
            return k != null && _hints.TryGetValue(k, out var v) ? v : null;
        }

        void EnsureHint()
        {
            if (_hint != null || !_hud || _hud.Root == null) return;
            _hint = new VisualElement { name = "tut-hint", pickingMode = PickingMode.Ignore }; _hint.AddToClassList("tut-hint");
            var pill = new VisualElement { pickingMode = PickingMode.Ignore }; pill.AddToClassList("tut-hint__pill");
            var go = new VisualElement { pickingMode = PickingMode.Ignore }; go.AddToClassList("tut-hint__go");
            _hintText = new Label { pickingMode = PickingMode.Ignore }; _hintText.AddToClassList("tut-hint__text");
            pill.Add(go); pill.Add(_hintText); _hint.Add(pill);
            (_hud.SafeRoot ?? _hud.Root).Add(_hint);
        }

        // ---------------- #94: comparse graduali + Salta tutorial ----------------
        /// <summary>Sottoinsieme di onboarding.json → lockedAtStart presente nella slice. Per decisione di DevOn (#64/#94) niente telefono,
        /// niente barra debito (il debito si vede solo a fine giornata e nel fumetto di Ada), combo = ComboPopup nel mondo: nessun id HUD per questi.</summary>
        public static readonly string[] LockedAtStart = { "hud.pillolaValute", "zona.arrivi", "zona.scaffaleTerracotta", "zona.bancone", "pad.zaino", "pad.assumiNonno", "robot.nonno.attivo" };
        bool _uiRegistered, _comboSeen; VisualElement _skip;

        /// <summary>Reveal del passo: da onboarding.json (steps[].reveals) se importato, altrimenti la mappa predefinita. Gli id non presenti nella slice vengono ignorati da HudReveal.</summary>
        string[] RevealsOf(Step s)
        {
            if (Steps != null && (int)s < Steps.Length && Steps[(int)s].reveals != null) return Steps[(int)s].reveals;
            return s switch
            {
                Step.PickUp => new[] { "zona.arrivi" }, Step.Shelve => new[] { "zona.scaffaleTerracotta" }, Step.Serve => new[] { "zona.bancone" },
                Step.Coins => new[] { "hud.pillolaValute" }, Step.ZainoPad => new[] { "pad.zaino" }, Step.HireNonno => new[] { "pad.assumiNonno" },
                Step.NonnoConfirm => new[] { "robot.nonno.attivo" }, _ => System.Array.Empty<string>()
            };
        }
        void RevealFor(Step s) { if (s >= Step.Fine) UltimoMiglio.UI.HudReveal.ShowAll(); else foreach (var id in RevealsOf(s)) UltimoMiglio.UI.HudReveal.Show(id); }

        void RegisterWorldReveals()
        {
            void W(string id, Transform t) { if (t) UltimoMiglio.UI.HudReveal.RegisterWorld(id, t.gameObject); }
            W("zona.arrivi", Arrivi); W("zona.scaffaleTerracotta", Scaffale); W("zona.bancone", Bancone); W("pad.zaino", PadZaino); W("pad.assumiNonno", PadNonno);
            var nr = FindAnyObjectByType<NonnoRobot>(); if (nr) UltimoMiglio.UI.HudReveal.RegisterWorld("robot.nonno.attivo", nr.gameObject);
        }

        void EnsureUiReveals()
        {
            if (_uiRegistered || !_hud || _hud.Root == null) return;
            _uiRegistered = true;
            UltimoMiglio.UI.HudReveal.Register("hud.pillolaValute", _hud.Root.Q("currency-pill"));
        }

        /// <summary>Da chiamare quando il moltiplicatore combo supera la soglia del passo combo (onboarding.json: comboReached ≥ 1,2).</summary>
        public void NotifyCombo() => NotifyCombo(0f);

        /// <summary>Combo raggiunta (moltiplicatore, es. 1.2): popup "x1,2" sopra Nina (1,5 s) e, la prima volta, gate comboReached → "Salta tutorial".</summary>
        public void NotifyCombo(float multiplier)
        {
            ComboPopup.Show(multiplier > 0f ? "x" + multiplier.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("it-IT")) : "Combo!");
            if (!_comboSeen) { _comboSeen = true; Debug.Log($"[Onb] comboReached x{multiplier:0.0}"); }
        }

        void EnsureSkip()
        {
            if (_skip != null || !_hud || _hud.Root == null) return;
            var b = new Button(Skip) { name = "skip-tutorial", text = "Salta tutorial" }; b.AddToClassList("skip-tut");
            _skip = b; (_hud.SafeRoot ?? _hud.Root).Add(_skip);
        }

        /// <summary>Salta il tutorial: rivela tutto ciò che i passi saltati avrebbero rivelato e chiude il tutorial.</summary>
        public void Skip()
        {
            if (Current >= Step.Fine) return;
            Onb("skip", Current, "from", IdOf(Current));
            Onb("end", Step.FreePlay, "total", OnbTime, "hints", _hintCount, "lvl3", _lvl3);
            UltimoMiglio.UI.HudReveal.ShowAll(); CameraPeek.Cancel();
            if (_hud) _hud.CompleteAdaNote();
            Enter(Step.Chiuso);
        }

        public string CurrentText => Current < Step.Fine ? (Texts != null && (int)Current < Texts.Length ? Texts[(int)Current] : CurrentHintText()) : Current == Step.Fine ? EndText : null;
        string CurrentHintText() { var h = CurrentHint(); return string.IsNullOrEmpty(h) ? null : h; }

        void Enter(Step s)
        {
            Current = s; PlayerPrefs.SetInt(PrefKey, (int)s); PlayerPrefs.Save();
            var eco = GameRoot.Ready ? GameRoot.I.Eco : null;
            if (GameRoot.I && GameRoot.I.Nina) _startPos = GameRoot.I.Nina.transform.position;
            _shelfBase = ShelfTotal(); _servedBase = eco?.S.CustomersServed ?? 0; _walletBase = eco?.S.Wallet ?? 0; _endT = 0; _stepStart = Time.time;
            _debtBase = eco?.S.DebtPaid ?? 0; _padBase = eco == null ? 0 : eco.S.Scarpe + eco.S.Insegna; _cassaSeen = false;
            // QA #42: LN sufficienti per i pad del tutorial (Zaino prima, poi NONNO) senza farm
            if (eco != null && (s == Step.ZainoPad || s == Step.HireNonno))
            {
                double need = eco.Cost(s == Step.ZainoPad ? UpgradeKind.Zaino : UpgradeKind.NonnoHire);
                if (!double.IsInfinity(need) && eco.S.Wallet < need) eco.S.Wallet = need;
            }
            if (s == Step.Coins && eco != null && eco.S.Wallet < 4)
                eco.S.Wallet = System.Math.Max(eco.S.Wallet, 4);
            if (s < Step.Chiuso) Onb("start", s, "reveals", RevealsOf(s).Length);
            if (s == Step.FreePlay) Onb("end", s, "total", OnbTime, "hints", _hintCount, "lvl3", _lvl3);
            Debug.Log($"[Tutorial] step {(int)s} {s}: {CurrentText}");
            StepChanged?.Invoke(s);
            RevealFor(s);
            var t = Target();
            // #94: con gli aiuti progressivi la freccia a bordo schermo arriva solo dopo 20 s di inattività (OnboardingHints)
            if (_edge) { if (t && !OnboardingHints.Active) _edge.SetTarget(t, s.ToString().ToUpper()); else _edge.Clear(); }
            if (_marker) _marker.gameObject.SetActive(false); // QA v2: no world waypoint if edge arrow exists
        }

        static int ShelfTotal() { int n = 0; foreach (var sh in FindObjectsByType<Shelf>(FindObjectsSortMode.None)) n += sh.Count; return n; }

        public Transform CurrentTarget => Target();
        Transform Target() => Current switch
        {
            Step.PickUp => Arrivi, Step.Shelve => Scaffale, Step.Serve => Bancone, Step.Coins => Monete, Step.Combo => Bancone,
            Step.ZainoPad => PadZaino, Step.NonnoIntro => PadNonno, Step.HireNonno => PadNonno, Step.PadNuovi => CheapestNewPad(), Step.CassaPass => Cassa(), _ => null
        };

        void Update()
        {
            if (!GameRoot.Ready) return;
            EnsureBanner();
            EnsureObjective();
            if (_objective != null) { _objective.text = CurrentObjectiveText; _objective.style.display = Current <= Step.Fine ? DisplayStyle.Flex : DisplayStyle.None; _objective.BringToFront(); }
            // Fumetto Ada compatto (AdaNoteView): un biglietto per passo, chiuso a fine tutorial
            if (UseAdaNote && _hud && _adaShown != Current)
            {
                _adaShown = Current; var at = CurrentText;
                if (Current <= Step.Fine && !string.IsNullOrEmpty(at)) _hud.ShowAdaNote(at, sticky: Current < Step.Fine, seconds: EndSeconds);
                else _hud.CompleteAdaNote();
            }
            EnsureUiReveals();
            EnsureSkip();
            if (_skip != null)
            {
                bool skipOn = Current < Step.Fine && (_comboSeen || Current > SaltaDopo);
                _skip.EnableInClassList("skip-tut--on", skipOn);
                _hud.Root.EnableInClassList("hud--skip", skipOn); // stringe la riga obiettivo (Onboarding.uss)
            }
            EnsureHint();
            if (_hint != null) { var h = Current < Step.Fine ? CurrentHint() : null; var joy = _hud.Root.Q("joystick-base"); bool joyOver = joy != null && joy.resolvedStyle.display == DisplayStyle.Flex && joy.resolvedStyle.opacity > 0.05f && joy.worldBound.Overlaps(_hint.worldBound);
                _hint.EnableInClassList("tut-hint--on", !string.IsNullOrEmpty(h) && !joyOver); // mai sotto il pollice/joystick flottante
                if (_hintText != null && h != null) _hintText.text = h; }
            // B4 P2: testi Ada nel fumetto HUD (non solo console)
            if (_banner != null)
            {
                var txt = CurrentText;
                bool show = Current <= Step.Fine && !string.IsNullOrEmpty(txt);
                _banner.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show && _bannerText != null) _bannerText.text = txt;
            }
            var t = Target();
            if (_marker) _marker.gameObject.SetActive(false);
            var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina;
            float inStep = Time.time - _stepStart;
            if (Current == Step.CassaPass) { var cz = FindAnyObjectByType<CassaZone>(); if (cz && cz.Occupied) _cassaSeen = true; }
            string ev = Current switch
            {
                Step.ColdOpen => inStep >= ColdOpenSec ? "coldOpenEnd" : null,
                Step.Move => nina && (nina.transform.position - _startPos).sqrMagnitude > 1.5f * 1.5f ? "playerMoved" : null,
                Step.PickUp => nina && nina.Stack.Count >= 1 ? "parcelPicked" : null,
                Step.Shelve => ShelfTotal() > _shelfBase ? "shelfDeposit" : null,
                Step.Serve => eco.S.CustomersServed > _servedBase ? "customerServed" : null,
                // LN già accreditati su Bancone.Serve (CollectActive); monete a terra = Burst VFX
                Step.Coins => eco.S.Wallet > 0.01 ? "coinsCollected" : null,
                Step.ZainoPad => eco.S.Zaino >= 1 ? "padPurchased" : null,
                Step.Combo => _comboSeen ? "comboReached" : null,
                Step.NonnoIntro => inStep >= NonnoIntroSec ? "cameraDone" : null,
                Step.HireNonno => eco.S.NonnoLevel >= 0 ? "nonnoHired" : null,
                Step.NonnoConfirm => NonnoWorking() || inStep >= NonnoConfirmMaxSec ? "nonnoFirstRestock" : null,
                Step.PadNuovi => eco.S.Scarpe + eco.S.Insegna > _padBase || (double.IsInfinity(eco.Cost(UpgradeKind.Scarpe)) && double.IsInfinity(eco.Cost(UpgradeKind.Insegna))) ? "padPurchasedAny" : null,
                Step.CassaPass => _cassaSeen || !Cassa() ? "cassaPassed" : null,
                Step.DebtCard => eco.S.DebtPaid > _debtBase || eco.S.DebtPaid > 0 ? "firstDebtDeposit" : null,
                Step.WallPad => inStep >= WallPadSec ? "goalSeen" : null,
                Step.FreePlay => (_endT += Time.deltaTime) > EndSeconds ? "freePlayShown" : null,
                _ => null
            };
            // #91 softgate: il passo combo avanza dopo 40 s senza x1,2
            if (ev == null && Current == Step.Combo && inStep >= ComboSoftGateSec) { Onb("softgate", Current, "idle", (double)inStep); Enter(Current + 1); return; }
            if (ev == null) { CheckNoLn(eco); return; }
            if (Current < Step.FreePlay) Onb("gate", Current, "event", ev, "dur", (double)inStep);
            if (_hud && Current < Step.Fine) _hud.PulseCurrency();
            Enter(Current + 1);
        }

        void CheckNoLn(EconomyCore eco)
        {
            var k = Current == Step.ZainoPad ? UpgradeKind.Zaino : Current == Step.HireNonno ? UpgradeKind.NonnoHire : (UpgradeKind?)null;
            if (k == null || Time.time - _nolnpAt < 5f) return;
            var pad = (Current == Step.ZainoPad ? PadZaino : PadNonno)?.GetComponent<UpgradePad>();
            double cost = eco.Cost(k.Value);
            if (pad && pad.Occupied && eco.S.Wallet <= 0 && !double.IsInfinity(cost))
            { _nolnpAt = Time.time; Onb("nolnp", Current, "pad", Current == Step.ZainoPad ? "zaino" : "assumiNonno", "wallet", (int)eco.S.Wallet, "cost", (int)cost); }
        }
        static bool NonnoWorking()
        {
            foreach (var b in FindObjectsByType<NonnoRobot>(FindObjectsSortMode.None)) if (b.State != NonnoRobot.WorkState.Idle || b.CarryCount > 0) return true;
            return false;
        }
        Transform Cassa() { var c = FindAnyObjectByType<CassaZone>(); return c ? c.transform : null; }
        Transform CheapestNewPad()
        {
            Transform best = null; double bc = double.PositiveInfinity; var eco = GameRoot.I.Eco;
            foreach (var p in FindObjectsByType<UpgradePad>(FindObjectsSortMode.None))
                if ((p.Kind == UpgradeKind.Scarpe || p.Kind == UpgradeKind.Insegna) && eco.Cost(p.Kind) < bc) { bc = eco.Cost(p.Kind); best = p.transform; }
            return best;
        }
    }
}
