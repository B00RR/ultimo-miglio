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
    /// In the Editor mouse is simulated via TouchSimulation.
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
        public bool IsActive => fingerId >= 0;
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
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE // mouse drives the joystick on desktop browsers
            UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
#endif
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

        void OnDown(Finger f)
        {
            if (fingerId >= 0 || Excluded(f.screenPosition)) return; // first touch wins
            fingerId = f.index; basePx = f.screenPosition; Value = Vector2.zero;
            Draw(f.screenPosition);
        }

        void OnMove(Finger f)
        {
            if (f.index != fingerId) return;
            float dp = SafeAreaUtil.DpToPx, r = radiusDp * dp, dz = deadZoneDp * dp;
            Vector2 d = f.screenPosition - basePx;
            if (d.magnitude > r) { basePx = f.screenPosition - d.normalized * r; d = d.normalized * r; } // dragged base
            float m = d.magnitude;
            Value = m <= dz ? Vector2.zero : d / m * Mathf.Pow((m - dz) / (r - dz), responseExponent);
            Draw(f.screenPosition);
        }

        void OnUp(Finger f) { if (f.index == fingerId) ForceRelease(); }

        void ForceRelease()
        {
            if (fingerId < 0) return;
            fingerId = -1; Value = Vector2.zero; Released?.Invoke();
        }

        void Update()
        {
            if (baseEl == null && hud && hud.Root != null) { baseEl = hud.Root.Q("joystick-base"); knobEl = hud.Root.Q("joystick-knob"); }
            if (IsActive) Moved?.Invoke(Value);
            float target = IsActive ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, target, fadeSeconds > 0 ? Time.unscaledDeltaTime / fadeSeconds : 1f);
            if (baseEl != null) { baseEl.style.opacity = alpha; baseEl.style.display = alpha > 0.001f ? DisplayStyle.Flex : DisplayStyle.None; }
        }

        void Draw(Vector2 fingerPx)
        {
            if (baseEl == null || baseEl.panel == null) return;
            var p = SafeAreaUtil.ScreenToPanel(baseEl.panel, basePx);
            var k = SafeAreaUtil.ScreenToPanel(baseEl.panel, fingerPx);
            float w = baseEl.resolvedStyle.width;
            baseEl.style.left = p.x - w * 0.5f; baseEl.style.top = p.y - w * 0.5f;
            if (knobEl != null) knobEl.style.translate = new Translate(k.x - p.x, k.y - p.y, 0);
        }
    }
}
