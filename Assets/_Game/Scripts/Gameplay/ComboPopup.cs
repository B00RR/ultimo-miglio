using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Combo (DevOn + Game Designer, #94/#106 comboFeedback): niente contatore nell'HUD. Solo "+N" (N = clienti serviti di fila)
    /// ancorato sopra la testa di Kaia (2,3 m), pop di scala, sale di 40 dp e sparisce in 1,5 s (dissolvenza negli ultimi 0,5 s).
    /// Il moltiplicatore (x1,2…) è interno e non si mostra mai. Non intercetta input.
    /// Uso: ComboPopup.Show(3) → "+3"; chiamato da TutorialManager.NotifyCombo(clientiDiFila, moltiplicatore).</summary>
    public static class ComboPopup
    {
        public const float DurationSec = 1.5f, RiseDp = 40f, HeightM = 2.3f;
        static Label _cur;

        public static void Show(int clientiDiFila) => Show("+" + clientiDiFila);

        static void Show(string text)
        {
            var hud = Object.FindAnyObjectByType<HudController>();
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            if (!hud || hud.Root == null || !nina || string.IsNullOrEmpty(text)) return;
            _cur?.RemoveFromHierarchy();
            var layer = hud.Root.Q("world-layer") ?? hud.Root;
            var l = new Label(text) { name = "combo-popup", pickingMode = PickingMode.Ignore }; l.AddToClassList("combo-popup");
            layer.Add(l); _cur = l;
            float t0 = Time.unscaledTime; var tr = nina.transform;
            OnbLog.Write($"combo {text}");
            l.schedule.Execute(() =>
            {
                float t = Time.unscaledTime - t0; var cam = Camera.main;
                if (t >= DurationSec || !tr || !cam || l.panel == null) { l.RemoveFromHierarchy(); if (_cur == l) _cur = null; return; }
                Vector3 sp = cam.WorldToScreenPoint(tr.position + Vector3.up * HeightM);
                var p = SafeAreaUtil.ScreenToPanel(l.panel, sp);
                float k = t / DurationSec;
                l.style.left = p.x; l.style.top = p.y - RiseDp * (1f - (1f - k) * (1f - k));
                l.style.opacity = k < 0.67f ? 1f : 1f - (k - 0.67f) / 0.33f;
                float pop = t < 0.12f ? Mathf.Lerp(0.35f, 1.25f, t / 0.12f) : t < 0.28f ? Mathf.Lerp(1.25f, 1f, (t - 0.12f) / 0.16f) : 1f;
                l.style.scale = new Scale(new Vector2(pop, pop));
                l.style.rotate = new Rotate(new Angle(-6f * (1f - Mathf.Clamp01(t / 0.7f))));
                l.style.display = sp.z > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(16).Until(() => l.panel == null);
        }
    }
}
