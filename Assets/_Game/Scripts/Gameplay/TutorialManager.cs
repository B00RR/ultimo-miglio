using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Onboarding cap.1: one step at a time (Nonna Ada banner in the HUD + animated ground marker + edge arrow).
    /// Steps complete from game state (EconomyState / stack / shelves). Progress saved in PlayerPrefs "um_tut_step".</summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        public enum Step { Muoviti, Arrivi, Scaffali, Bancone, Monete, Zaino, Nonno, Fine, Chiuso }
        public const string PrefKey = "um_tut_step";
        public static TutorialManager I { get; private set; }
        public static System.Action<Step> StepChanged;

        public string Speaker = "Nonna Ada";
        [TextArea] public string[] Texts = new string[7]; [TextArea] public string EndText;
        public Transform Arrivi, Scaffale, Bancone, Monete, PadZaino, PadNonno;
        public float EndSeconds = 6f;

        public Step Current { get; private set; }
        Vector3 _startPos; int _shelfBase, _servedBase; double _walletBase; float _endT;
        Transform _marker, _markerArrow; VisualElement _banner; Label _bannerText;
        UltimoMiglio.UI.EdgeArrow _edge; UltimoMiglio.UI.HudController _hud;

        void Awake() { I = this; }

        void Start()
        {
            Current = (Step)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, (int)Step.Chiuso);
            _hud = FindAnyObjectByType<UltimoMiglio.UI.HudController>();
            _edge = FindAnyObjectByType<UltimoMiglio.UI.EdgeArrow>(FindObjectsInactive.Include);
            BuildMarker();
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
            _markerArrow = new GameObject("Arrow").transform; _markerArrow.SetParent(_marker, false);
            for (int s = -1; s <= 1; s += 2)
            {   // chevron pointing down made of two bars
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(_markerArrow, false); bar.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);
                bar.transform.localPosition = new Vector3(s * 0.17f, 0.2f, 0); bar.transform.localRotation = Quaternion.Euler(0, 0, s * 35f);
                bar.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        void EnsureBanner()
        {
            if (_banner != null || !_hud || _hud.Root == null) return;
            _banner = new VisualElement { name = "tutorial-banner", pickingMode = PickingMode.Ignore };
            var st = _banner.style;
            st.position = Position.Absolute; st.left = Length.Percent(50); st.top = 312; st.translate = new Translate(Length.Percent(-50), 0); // banda medio-alta: mai sopra joystick/bottoni
            st.width = Length.Percent(88); st.maxWidth = 1092; st.maxHeight = 364; st.overflow = Overflow.Hidden; st.flexDirection = FlexDirection.Row; st.alignItems = Align.Center;
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
            var who = new Label(Speaker) { pickingMode = PickingMode.Ignore }; who.style.color = new Color(0.78f, 0.33f, 0.24f); who.style.fontSize = 30; who.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bannerText = new Label { pickingMode = PickingMode.Ignore }; _bannerText.style.color = new Color(0.23f, 0.16f, 0.12f); _bannerText.style.fontSize = 44; _bannerText.style.whiteSpace = WhiteSpace.Normal;
            col.Add(who); col.Add(_bannerText); _banner.Add(face); _banner.Add(col);
            _hud.Root.Add(_banner);
        }

        public string CurrentText => Current < Step.Fine ? Texts[(int)Current] : Current == Step.Fine ? EndText : null;

        void Enter(Step s)
        {
            Current = s; PlayerPrefs.SetInt(PrefKey, (int)s); PlayerPrefs.Save();
            var eco = GameRoot.Ready ? GameRoot.I.Eco : null;
            if (GameRoot.I && GameRoot.I.Nina) _startPos = GameRoot.I.Nina.transform.position;
            _shelfBase = ShelfTotal(); _servedBase = eco?.S.CustomersServed ?? 0; _walletBase = eco?.S.Wallet ?? 0; _endT = 0;
            Debug.Log($"[Tutorial] step {(int)s} {s}: {CurrentText}");
            StepChanged?.Invoke(s);
            var t = Target();
            if (_edge) { if (t) _edge.SetTarget(t, s.ToString().ToUpper()); else _edge.Clear(); }
            if (_marker) _marker.gameObject.SetActive(t);
        }

        static int ShelfTotal() { int n = 0; foreach (var sh in FindObjectsByType<Shelf>(FindObjectsSortMode.None)) n += sh.Count; return n; }

        Transform Target() => Current switch
        {
            Step.Arrivi => Arrivi, Step.Scaffali => Scaffale, Step.Bancone => Bancone, Step.Monete => Monete,
            Step.Zaino => PadZaino, Step.Nonno => PadNonno, _ => null
        };

        void Update()
        {
            if (!GameRoot.Ready) return;
            EnsureBanner();
            if (_bannerText != null) { var txt = CurrentText; if (_bannerText.text != txt) _bannerText.text = txt ?? ""; if (_banner != null) _banner.style.display = txt == null ? DisplayStyle.None : DisplayStyle.Flex; }
            var t = Target();
            if (_marker && t) { _marker.position = new Vector3(t.position.x, 0, t.position.z); _markerArrow.localPosition = new Vector3(0, 1.4f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.4f, 0); var cam = Camera.main; if (cam) _markerArrow.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)); _marker.GetChild(0).localScale = new Vector3(1.4f, 0.01f, 1.4f) * (1f + 0.12f * Mathf.Sin(Time.time * 5f)); }
            var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina;
            bool done = Current switch
            {
                Step.Muoviti => nina && (nina.transform.position - _startPos).sqrMagnitude > 1.5f * 1.5f,
                Step.Arrivi => nina && nina.Stack.Count >= Mathf.Min(3, nina.Stack.Capacity),
                Step.Scaffali => ShelfTotal() > _shelfBase,
                Step.Bancone => eco.S.CustomersServed > _servedBase,
                Step.Monete => eco.S.Wallet > _walletBase + 0.01,
                Step.Zaino => eco.S.Zaino >= 1,
                Step.Nonno => eco.S.NonnoLevel >= 0,
                Step.Fine => (_endT += Time.deltaTime) > EndSeconds,
                _ => false
            };
            if (done) { if (_hud && Current < Step.Fine) _hud.PulseCurrency(); Enter(Current + 1); }
        }
    }
}
