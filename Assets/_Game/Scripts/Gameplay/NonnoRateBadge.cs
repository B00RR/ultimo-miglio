using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Badge "6 pacchi/min" di NONNO (#94): pillola piccola (22 dp, testo 12 dp) ancorata 2,1 m sopra il robot,
    /// visibile solo con NONNO assunto e in camera. Valore = EconomyCore.NonnoRestockPpm (dati, mai scritto a mano).
    /// Non intercetta input. Stile: .nonno-badge in Onboarding.uss.</summary>
    [RequireComponent(typeof(NonnoRobot))]
    public sealed class NonnoRateBadge : MonoBehaviour
    {
        public float HeightM = 2.1f;
        VisualElement _el; Label _txt; HudController _hud; Camera _cam; double _shown = -1;

        void Ensure()
        {
            if (_el != null) return;
            if (!_hud) _hud = FindAnyObjectByType<HudController>(); if (!_hud || _hud.Root == null) return;
            var layer = _hud.Root.Q("world-layer") ?? _hud.Root;
            _el = new VisualElement { name = "nonno-badge", pickingMode = PickingMode.Ignore }; _el.AddToClassList("nonno-badge");
            var ic = new VisualElement { pickingMode = PickingMode.Ignore }; ic.AddToClassList("nonno-badge__icon");
            _txt = new Label { pickingMode = PickingMode.Ignore }; _txt.AddToClassList("nonno-badge__text");
            _el.Add(ic); _el.Add(_txt); layer.Add(_el);
        }

        void LateUpdate()
        {
            if (!GameRoot.Ready) return;
            Ensure(); if (_el == null) return;
            var eco = GameRoot.I.Eco; double ppm = eco.NonnoRestockPpm;
            if (!_cam) _cam = Camera.main;
            Vector3 sp = _cam ? _cam.WorldToScreenPoint(transform.position + Vector3.up * HeightM) : Vector3.back;
            bool show = ppm > 0 && sp.z > 0 && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height && !HudReveal.IsHidden("robot.nonno.attivo");
            _el.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show || _el.panel == null) return;
            if (ppm != _shown) { _shown = ppm; _txt.text = HudFormat.Amount(ppm) + " pacchi/min"; }
            var p = SafeAreaUtil.ScreenToPanel(_el.panel, sp);
            _el.style.left = p.x; _el.style.top = p.y;
        }

        void OnDestroy() { _el?.RemoveFromHierarchy(); }
    }
}
