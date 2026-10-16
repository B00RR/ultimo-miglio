using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Combo (decisione DevOn, #94): niente contatore nell'HUD. Un piccolo "+N" o "x1,2" ancorato al mondo 2,3 m sopra Nina,
    /// sale di 40 dp e sparisce in 1,5 s (dissolvenza negli ultimi 0,5 s). Testo 16 dp sole con bordo bruno. Non intercetta input.
    /// Uso: ComboPopup.Show("x1,2"); collegato a TutorialManager.NotifyCombo(). Un popup nuovo sostituisce quello in corso.</summary>
    public static class ComboPopup
    {
        public const float DurationSec = 1.5f, RiseDp = 40f, HeightM = 2.3f;
        static Label _cur;

        public static void Show(string text)
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            if (!hud || hud.Root == null || !nina || string.IsNullOrEmpty(text)) return;
            _cur?.RemoveFromHierarchy();
            var layer = hud.Root.Q("world-layer") ?? hud.Root;
            var l = new Label(text) { name = "combo-popup", pickingMode = PickingMode.Ignore }; l.AddToClassList("combo-popup");
            layer.Add(l); _cur = l;
            float t0 = Time.unscaledTime; var tr = nina.transform;
            Debug.Log($"[Onb] combo {text}");
            l.schedule.Execute(() =>
            {
                float t = Time.unscaledTime - t0; var cam = Camera.main;
                if (t >= DurationSec || !tr || !cam || l.panel == null) { l.RemoveFromHierarchy(); if (_cur == l) _cur = null; return; }
                Vector3 sp = cam.WorldToScreenPoint(tr.position + Vector3.up * HeightM);
                var p = SafeAreaUtil.ScreenToPanel(l.panel, sp);
                float k = t / DurationSec;
                l.style.left = p.x; l.style.top = p.y - RiseDp * (1f - (1f - k) * (1f - k));
                l.style.opacity = k < 0.67f ? 1f : 1f - (k - 0.67f) / 0.33f;
                l.style.display = sp.z > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(16).Until(() => l.panel == null);
        }
    }
}
