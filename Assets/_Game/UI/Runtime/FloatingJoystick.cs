using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Full-screen floating joystick (04_UI §1.1, 02_TECH §2b). EnhancedTouch, first touchId wins, dp units,
    /// dead zone 8 dp, radius 56 dp, dragged base, 150 ms fade. Ignores touches on HUD overlays, on the open
    /// phone and on the 24 dp side gesture strips. Output: <see cref="Value"/> (screen space, normalized) and
    /// <see cref="WorldDirection"/> (rotated by camera yaw, default 45°). Visuals are drawn in the HUD document.
    /// Desktop/WebGL: mouse click-drag uses the same floating-stick path; WASD/arrows feed the same Value (no visuals).
    /// </summary>
    public class FloatingJoystick : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] PhoneController phone;
        [SerializeField] Transform cameraTransform;           // yaw source; if null uses fixedYaw
        [SerializeField] float fixedYaw = 45f;
        [SerializeField] float deadZoneDp = 8f, radiusDp = 56f, edgeExclusionDp = 24f, fadeSeconds = 0.15f;
        [SerializeField, Range(1f, 2f)] float responseExponent = 1.25f; // "slightly exponential"

        public event Action<Vector2> Moved;   // per frame while active
        public event Action Released;

        public Vector2 Value { get; private set; }
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
            // Desktop/WebGL: mouse click-drag and WASD/arrows are read directly in Update (no TouchSimulation,
            // which would double-count browser-emulated mouse events on touch devices).
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
            if (fingerId != -1 || Excluded(f.screenPosition)) return; // first touch wins
            Begin(f.index, f.screenPosition);
        }

        void Begin(int id, Vector2 pos) { fingerId = id; basePx = pos; Value = Vector2.zero; Draw(pos); }

        void OnMove(Finger f) { if (f.index == fingerId) Track(f.screenPosition); }

        void Track(Vector2 screenPosition)
        {
            float dp = SafeAreaUtil.DpToPx, r = radiusDp * dp, dz = deadZoneDp * dp;
            Vector2 d = screenPosition - basePx;
            if (d.magnitude > r) { basePx = screenPosition - d.normalized * r; d = d.normalized * r; } // dragged base
            float m = d.magnitude;
            Value = m <= dz ? Vector2.zero : d / m * Mathf.Pow((m - dz) / (r - dz), responseExponent);
            Draw(screenPosition);
        }

        void OnUp(Finger f) { if (f.index == fingerId) ForceRelease(); }

        void ForceRelease()
        {
            if (fingerId == -1) return;
            fingerId = -1; Value = Vector2.zero; Released?.Invoke();
        }

        void Update()
        {
            if (baseEl == null && hud && hud.Root != null) { baseEl = hud.Root.Q("joystick-base"); knobEl = hud.Root.Q("joystick-knob"); }
            PollDesktop();
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
