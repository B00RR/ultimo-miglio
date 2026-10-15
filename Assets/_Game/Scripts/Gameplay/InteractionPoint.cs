using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    public enum PoiKind { Prendi, Consegna, Acquisto }

    /// <summary>My Little Universe-style interaction point: raised rounded ground pad with a thick ink rim that pulses
    /// (scale + emission), coloured by kind (prendi = blu, consegna = verde, acquisto = giallo), plus a floating billboard
    /// chip (icon + max 2 words, Fredoka) drawn in the HUD world layer. The current tutorial target gets a bigger,
    /// brighter pad and a vertical beacon; the others stay calm. Purely visual: gameplay stays in ProximityZone.</summary>
    public sealed class InteractionPoint : MonoBehaviour
    {
        public PoiKind Kind; public string Label; public Texture2D Icon; public Font LabelFont;
        public float Radius = 1.1f; public bool HasCard; public UpgradePad Pad; public UltimoMiglio.VFX.UM_PadFillRing Ring;
        public static readonly Color Blu = new(0.369f, 0.498f, 0.525f), Verde = new(0.56f, 0.79f, 0.66f), Giallo = new(0.957f, 0.769f, 0.188f), Ink = new(0.231f, 0.165f, 0.125f);

        Transform _rim, _fill, _beacon; Material _fillMat, _beaconMat; Color _col;
        VisualElement _chip; Label _text; UltimoMiglio.UI.HudController _hud; Camera _cam; TutorialManager _tut;
        bool _active; float _phase;

        public Color KindColor => Kind == PoiKind.Prendi ? Blu : Kind == PoiKind.Consegna ? Verde : Giallo;

        void Start()
        {
            _col = KindColor; _phase = Random.value * 6f;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            Material M(Color c) { var m = new Material(sh); m.SetColor("_BaseColor", c); return m; }
            _rim = Disc("POI_Rim", M(Ink), new Vector3(Radius * 2f + 0.25f, 0.06f, Radius * 2f + 0.25f), 0.04f);
            _fillMat = M(_col);
            _fill = Disc("POI_Fill", _fillMat, new Vector3(Radius * 2f, 0.07f, Radius * 2f), 0.06f);
            _beaconMat = M(Color.Lerp(_col, Color.white, 0.35f));
            _beacon = Disc("POI_Beacon", _beaconMat, new Vector3(0.18f, 2.2f, 0.18f), 2.2f);
            _beacon.gameObject.SetActive(false);
            _tut = FindAnyObjectByType<TutorialManager>();
        }

        Transform Disc(string n, Material m, Vector3 s, float y)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = n; Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0, y, 0); g.transform.localScale = s;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g.transform;
        }

        bool Available() => !Pad || (GameRoot.Ready && !double.IsInfinity(GameRoot.I.Eco.Cost(Pad.Kind)) && GameRoot.I.ActiveTime >= Pad.UnlockAtActiveSec);

        void EnsureChip()
        {
            if (_chip != null) return;
            if (!_hud) _hud = FindFirstObjectByType<UltimoMiglio.UI.HudController>();
            if (!_hud || _hud.Root == null) return;
            _cam = Camera.main;
            var layer = _hud.Root.Q("world-layer") ?? _hud.Root;
            _chip = new VisualElement { name = "poi-chip", pickingMode = PickingMode.Ignore };
            var s = _chip.style; s.position = Position.Absolute; s.flexDirection = FlexDirection.Row; s.alignItems = Align.Center;
            s.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
            s.paddingLeft = 14; s.paddingRight = 26; s.paddingTop = 8; s.paddingBottom = 8;
            s.backgroundColor = Kind == PoiKind.Acquisto ? Giallo : new Color(1f, 0.976f, 0.933f);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 999;
            s.borderLeftWidth = s.borderRightWidth = s.borderTopWidth = s.borderBottomWidth = 6;
            s.borderLeftColor = s.borderRightColor = s.borderTopColor = s.borderBottomColor = Ink;
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            var i = ic.style; i.width = 72; i.height = 72; i.borderTopLeftRadius = i.borderTopRightRadius = i.borderBottomLeftRadius = i.borderBottomRightRadius = 36;
            i.backgroundColor = Kind == PoiKind.Acquisto ? Ink : _col; i.alignItems = Align.Center; i.justifyContent = Justify.Center; i.marginRight = 12;
            var img = new VisualElement { pickingMode = PickingMode.Ignore }; img.style.width = 48; img.style.height = 48;
            if (Icon) { img.style.backgroundImage = Icon; img.style.unityBackgroundImageTintColor = new Color(1f, 0.976f, 0.933f); }
            ic.Add(img);
            _text = new Label(Label) { pickingMode = PickingMode.Ignore };
            var t = _text.style; t.fontSize = Kind == PoiKind.Acquisto ? 56 : 46; t.color = Ink; t.unityFontStyleAndWeight = FontStyle.Bold; t.whiteSpace = WhiteSpace.NoWrap;
            if (LabelFont) t.unityFontDefinition = FontDefinition.FromFont(LabelFont);
            _chip.Add(ic); _chip.Add(_text); layer.Add(_chip);
            var arrow = _hud.Root.Q("edge-arrow"); arrow?.BringToFront(); // the off-screen arrow is never under a chip/card
        }

        void LateUpdate()
        {
            if (!_fill) return;
            bool avail = Available();
            if (_rim.gameObject.activeSelf != avail) { _rim.gameObject.SetActive(avail); _fill.gameObject.SetActive(avail); }
            var target = _tut ? _tut.CurrentTarget : null;
            _active = avail && target && (target.position - transform.position).sqrMagnitude < 9f;
            _beacon.gameObject.SetActive(_active);
            float t = Time.time * (_active ? 4.5f : 2.2f) + _phase, k = 0.5f + 0.5f * Mathf.Sin(t);
            float big = _active ? 1.18f : 1f, amp = _active ? 0.08f : 0.035f;
            float sc = big * (1f + amp * k);
            _rim.localScale = new Vector3((Radius * 2f + 0.25f) * sc, 0.06f, (Radius * 2f + 0.25f) * sc);
            _fill.localScale = new Vector3(Radius * 2f * sc, 0.07f, Radius * 2f * sc);
            _fillMat.SetColor("_BaseColor", Color.Lerp(_col, Color.white, (_active ? 0.45f : 0.18f) * k)); // "emission" pulse
            if (_active) { _beacon.localScale = new Vector3(0.18f, 2.2f + 0.3f * k, 0.18f); _beaconMat.SetColor("_BaseColor", Color.Lerp(_col, Color.white, 0.3f + 0.4f * k)); }

            EnsureChip();
            if (_chip == null || !_cam) return;
            if (Pad && GameRoot.Ready && Kind == PoiKind.Acquisto) { var eco = GameRoot.I.Eco; double left = eco.Cost(Pad.Kind) - eco.S.PadPaid[(int)Pad.Kind]; if (!double.IsInfinity(left)) _text.text = Mathf.CeilToInt((float)left).ToString(); }
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            if (nina) UltimoMiglio.UI.World.WorldAnchoredElement.Focus = nina.transform;
            bool near = nina && (nina.transform.position - transform.position).sqrMagnitude < 9f;
            if (Ring && !Pad) Ring.SetProgress(near ? 1f : 0f);
            Vector3 sp = _cam.WorldToScreenPoint(transform.position + Vector3.up * (2.0f + 0.15f * Mathf.Sin(Time.time * 2.4f + _phase)));
            bool show = avail && !(HasCard && near) && sp.z > 0 && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
            _chip.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || _chip.panel == null) return;
            var p = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(_chip.panel, sp);
            _chip.style.left = p.x; _chip.style.top = p.y;
            _chip.style.scale = new Scale(Vector3.one * (_active ? 1.12f : 1f));
        }

        void OnDestroy() { _chip?.RemoveFromHierarchy(); }
    }
}
