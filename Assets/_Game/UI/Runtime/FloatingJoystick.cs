using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Joystick flottante a tutto schermo (04_UI §1.1, 02_TECH §2b). EnhancedTouch, vince il primo touchId, unità dp,
    /// zona morta 8 dp, raggio 56 dp, base trascinata, dissolvenza 150 ms. Ignora i tocchi sugli overlay dell'HUD, sul
    /// telefono aperto e sulle strisce laterali dei gesti da 24 dp. Uscita: <see cref="Value"/> (spazio schermo, normalizzato) e
    /// <see cref="WorldDirection"/> (ruotato dallo yaw della telecamera, predefinito 45°). La grafica è disegnata nel documento dell'HUD.
    /// Desktop/WebGL: clic e trascinamento del mouse usano lo stesso percorso dello stick flottante; WASD/frecce alimentano lo stesso Value (senza grafica).
    /// #205: con due dita sullo schermo (pinch della telecamera) lo stick resta sospeso finché tutte le dita non vengono sollevate, così il pinch
    /// non muove mai Nina. WorldDirection segue lo yaw attuale della telecamera (rotazione del giocatore −45°..+45°): "su" è sempre il su dello schermo.
    /// </summary>
    public class FloatingJoystick : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] PhoneController phone;
        [SerializeField] Transform cameraTransform;           // sorgente dello yaw; se null usa fixedYaw
        [SerializeField] float fixedYaw = 45f;
        [SerializeField] float deadZoneDp = 8f, radiusDp = 56f, edgeExclusionDp = 24f, fadeSeconds = 0.15f;
        [SerializeField, Range(1f, 2f)] float responseExponent = 1.25f; // "leggermente esponenziale"

        public event Action<Vector2> Moved;   // a ogni frame mentre è attivo
        public event Action Released;

        public Vector2 Value { get; private set; }
        /// <summary>#205: true dal momento in cui si appoggia un secondo dito finché tutte le dita non vengono sollevate (pinch).</summary>
        public bool Suspended { get; private set; }
        /// <summary>Solo per i test: se impostato, sostituisce l'input da tocco/mouse/tastiera (nessuna allocazione: struct nullable).</summary>
        public Vector2? TestValue;
        public bool IsActive => fingerId != -1 || KeyboardActive;
        public Vector3 WorldDirection
        {
            get
            {
                float yaw = cameraTransform ? cameraTransform.eulerAngles.y : fixedYaw;
                return Quaternion.Euler(0, yaw, 0) * new Vector3(Value.x, 0, Value.y);
            }
        }

        int fingerId = -1;
        Vector2 basePx;
        VisualElement baseEl, knobEl;
        float alpha;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            // Desktop/WebGL: clic e trascinamento del mouse e WASD/frecce sono letti direttamente in Update (niente TouchSimulation,
            // che conterebbe due volte gli eventi mouse emulati dal browser sui dispositivi touch).
            ETouch.onFingerDown += OnDown;
            ETouch.onFingerMove += OnMove;
            ETouch.onFingerUp += OnUp;
            if (phone) phone.Opened += ForceRelease;
        }

        void OnDisable()
        {
            ETouch.onFingerDown -= OnDown;
            ETouch.onFingerMove -= OnMove;
            ETouch.onFingerUp -= OnUp;
            if (phone) phone.Opened -= ForceRelease;
            ForceRelease();
        }

        bool Excluded(Vector2 screenPx)
        {
            float edge = edgeExclusionDp * SafeAreaUtil.DpToPx;
            if (screenPx.x < edge || screenPx.x > Screen.width - edge) return true;
            if (phone && phone.IsOpen) return true;
            if (hud && hud.Root?.panel != null && hud.IsOverInteractive(SafeAreaUtil.ScreenToPanel(hud.Root.panel, screenPx))) return true;
            return false;
        }

        const int MouseId = -100;
        [SerializeField] bool keyboardInput = true, mouseInput = true;
        public bool KeyboardActive { get; private set; }

        void OnDown(Finger f)
        {
            if (ETouch.activeFingers.Count >= 2) { Suspend(); return; } // #205: pinch → nessun movimento
            if (Suspended || fingerId != -1 || Excluded(f.screenPosition)) return; // vince il primo tocco
            Begin(f.index, f.screenPosition);
        }

        void Begin(int id, Vector2 pos) { fingerId = id; basePx = pos; Value = Vector2.zero; Draw(pos); }

        void OnMove(Finger f) { if (f.index == fingerId) Track(f.screenPosition); }

        void Track(Vector2 screenPosition)
        {
            float dp = SafeAreaUtil.DpToPx, r = radiusDp * dp, dz = deadZoneDp * dp;
            Vector2 d = screenPosition - basePx;
            if (d.magnitude > r) { basePx = screenPosition - d.normalized * r; d = d.normalized * r; } // base trascinata
            float m = d.magnitude;
            Value = m <= dz ? Vector2.zero : d / m * Mathf.Pow((m - dz) / (r - dz), responseExponent);
            Draw(screenPosition);
        }

        void OnUp(Finger f) { if (f.index == fingerId) ForceRelease(); }

        void Suspend() { Suspended = true; ForceRelease(); }

        void ForceRelease()
        {
            if (fingerId == -1) return;
            fingerId = -1; Value = Vector2.zero; Released?.Invoke();
        }

        void Update()
        {
            if (baseEl == null && hud && hud.Root != null) { baseEl = hud.Root.Q("joystick-base"); knobEl = hud.Root.Q("joystick-knob"); }
            if (Suspended && ETouch.activeFingers.Count == 0) Suspended = false;
            if (TestValue.HasValue) { Value = TestValue.Value; KeyboardActive = Value != Vector2.zero; }
            else PollDesktop();
            if (IsActive) Moved?.Invoke(Value);
            if (baseEl == null) return;
            if (fingerId == -1)
                Dock();
            else
            {
                baseEl.style.opacity = 1f;
                baseEl.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>A riposo il joystick sta in basso a sinistra, margine 24 dp (direzione C). Trascinato, segue il dito.</summary>
        void Dock()
        {
            alpha = Mathf.MoveTowards(alpha, 1f, fadeSeconds > 0f ? Time.unscaledDeltaTime / fadeSeconds : 1f);
            baseEl.style.opacity = alpha;
            baseEl.style.display = DisplayStyle.Flex;
            baseEl.style.top = StyleKeyword.Null;
            baseEl.style.right = StyleKeyword.Null;
            baseEl.style.left = 24f;
            baseEl.style.bottom = 24f;
            if (knobEl != null) knobEl.style.translate = new Translate(0, 0, 0);
        }

        void PollDesktop()
        {
            bool phoneOpen = phone && phone.IsOpen;
            var mouse = mouseInput ? UnityEngine.InputSystem.Mouse.current : null;
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                if (fingerId == -1 && !phoneOpen && mouse.leftButton.wasPressedThisFrame && ETouch.activeTouches.Count == 0 && !Excluded(mp)) Begin(MouseId, mp);
                else if (fingerId == MouseId) { if (mouse.leftButton.isPressed && !phoneOpen) Track(mp); else ForceRelease(); }
            }
            KeyboardActive = false;
            var kb = keyboardInput ? UnityEngine.InputSystem.Keyboard.current : null;
            if (kb != null && fingerId == -1 && !phoneOpen)
            {
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                var v = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
                if (v.sqrMagnitude > 0f) { KeyboardActive = true; Value = v; }
                else if (Value != Vector2.zero) { Value = Vector2.zero; Released?.Invoke(); }
            }
        }

        void Draw(Vector2 fingerPx)
        {
            if (baseEl == null || baseEl.panel == null) return;
            var p = SafeAreaUtil.ScreenToPanel(baseEl.panel, basePx);
            var k = SafeAreaUtil.ScreenToPanel(baseEl.panel, fingerPx);
            float w = baseEl.resolvedStyle.width;
            baseEl.style.bottom = StyleKeyword.Null;
            baseEl.style.left = p.x - w * 0.5f; baseEl.style.top = p.y - w * 0.5f;
            if (knobEl != null) knobEl.style.translate = new Translate(k.x - p.x, k.y - p.y, 0);
        }
    }
}
