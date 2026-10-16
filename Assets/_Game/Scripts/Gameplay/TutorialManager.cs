using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Onboarding cap.1: one step at a time (Nonna Ada banner in the HUD + animated ground marker + edge arrow).
    /// Steps complete from game state (EconomyState / stack / shelves). Progress saved in PlayerPrefs "um_tut_step".</summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        public enum Step { Muoviti, Arrivi, Scaffali, Bancone, Monete, Nonno, Zaino, Fine, Chiuso }
        public const string PrefKey = "um_tut_step";
        public static TutorialManager I { get; private set; }
        public static System.Action<Step> StepChanged;

        public string Speaker = "Nonna Ada";
        [TextArea] public string[] Texts = new string[7]; [TextArea] public string EndText;
        public Transform Arrivi, Scaffale, Bancone, Monete, PadZaino, PadNonno;
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
        public Step SaltaDopo = Step.Monete;

        public Step Current { get; private set; }
        public string CurrentObjectiveText => Current < Step.Fine && (int)Current < Objectives.Length ? Objectives[(int)Current] : (Current == Step.Fine ? "Deposito aperto!" : "");
        Vector3 _startPos; int _shelfBase, _servedBase; double _walletBase; float _endT;
        Transform _marker, _markerArrow; VisualElement _banner; Label _bannerText, _objective;
        static readonly string[] Objectives = { "Muoviti", "Vai agli Arrivi", "Metti sullo scaffale", "Porta al Bancone", "Raccogli le monete", "Assumi NONNO", "Compra lo Zaino" };
        UltimoMiglio.UI.EdgeArrow _edge; UltimoMiglio.UI.HudController _hud;

        void Awake() { I = this; }

        void Start()
        {
            LoadTesti();
            Current = (Step)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, (int)Step.Chiuso);
            _hud = FindAnyObjectByType<UltimoMiglio.UI.HudController>();
            _edge = FindAnyObjectByType<UltimoMiglio.UI.EdgeArrow>(FindObjectsInactive.Include);
            BuildMarker();
            if (AiutiProgressivi && !GetComponent<OnboardingHints>()) gameObject.AddComponent<OnboardingHints>();
            foreach (var nr in FindObjectsByType<NonnoRobot>(FindObjectsSortMode.None)) if (!nr.GetComponent<NonnoRateBadge>()) nr.gameObject.AddComponent<NonnoRateBadge>();
            RegisterWorldReveals();
            // Graduale solo per una partita NUOVA (nessun passo salvato) o iniziata già in graduale; salvataggi precedenti = tutto visibile
            bool nuova = !PlayerPrefs.HasKey(PrefKey);
            bool escluso = PlayerPrefs.HasKey(PrefGraduale) && PlayerPrefs.GetInt(PrefGraduale) == 0; // test/QA: um_onb_graduale = 0 → tutto visibile subito
            if (RivelaGraduale && nuova && !escluso) { PlayerPrefs.SetInt(PrefGraduale, 1); PlayerPrefs.Save(); }
            bool graduale = RivelaGraduale && !escluso && Current < Step.Fine && (nuova || PlayerPrefs.GetInt(PrefGraduale, 0) == 1);
            if (graduale) UltimoMiglio.UI.HudReveal.HideAll(LockedAtStart);
            for (var k = Step.Muoviti; k < Current; k++) RevealFor(k); // ripresa da PlayerPrefs: ciò che è già stato rivelato resta visibile
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
        {   // id PR #62 (move…) + id storici (muoviti…): l'ordine nel JSON non conta più
            { "move", Step.Muoviti }, { "pickup", Step.Arrivi }, { "shelve", Step.Scaffali }, { "serve", Step.Bancone }, { "coins", Step.Monete },
            { "unlock_nonno", Step.Nonno }, { "upgrade_zaino", Step.Zaino },
            { "muoviti", Step.Muoviti }, { "arrivi", Step.Arrivi }, { "scaffali", Step.Scaffali }, { "bancone", Step.Bancone }, { "monete", Step.Monete },
            { "nonno", Step.Nonno }, { "zaino", Step.Zaino }
        };
        readonly System.Collections.Generic.Dictionary<string, string> _hints = new();
        VisualElement _hint; Label _hintText; Step _adaShown = (Step)(-1);

        void LoadTesti()
        {
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
                Step.Muoviti => "onb.move.card",
                Step.Arrivi => GameRoot.I && GameRoot.I.Nina && GameRoot.I.Nina.Stack.Count >= GameRoot.I.Nina.Stack.Capacity ? "onb.pick.max" : null,
                Step.Scaffali => "onb.deposit.hint", Step.Bancone => "onb.cust.first", Step.Monete => "onb.coins.walk",
                Step.Zaino => "onb.zaino.hint", _ => null
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

        static string[] RevealsOf(Step s) => s switch
        {
            Step.Arrivi => new[] { "zona.arrivi" }, Step.Scaffali => new[] { "zona.scaffaleTerracotta" }, Step.Bancone => new[] { "zona.bancone" },
            Step.Monete => new[] { "hud.pillolaValute" }, Step.Nonno => new[] { "pad.assumiNonno", "robot.nonno.attivo" }, Step.Zaino => new[] { "pad.zaino" },
            _ => System.Array.Empty<string>()
        };
        static void RevealFor(Step s) { if (s >= Step.Fine) UltimoMiglio.UI.HudReveal.ShowAll(); else foreach (var id in RevealsOf(s)) UltimoMiglio.UI.HudReveal.Show(id); }

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
        /// <summary>Soglia del gate combo (onboarding.json: comboReached ≥ 1,2). Il moltiplicatore è interno: non si mostra mai.</summary>
        public const float ComboGate = 1.2f;

        /// <summary>Cliente servito in combo (Game Designer, #106): a schermo solo "+N" sopra Nina (N = clienti serviti di fila, 1,5 s).
        /// Il moltiplicatore serve solo per la soglia del passo combo (≥ 1,2 → gate comboReached → "Salta tutorial").</summary>
        public void NotifyCombo(int clientiDiFila, float moltiplicatore)
        {
            if (clientiDiFila >= 2) ComboPopup.Show(clientiDiFila); // il primo cliente non è ancora una combo
            if (!_comboSeen && moltiplicatore >= ComboGate - 1e-4f) { _comboSeen = true; UltimoMiglio.UI.OnbLog.Write("gate comboReached"); }
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
            UltimoMiglio.UI.OnbLog.Write($"skip at step={Current}");
            UltimoMiglio.UI.HudReveal.ShowAll(); CameraPeek.Cancel();
            if (_hud) _hud.CompleteAdaNote();
            Enter(Step.Chiuso);
        }

        public string CurrentText => Current < Step.Fine ? Texts[(int)Current] : Current == Step.Fine ? EndText : null;

        void Enter(Step s)
        {
            Current = s; PlayerPrefs.SetInt(PrefKey, (int)s); PlayerPrefs.Save();
            var eco = GameRoot.Ready ? GameRoot.I.Eco : null;
            if (GameRoot.I && GameRoot.I.Nina) _startPos = GameRoot.I.Nina.transform.position;
            _shelfBase = ShelfTotal(); _servedBase = eco?.S.CustomersServed ?? 0; _walletBase = eco?.S.Wallet ?? 0; _endT = 0;
            // QA #42: after Arrivi/Bancone path, enough LN to hire NONNO within ~90s tutorial
            if (s == Step.Nonno && eco != null && eco.S.NonnoLevel < 0)
            {
                double need = eco.Cost(UpgradeKind.NonnoHire);
                if (!double.IsInfinity(need) && eco.S.Wallet < need)
                    eco.S.Wallet = need;
            }
            if (s == Step.Monete && eco != null && eco.S.Wallet < 4)
                eco.S.Wallet = System.Math.Max(eco.S.Wallet, 4);
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
            Step.Arrivi => Arrivi, Step.Scaffali => Scaffale, Step.Bancone => Bancone, Step.Monete => Monete,
            Step.Zaino => PadZaino, Step.Nonno => PadNonno, _ => null
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
            bool done = Current switch
            {
                Step.Muoviti => nina && (nina.transform.position - _startPos).sqrMagnitude > 1.5f * 1.5f,
                Step.Arrivi => nina && nina.Stack.Count >= Mathf.Min(3, nina.Stack.Capacity),
                Step.Scaffali => ShelfTotal() > _shelfBase,
                Step.Bancone => eco.S.CustomersServed > _servedBase,
                // LN già accreditati su Bancone.Serve (CollectActive); monete a terra = Burst VFX
                Step.Monete => eco.S.Wallet > 0.01,
                Step.Zaino => eco.S.Zaino >= 1,
                Step.Nonno => eco.S.NonnoLevel >= 0,
                Step.Fine => (_endT += Time.deltaTime) > EndSeconds,
                _ => false
            };
            if (done) { if (_hud && Current < Step.Fine) _hud.PulseCurrency(); Enter(Current + 1); }
        }
    }
}
