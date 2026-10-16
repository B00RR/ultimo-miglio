using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    public enum PoiKind { Prendi, Consegna, Acquisto }

    /// <summary>My Little Universe-style interaction point: flat inset ground ring (#187, tufo + tinted fill, no ink rim) that pulses
    /// (scale + emission), coloured by kind (prendi = blu, consegna = verde, acquisto = giallo), plus a floating billboard
    /// chip (icon + max 2 words, Fredoka) drawn in the HUD world layer. The current tutorial target gets a bigger,
    /// brighter pad and a vertical beacon; the others stay calm. Purely visual: gameplay stays in ProximityZone.</summary>
    public sealed class InteractionPoint : MonoBehaviour
    {
        public PoiKind Kind; public string Label; public Texture2D Icon; public Font LabelFont;
        public float Radius = 1.1f; public bool HasCard; public UpgradePad Pad; public UltimoMiglio.VFX.UM_PadFillRing Ring; public UltimoMiglio.VFX.UM_PadIcon PadIcon;
        // #69 (guida §2: niente blu/grigi freddi): Prendi = terracotta #C8633B, Consegna = menta #8FC9A8, Acquisto/upgrade = giallo sole #F4C430 (Concept, #69); bordo outline #3B2F2A.
        // Il nome "Blu" resta per compatibilità API, ma ora vale terracotta.
        public static readonly Color Blu = new(0.784f, 0.388f, 0.231f), Verde = new(0.561f, 0.788f, 0.659f), Giallo = new(0.957f, 0.769f, 0.188f), Ink = new(0.231f, 0.184f, 0.165f);
        static readonly Color Crema = new(0.953f, 0.890f, 0.765f); // #F3E3C3: schiarimenti caldi (mai verso il bianco/grigio freddo)

        // #187: visuale creata in edit time dallo SliceSceneBuilder e salvata in scena (anello zone_inset_ring + colonna faro).
        // Materiali condivisi (asset, uno per tipo); le tinte di stato/pulse passano da MaterialPropertyBlock: niente new Material / renderer.material a runtime.
        [SerializeField] Transform _ring; [SerializeField] Renderer _ringR; [SerializeField] Transform _beacon; [SerializeField] Renderer _beaconR;
        /// <summary>Indice della submesh di riempimento in zone_inset_ring.fbx (0 = anello tufo, 1 = disco tingibile).</summary>
        public const int FillSubmesh = 1;
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock _mpb, _beaconMpb; Color _col; Transform _fill;
        public Transform RingVisual => _ring;
        /// <summary>#187 (Game Designer): il bordo esterno dell'anello coincide con Radius = metà del lato minore del trigger.</summary>
        public const float VisualScale = 1f;

        /// <summary>Chiamato SOLO in edit time dallo scene builder: collega anello e faro già creati e salvati in scena.</summary>
        public void BindVisuals(Transform ring, Transform beacon)
        {
            _ring = ring; _ringR = ring ? ring.GetComponentInChildren<Renderer>() : null;
            _beacon = beacon; _beaconR = beacon ? beacon.GetComponent<Renderer>() : null;
        }
        VisualElement _chip, _chipCol, _chevron, _bar, _barFill; Label _text, _barLabel;
        /// <summary>#94: aiuto livello 1 (10 s fermo): il bersaglio pulsa più forte (pad a terra + pillola).</summary>
        public bool HintPulse { get; set; }
        /// <summary>#94: barra LN sempre visibile (es. aiuto 40 s su Zaino/NONNO), altrimenti solo con pagamento iniziato o Nina vicina.</summary>
        public bool ForceBar { get; set; } UltimoMiglio.UI.HudController _hud; Camera _cam; TutorialManager _tut;
        bool _active; float _phase;


        void HideFloatingChips()
        {
            // QA #41: no floating Prendi/Scaffale/Pacchi chips — ground pads only
            if (PadIcon) PadIcon.SetVisible(false);
            var pt = GetComponent<UltimoMiglio.UI.World.PadTag>();
            if (pt) { pt.enabled = false; Destroy(pt); }
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c.name == "PadTag" || c.name.StartsWith("PadIcon")) c.gameObject.SetActive(false);
            }
        }

        /// #69: il quadrato giallo arrotondato (UI ZoneRing) sotto il disco si leggeva come griglia di debug.
        /// Il disco del POI già mostra la zona: nascondo il quad del ProximityZone e quelli figli (la logica di Fill resta, solo invisibile).
        void HideLegacyZoneSquare()
        {
            var zone = GetComponent<ProximityZone>();
            if (zone && zone.Ring) { var r = zone.Ring.GetComponent<Renderer>(); if (r) r.enabled = false; }
            foreach (var zr in GetComponentsInChildren<UltimoMiglio.UI.ZoneFillRing>(true)) { var r = zr.GetComponent<Renderer>(); if (r) r.enabled = false; }
        }

        public Color KindColor => Kind == PoiKind.Prendi ? Blu : Kind == PoiKind.Consegna ? Verde : Giallo;

        void Start()
        {
            if (!Ring && !Pad && (Ring = GetComponentInChildren<UltimoMiglio.VFX.UM_PadFillRing>(true))) // ricollega se la scena ha perso il riferimento (anello rotto in batchmode)
            { Ring.playCompleteBurst = false; Ring.SetKind(Kind == PoiKind.Consegna ? UltimoMiglio.VFX.UM_PadFillRing.Kind.Delivery : UltimoMiglio.VFX.UM_PadFillRing.Kind.Pickup); } // come SliceSceneBuilder
            HideFloatingChips();
            HideLegacyZoneSquare();
            _col = KindColor; _phase = Random.value * 6f;
            _mpb = new MaterialPropertyBlock(); _beaconMpb = new MaterialPropertyBlock();
            _fill = _ring; // compat: LateUpdate esce se la scena non ha la visuale (es. test che aggiungono il componente a mano)
            if (_beacon) _beacon.gameObject.SetActive(false);
            _tut = FindAnyObjectByType<TutorialManager>();
            if (PadIcon) PadIcon.SetVisible(false);
            var pt = GetComponent<UltimoMiglio.UI.World.PadTag>(); if (pt) pt.enabled = false;
        }

        static Color Saturate(Color c, float k)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(sat * k), Mathf.Clamp01(v * 1.05f));
        }

        bool Available() => !Pad || (GameRoot.Ready && !double.IsInfinity(GameRoot.I.Eco.Cost(Pad.Kind)) && GameRoot.I.ActiveTime >= Pad.UnlockAtActiveSec);

        void EnsureChip()
        {
            if (_chip != null) return;
            var hideTag = GetComponent<UltimoMiglio.UI.World.PadTag>(); if (hideTag) hideTag.enabled = false;
            // B2 P2: solo pad acquisto mostrano il costo in LN (chip); zone Prendi/Consegna restano senza chip
            if (Kind != PoiKind.Acquisto || !Pad) return;
            if (!_hud) _hud = FindFirstObjectByType<UltimoMiglio.UI.HudController>();
            if (!_hud || _hud.Root == null) return;
            _cam = Camera.main;
            var layer = _hud.Root.Q("world-layer") ?? _hud.Root;
            _chip = new VisualElement { name = "poi-chip", pickingMode = PickingMode.Ignore };
            // Colonna: freccia ▼ terracotta SOPRA la pillola (mai sovrapposta al prezzo), centrata sullo stesso asse; ancora = fondo pillola
            _chipCol = new VisualElement { name = "poi-chip-col", pickingMode = PickingMode.Ignore }; _chipCol.AddToClassList("poi-chip-col");
            _chevron = new VisualElement { name = "poi-chevron", pickingMode = PickingMode.Ignore }; _chevron.AddToClassList("poi-chevron");
            var s = _chip.style; s.flexDirection = FlexDirection.Row; s.alignItems = Align.Center; s.alignSelf = Align.Center;
            s.paddingLeft = 8; s.paddingRight = 12; s.paddingTop = 4; s.paddingBottom = 4;
            s.backgroundColor = Kind == PoiKind.Acquisto ? Giallo : new Color(1f, 0.976f, 0.933f);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 999;
            s.borderLeftWidth = s.borderRightWidth = s.borderTopWidth = s.borderBottomWidth = 3; // bordo 3–4 px (GUIDA §10)
            s.borderLeftColor = s.borderRightColor = s.borderTopColor = s.borderBottomColor = Ink;
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            var i = ic.style; i.width = 28; i.height = 28; i.borderTopLeftRadius = i.borderTopRightRadius = i.borderBottomLeftRadius = i.borderBottomRightRadius = 36;
            i.backgroundColor = Kind == PoiKind.Acquisto ? Ink : _col; i.alignItems = Align.Center; i.justifyContent = Justify.Center; i.marginRight = 12;
            var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.width = 18; img.style.height = 18;
            if (Icon) { img.style.backgroundImage = Icon; img.style.unityBackgroundImageTintColor = new Color(1f, 0.976f, 0.933f); }
            ic.Add(img);
            _text = new Label(Label) { pickingMode = PickingMode.Ignore };
            var t = _text.style; t.fontSize = Kind == PoiKind.Acquisto ? 18 : 16; t.color = Ink; t.unityFontStyleAndWeight = FontStyle.Bold; t.whiteSpace = WhiteSpace.NoWrap;
            if (LabelFont) t.unityFontDefinition = FontDefinition.FromFont(LabelFont);
            _chip.Add(ic); _chip.Add(_text);
            // #94: barra LN "32/50" fra la freccia e la pillola: freccia ▼ → barra → pillola, stesso asse
            _bar = new VisualElement { name = "poi-bar", pickingMode = PickingMode.Ignore }; _bar.AddToClassList("poi-bar");
            var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("poi-bar__track");
            _barFill = new VisualElement { pickingMode = PickingMode.Ignore }; _barFill.AddToClassList("poi-bar__fill"); track.Add(_barFill);
            _barLabel = new Label { pickingMode = PickingMode.Ignore }; _barLabel.AddToClassList("poi-bar__label");
            _bar.Add(track); _bar.Add(_barLabel);
            _chipCol.Add(_chevron); _chipCol.Add(_bar); _chipCol.Add(_chip); layer.Add(_chipCol);
            SetPadOff(PadOff); // #240: la pillola creata dopo l'acquisto nasce già spenta
            var arrow = _hud.Root.Q("edge-arrow"); arrow?.BringToFront(); // the off-screen arrow is never under a chip/card
        }

        /// #240: pad spento dopo l'acquisto. Prezzo grigio (UM_PadOffStyle.Price) e freccia #9E8F7E rivolta verso l'esterno.
        public bool PadOff { get; private set; }
        public void SetPadOff(bool off)
        {
            PadOff = off;
            if (_text != null) _text.style.color = off ? UltimoMiglio.VFX.UM_PadOffStyle.Price : Ink;
            if (_chevron != null)
            {
                _chevron.style.unityBackgroundImageTintColor = off ? UltimoMiglio.VFX.UM_PadOffStyle.Arrow : Color.white;
                _chevron.EnableInClassList("poi-chevron--off", off);
            }
        }
        public Color PriceColor => PadOff ? UltimoMiglio.VFX.UM_PadOffStyle.Price : Ink;

        int _shownLeft = int.MinValue, _shownPaid = int.MinValue, _shownCost = int.MinValue;
        UltimoMiglio.UI.World.PadTag _tag; bool _tagLooked;

        void LateUpdate()
        {
            if (!_fill) return;
            bool avail = Available();
            if (_ring.gameObject.activeSelf != avail) _ring.gameObject.SetActive(avail);
            var target = _tut ? _tut.CurrentTarget : null;
            _active = avail && target && (target.position - transform.position).sqrMagnitude < 9f;
            bool beaconOn = _active && Kind != PoiKind.Acquisto && _beacon; // la colonna 3D attraversava la pillola prezzo: niente faro sui pad acquisto
            if (_beacon && _beacon.gameObject.activeSelf != beaconOn) _beacon.gameObject.SetActive(beaconOn);
            float t = Time.time * (_active ? 4.5f : 2.2f) + _phase, k = 0.5f + 0.5f * Mathf.Sin(t);
            // #187 (Game Designer): l'anello resta esattamente grande quanto il trigger, niente scala in pulsazione;
            // il pulse (anche quello forte dell'aiuto 10 s, #94) passa solo dalla tinta del disco.
            float amp = HintPulse ? 0.45f : _active ? 0.22f : 0.08f;
            float d = Radius * 2f * VisualScale; // mesh Ø 1 m a scala 1
            if (_ring.localScale.x != d) _ring.localScale = new Vector3(d, 1f, d);
            if (_ringR)
            {
                float baseA = Kind == PoiKind.Acquisto ? 0.92f : 0.78f;
                var fill = Color.Lerp(_col, Crema, amp * k);
                fill.a = baseA * (0.88f + 0.12f * k); // pulse leggibile
                _ringR.GetPropertyBlock(_mpb, FillSubmesh); _mpb.SetColor(BaseColorId, fill); _mpb.SetColor(ColorId, fill); _ringR.SetPropertyBlock(_mpb, FillSubmesh);
            }
            if (beaconOn)
            {
                _beacon.localScale = new Vector3(0.18f, 2.2f + 0.3f * k, 0.18f);
                if (_beaconR) { var bc = Color.Lerp(_col, Crema, 0.3f + 0.4f * k); _beaconMpb.SetColor(BaseColorId, bc); _beaconMpb.SetColor(ColorId, bc); _beaconR.SetPropertyBlock(_beaconMpb); }
            }

            EnsureChip();
            if (_chip == null || !_cam) return;
            bool barOn = false;
            if (Pad && GameRoot.Ready && Kind == PoiKind.Acquisto && _text != null)
            {
                var eco = GameRoot.I.Eco; double cost = eco.Cost(Pad.Kind), paid = eco.S.PadPaid[(int)Pad.Kind];
                if (!double.IsInfinity(cost))
                {
                    // #191: stringhe ricostruite solo quando cambiano i numeri (prima: allocazioni a ogni frame per pad → picchi GC su WebGL)
                    int left = Mathf.CeilToInt((float)Mathf.Max(0, (float)(cost - paid)));
                    if (left != _shownLeft) { _shownLeft = left; _text.text = left + " LN"; } // pillola = quanto manca
                    int pd = Mathf.FloorToInt((float)paid), c = Mathf.CeilToInt((float)cost);
                    if (pd != _shownPaid || c != _shownCost) { _shownPaid = pd; _shownCost = c; _barLabel.text = pd + "/" + c; }
                    _barFill.style.width = Length.Percent(c > 0 ? Mathf.Clamp01((float)(paid / cost)) * 100f : 0f);
                    var n = GameRoot.I.Nina; bool close = n && (n.transform.position - transform.position).sqrMagnitude < 9f;
                    barOn = ForceBar || paid > 0.01 || close;
                }
            }
            if (_bar != null) _bar.EnableInClassList("poi-bar--on", barOn);
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            bool near = nina && (nina.transform.position - transform.position).sqrMagnitude < 9f;
            if (Ring && !Pad) Ring.SetProgress(near ? 1f : 0f);
            if (PadIcon) { PadIcon.SetVisible(false); PadIcon.SetPlayerNear(near); }
            if (!_tagLooked) { _tagLooked = true; _tag = GetComponent<UltimoMiglio.UI.World.PadTag>(); } // #191: cache
            if (_tag && _tag.enabled) _tag.enabled = false;
            // Offset mondo: più basso se il pad è in alto nello schermo, così la pillola non finisce sotto l'obiettivo
            float bob = 0.12f * Mathf.Sin(Time.time * 2.4f + _phase);
            Vector3 world = transform.position + Vector3.up * (1.55f + bob);
            Vector3 sp = _cam.WorldToScreenPoint(world);
            if (sp.z > 0 && sp.y > Screen.height * 0.72f)
                sp = _cam.WorldToScreenPoint(transform.position + Vector3.up * (1.05f + bob * 0.5f));
            bool show = avail && !(HasCard && near) && sp.z > 0 && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
            _chipCol.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || _chip.panel == null) return;
            _chevron.EnableInClassList("poi-chevron--on", _active);
            _chipCol.EnableInClassList("poi-chip-col--pulse", HintPulse && Mathf.Repeat(Time.time, 0.8f) < 0.4f); // #94 aiuto 10 s // indica il pad SOLO quando è l'obiettivo del tutorial
            _chevron.style.translate = new Translate(0, 3f * Mathf.Sin(Time.time * 6f), 0);
            var p = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(_chip.panel, sp);
            // QA v5 P2: non sotto la riga obiettivo; QA v6 P3: non tagliata sui bordi SX/DX
            const float hudTopReserve = 206f; // fondo fumetto Ada (106 dp) + 8 + pillola 44 + barra LN 22 (#94) + freccia 26: la colonna non entra mai nella fascia alta HUD
            // Pad alto sullo schermo: niente clamp che stacca la pillola dal pad. La colonna si ribalta SOTTO il pad
            // (freccia ▲ verso il pad, pillola sotto), così indica sempre il pad e non entra nella fascia alta.
            bool flip = p.y < hudTopReserve;
            if (flip) { var pad = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(_chip.panel, _cam.WorldToScreenPoint(transform.position)); p.y = Mathf.Max(pad.y + 10f, hudTopReserve - 70f); }
            _chipCol.EnableInClassList("poi-chip-col--flip", flip);
            float panelW = _chip.panel.visualTree.worldBound.width;
            if (panelW < 1f) panelW = Screen.width;
            float halfChip = 70f; // pillola ~140px, anchor al centro (translate -50%)
            const float edgePad = 10f;
            if (p.x < halfChip + edgePad) p.x = halfChip + edgePad;
            if (p.x > panelW - halfChip - edgePad) p.x = panelW - halfChip - edgePad;
            _chipCol.style.left = p.x; _chipCol.style.top = p.y;
            _chip.style.scale = new Scale(Vector3.one * (_active ? 1.08f : 1f));
            // Chip sopra il world-layer ma sotto i controlli: porta in cima al world-layer
            _chipCol.BringToFront();
        }

        void OnDestroy() { _chipCol?.RemoveFromHierarchy(); }
    }
}
