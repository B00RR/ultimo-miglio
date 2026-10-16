using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;

namespace UltimoMiglio.UI.Bridge
{
    /// <summary>Impostazioni: zoom −/+, slider Musica/Suoni, SALVA → PlayerPrefs (cam_zoom_v3, vol_music, vol_sfx).
    /// Pausa il gioco (timeScale=0) mentre è aperto. Aperto dal bottone ingranaggio dell'HUD.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        public const string PrefMusic = AudioManager.PrefMusic, PrefSfx = AudioManager.PrefSfx;
        VisualElement _root, _panel; Label _zoomL; Slider _music, _sfx; HudController _hud;
        float _prevScale = 1f; bool _open;

        public void Bind(HudController hud, VisualElement hudRoot)
        {
            _hud = hud;
            _root = new VisualElement { name = "settings-root", pickingMode = PickingMode.Position };
            var rs = _root.style; rs.position = Position.Absolute; rs.left = 0; rs.top = 0; rs.right = 0; rs.bottom = 0;
            rs.alignItems = Align.Center; rs.justifyContent = Justify.Center; rs.backgroundColor = new Color(0.15f, 0.1f, 0.08f, 0.45f);
            _root.style.display = DisplayStyle.None;

            _panel = new VisualElement { name = "settings-panel" };
            var ps = _panel.style; ps.width = Length.Percent(88); ps.maxWidth = 420; ps.paddingTop = 28; ps.paddingBottom = 28; ps.paddingLeft = 28; ps.paddingRight = 28;
            ps.backgroundColor = new Color(1f, 0.96f, 0.88f, 0.97f); ps.borderTopLeftRadius = ps.borderTopRightRadius = ps.borderBottomLeftRadius = ps.borderBottomRightRadius = 28;
            ps.borderLeftWidth = ps.borderRightWidth = ps.borderTopWidth = ps.borderBottomWidth = 5; ps.borderLeftColor = ps.borderRightColor = ps.borderTopColor = ps.borderBottomColor = new Color(0.23f, 0.16f, 0.125f);

            var title = Lab("IMPOSTAZIONI", 28, true); title.style.unityTextAlign = TextAnchor.MiddleCenter; title.style.marginBottom = 18; _panel.Add(title);

            _panel.Add(Lab("Zoom", 18, true));
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, alignItems = Align.Center, marginBottom = 18 } };
            var minus = Btn("−", () => Zoom(-1.5f)); var plus = Btn("+", () => Zoom(1.5f));
            _zoomL = Lab("7.0", 22, true); _zoomL.style.minWidth = 80; _zoomL.style.unityTextAlign = TextAnchor.MiddleCenter; _zoomL.style.marginLeft = 12; _zoomL.style.marginRight = 12;
            row.Add(minus); row.Add(_zoomL); row.Add(plus); _panel.Add(row);

            _panel.Add(Lab("Musica", 18, true));
            _music = new Slider(0, 1) { value = PlayerPrefs.GetFloat(PrefMusic, 0.6f) }; StyleSlider(_music); _music.RegisterValueChangedCallback(e => SetMusic(e.newValue)); _panel.Add(_music);
            _panel.Add(Lab("Suoni", 18, true));
            _sfx = new Slider(0, 1) { value = PlayerPrefs.GetFloat(PrefSfx, 1f) }; StyleSlider(_sfx); _sfx.RegisterValueChangedCallback(e => SetSfx(e.newValue)); _sfx.RegisterCallback<PointerUpEvent>(_ => AudioManager.I?.Play(AudioManager.Sfx.UiTap)); // anteprima volume Suoni _panel.Add(_sfx);

            var save = Btn("SALVA", SaveAndClose); save.style.marginTop = 22; save.style.alignSelf = Align.Center; save.style.minWidth = 180; _panel.Add(save);
            _root.Add(_panel); hudRoot.Add(_root);
            ApplySavedVolumes();
        }

        static Label Lab(string t, float fs, bool bold) { var l = new Label(t); l.style.fontSize = fs; l.style.color = new Color(0.23f, 0.16f, 0.125f); if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold; return l; }
        Button Btn(string t, System.Action a)
        {
            var b = new Button(() => { AudioManager.I?.Play(AudioManager.Sfx.UiTap); a?.Invoke(); }) { text = t };
            var s = b.style; s.height = 56; s.minWidth = 56; s.fontSize = 28; s.unityFontStyleAndWeight = FontStyle.Bold;
            s.backgroundColor = new Color(0.957f, 0.769f, 0.188f); s.color = new Color(0.23f, 0.16f, 0.125f);
            s.borderTopLeftRadius = s.borderTopRightRadius = s.borderBottomLeftRadius = s.borderBottomRightRadius = 18;
            s.borderLeftWidth = s.borderRightWidth = s.borderTopWidth = s.borderBottomWidth = 4; s.borderLeftColor = s.borderRightColor = s.borderTopColor = s.borderBottomColor = new Color(0.23f, 0.16f, 0.125f);
            return b;
        }
        void StyleSlider(Slider s) { s.style.height = 36; s.style.marginBottom = 12; }

        public void Toggle() { if (_open) SaveAndClose(); else Open(); }
        public void Open()
        {
            if (_open) return; AudioManager.I?.Play(AudioManager.Sfx.UiTap); _open = true; _prevScale = Time.timeScale; Time.timeScale = 0f;
            _root.style.display = DisplayStyle.Flex; RefreshZoomLabel();
        }
        void SaveAndClose()
        {
            var cam = Object.FindFirstObjectByType<DepotCameraFit>();
            if (cam) cam.SaveZoom();
            PlayerPrefs.SetFloat(PrefMusic, _music.value); PlayerPrefs.SetFloat(PrefSfx, _sfx.value); PlayerPrefs.Save();
            _root.style.display = DisplayStyle.None; _open = false; Time.timeScale = _prevScale <= 0 ? 1f : _prevScale;
        }
        void Zoom(float d) { var cam = Object.FindFirstObjectByType<DepotCameraFit>(); if (cam) { cam.ZoomBy(d); RefreshZoomLabel(); } }
        void RefreshZoomLabel() { var cam = Object.FindFirstObjectByType<DepotCameraFit>(); if (cam && _zoomL != null) _zoomL.text = cam.OrthoSize.ToString("0.0"); }
        void SetMusic(float v) { var a = AudioManager.I; if (a) a.SetMusicVolume(v); }
        void SetSfx(float v) { var a = AudioManager.I; if (a) a.SetSfxVolume(v); }
        void ApplySavedVolumes() { SetMusic(PlayerPrefs.GetFloat(PrefMusic, 0.6f)); SetSfx(PlayerPrefs.GetFloat(PrefSfx, 1f)); }
    }
}
