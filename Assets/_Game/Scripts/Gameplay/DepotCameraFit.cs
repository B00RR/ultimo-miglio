using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UltimoMiglio.VFX;
using UltimoMiglio.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace UltimoMiglio.Gameplay
{
    /// <summary>
    /// Iso camera: target = Nina. Dead-zone solo in movimento; a idle ricentra in ~0.3 s (QA v6).
    /// Il clamp mappa non può mai spingere Nina fuori dal 70% centrale dello schermo.
    /// #205: zoom/rotazione/inclinazione del giocatore da <see cref="CameraView"/> (ortho = base × zoom, yaw = iso + rot,
    /// pitch = tilt), pinch a due dita (joystick sospeso), doppio tocco a due dita = zoom 1,0, reset in 0,3 s.
    /// Nessuna allocazione per frame.
    /// </summary>
    // #191: gira prima del CinemachineBrain (LateUpdate) così la vcam è già aggiornata nello stesso frame (niente ritardo di 1 frame/scatti)
    [DefaultExecutionOrder(-100)]
    public sealed class DepotCameraFit : MonoBehaviour
    {
        /// <summary>#205: chiave della vecchia preferenza di zoom (ortho), eliminata da CameraView.Load.</summary>
        public const string LegacyPrefZoom = CameraView.LegacyPrefZoom;
        public CinemachineCamera Cm; public Transform Nina;
        public float Margin = 1.0f, TopHud = 0.20f, BottomHud = 0.08f, MinOrtho = 4.2f, MaxOrtho = 18f;
        /// <summary>FollowSmoothMove in corsa; FollowSmoothIdle ricentra da fermi (~0.3 s). DeadZone solo se velocity > soglia.</summary>
        public float FollowSmoothMove = 14f, FollowSmoothIdle = 12f, DeadZone = 0.14f, MoveSpeedEps = 0.35f;
        public float DefaultPortraitOrtho = 4.5f, DefaultLandscapeOrtho = 4.8f;
        public float CameraDistance = UM_QualityTier.CameraDistance;
        /// <summary>Viewport band in cui Nina deve restare (0.15..0.85 = 70% centrale).</summary>
        public float FrameMin = 0.15f, FrameMax = 0.85f;
        readonly List<Vector3> _pts = new();
        int _w, _h; float _ortho, _fullOrtho; Vector3 _centreWorld; Quaternion _rot; Vector2 _mn, _mx;
        float _aimX, _aimY; bool _aimInit; Vector3 _prevNina; bool _prevNinaInit;
        // #205: vista applicata (ammorbidita) e base della scena
        float _baseYaw, _baseOrtho, _z = 1f, _r, _t = 50f, _appliedYaw = float.NaN, _appliedTilt = float.NaN;
        float _resFrom = -1f, _rz0, _rr0, _rt0; bool _portrait;
        // pinch / doppio tocco a due dita
        float _pinchD0, _pinchZ0, _twoStart = -1f, _twoMaxMove, _lastTwoTap = -10f; Vector2 _twoA0, _twoB0;
        const float TwoTapMaxSec = 0.3f, TwoTapGapSec = 0.45f, TwoTapMoveDp = 18f;

        // Compat: vecchio campo serializzato FollowSmooth → Move
        public float FollowSmooth { get => FollowSmoothMove; set => FollowSmoothMove = value; }

        /// <summary>Solo catture/test: risoluzione simulata (es. 1080x1920 portrait su una RenderTexture); 0 = Screen.</summary>
        public static int TestScreenW, TestScreenH;
        /// <summary>Solo test: verifica di frecce/anelli del tutorial con la camera ruotata (in gioco il tutorial blocca la rotazione).</summary>
        public static bool TestIgnoraTutorial;
        static int SW => TestScreenW > 0 ? TestScreenW : Screen.width;
        static int SH => TestScreenH > 0 ? TestScreenH : Screen.height;
        public float OrthoSize => _ortho;
        public float FullOrtho => _fullOrtho;
        /// <summary>Vista applicata in questo frame (dopo ammorbidimento, blocchi del tutorial e camere brevi).</summary>
        public float AppliedZoom => _z; public float AppliedRot => _r; public float AppliedTilt => _t; public float BaseYaw => _baseYaw;

        void OnEnable() { EnhancedTouchSupport.Enable(); }
        void OnDisable() { if (CameraView.Pinching) CameraView.EndPinch(); }

        void Start()
        {
            Collect(); _rot = Cm ? Cm.transform.rotation : Quaternion.identity;
            if (Cm) Cm.Follow = null;
            // QA v6: knobs forzati (scene serializzate non devono tenere il follow lento / clamp aggressivo)
            FollowSmoothMove = 14f; FollowSmoothIdle = 12f; DeadZone = 0.14f; MoveSpeedEps = 0.35f;
            FrameMin = 0.15f; FrameMax = 0.85f;
            MinOrtho = 3f; // #205: zoom 0,7 × 4,5 = 3,15
            // #205: base = vista della scena; i valori del giocatore si sommano/moltiplicano a questa
            var e = _rot.eulerAngles; _baseYaw = e.y;
            CameraView.SetDefaultTilt(e.x > 180f ? e.x - 360f : e.x);
            CameraView.Load();
            UpdateTutorialFlag();
            SnapView();
        }

        /// <summary>Applica subito i valori correnti di CameraView, senza ammorbidimento (test, catture, avvio).</summary>
        public void SnapView()
        {
            _resFrom = -1f; _z = TargetZoom(); _r = CameraView.EffectiveRot; _t = CameraView.EffectiveTilt;
            UpdateView(0f, true); RecomputeBounds(); _aimInit = false; Apply(true);
        }

        static float TargetZoom() => CameraPeek.Active ? CameraView.ZoomDefault : CameraView.EffectiveZoom; // le camere brevi ignorano le impostazioni

        static void UpdateTutorialFlag()
        {
            var tm = TutorialManager.I;
            CameraView.Tutorial = !TestIgnoraTutorial && tm && tm.Current < TutorialManager.Step.Fine;
        }

        void Update()
        {
            UpdateTutorialFlag();
            HandleTouches();
        }

        /// <summary>Pinch a due dita (zoom, rimbalzo ai limiti, salvataggio al rilascio) e doppio tocco a due dita (zoom 1,0).</summary>
        void HandleTouches()
        {
            if (!EnhancedTouchSupport.enabled) return;
            var touches = ETouch.activeTouches; // ReadOnlyArray: niente allocazioni
            int n = touches.Count;
            bool blocked = Time.timeScale <= 0f || CameraView.UiOpen; // pannello aperto
            if (n >= 2 && !blocked)
            {
                Vector2 a = touches[0].screenPosition, b = touches[1].screenPosition;
                float d = Mathf.Max(1f, Vector2.Distance(a, b));
                if (!CameraView.Pinching)
                {
                    CameraView.Pinching = true; _pinchD0 = d; _pinchZ0 = CameraView.Zoom;
                    _twoStart = Time.unscaledTime; _twoA0 = a; _twoB0 = b; _twoMaxMove = 0f;
                }
                else
                {
                    _twoMaxMove = Mathf.Max(_twoMaxMove, Vector2.Distance(a, _twoA0), Vector2.Distance(b, _twoB0));
                    if (_twoMaxMove > TwoTapMoveDp * SafeAreaUtil.DpToPx) CameraView.SetZoomPinch(_pinchZ0 * _pinchD0 / d); // dita che si allargano = più vicino
                }
                return;
            }
            if (CameraView.Pinching)
            {
                bool tap = _twoStart >= 0f && Time.unscaledTime - _twoStart <= TwoTapMaxSec && _twoMaxMove <= TwoTapMoveDp * SafeAreaUtil.DpToPx;
                CameraView.EndPinch(); _twoStart = -1f;
                if (tap)
                {
                    if (Time.unscaledTime - _lastTwoTap <= TwoTapGapSec) { CameraView.ResetZoom(); _lastTwoTap = -10f; }
                    else _lastTwoTap = Time.unscaledTime;
                }
            }
        }

        /// <summary>Ammorbidisce la vista applicata verso il bersaglio (0,15 s) o esegue la transizione di reset (0,3 s); ricalcola i limiti se ruota.</summary>
        void UpdateView(float dt, bool snap = false)
        {
            float tz = TargetZoom(), tr = CameraView.EffectiveRot, tt = CameraView.EffectiveTilt;
            float req = CameraView.ResetRequestedAt;
            if (!snap && req >= 0f && req > _resFrom && Time.unscaledTime - req < CameraView.ResetSec) { _resFrom = req; _rz0 = _z; _rr0 = _r; _rt0 = _t; }
            if (snap) { _z = tz; _r = tr; _t = tt; }
            else if (_resFrom >= 0f && Time.unscaledTime - _resFrom < CameraView.ResetSec)
            {
                float u = Mathf.Clamp01((Time.unscaledTime - _resFrom) / CameraView.ResetSec); u = u * u * (3f - 2f * u);
                _z = Mathf.Lerp(_rz0, tz, u); _r = Mathf.Lerp(_rr0, tr, u); _t = Mathf.Lerp(_rt0, tt, u);
            }
            else
            {
                float k = 1f - Mathf.Exp(-3f * dt / CameraView.PinchSmoothSec); // ~95% in 0,15 s
                _z = Mathf.Lerp(_z, tz, k); _r = Mathf.Lerp(_r, tr, k); _t = Mathf.Lerp(_t, tt, k);
                if (Mathf.Abs(_z - tz) < 1e-4f) _z = tz; if (Mathf.Abs(_r - tr) < 1e-3f) _r = tr; if (Mathf.Abs(_t - tt) < 1e-3f) _t = tt;
            }
            bool portrait = SH > SW;
            if (snap || portrait != _portrait || _baseOrtho <= 0f) { _portrait = portrait; _baseOrtho = portrait ? DefaultPortraitOrtho : DefaultLandscapeOrtho; }
            _ortho = Mathf.Clamp(_baseOrtho * _z, MinOrtho, MaxOrtho);
            float yaw = _baseYaw + _r;
            if (yaw != _appliedYaw || _t != _appliedTilt)
            {
                _appliedYaw = yaw; _appliedTilt = _t;
                _rot = Quaternion.Euler(_t, yaw, 0f);
                if (Cm) Cm.transform.rotation = _rot;
                var cam = Camera.main; if (cam) cam.transform.rotation = _rot; // il joystick legge lo yaw nello stesso frame
                RecomputeBounds();
            }
        }

        void Collect()
        {
            _pts.Clear();
            var tut = FindAnyObjectByType<TutorialManager>();
            if (tut) foreach (var t in new[] { tut.Arrivi, tut.Scaffale, tut.Bancone, tut.Monete, tut.PadZaino, tut.PadNonno }) if (t) { _pts.Add(t.position + new Vector3(-1.2f, 0, -1.2f)); _pts.Add(t.position + new Vector3(1.2f, 0, 1.2f)); }
            foreach (var z in FindObjectsByType<ProximityZone>(FindObjectsSortMode.None)) { _pts.Add(z.transform.position + new Vector3(-1.2f, 0, -1.2f)); _pts.Add(z.transform.position + new Vector3(1.2f, 0, 1.2f)); }
        }

        CinemachinePositionComposer _pc; bool _pcLooked;

        void LateUpdate()
        {
            UpdateView(Time.unscaledDeltaTime);
            if (SW != _w || SH != _h) { RecomputeBounds(); Apply(true); }
            else Apply(false);
        }

        void RecomputeBounds()
        {
            _w = SW; _h = SH;
            if (!Cm || _pts.Count == 0) return;
            var inv = Quaternion.Inverse(_rot);
            _mn = new Vector2(float.MaxValue, float.MaxValue); _mx = new Vector2(float.MinValue, float.MinValue);
            foreach (var p in _pts)
                for (int i = 0; i < 2; i++)
                {
                    var l = inv * (p + Vector3.up * (i * 1.6f));
                    _mn = Vector2.Min(_mn, new Vector2(l.x, l.y)); _mx = Vector2.Max(_mx, new Vector2(l.x, l.y));
                }
            _mn -= Vector2.one * Margin; _mx += Vector2.one * Margin;
            float aspect = Mathf.Max(0.2f, (float)_w / Mathf.Max(1, _h));
            float usable = 1f - TopHud - BottomHud;
            _fullOrtho = Mathf.Max(MinOrtho, (_mx.y - _mn.y) * 0.5f / usable, (_mx.x - _mn.x) * 0.5f / aspect);
            MaxOrtho = Mathf.Max(MaxOrtho, _fullOrtho);
            _centreWorld = _rot * new Vector3((_mn.x + _mx.x) * 0.5f, (_mn.y + _mx.y) * 0.5f, 0);
        }

        void Apply(bool snap = false)
        {
            if (!Cm) return;
            float aspect = Mathf.Max(0.2f, (float)_w / Mathf.Max(1, _h));
            var inv = Quaternion.Inverse(_rot);
            Vector3 ninaPos = Nina ? Nina.position : _centreWorld;
            bool peek = CameraPeek.Active; // #94: camera breve, il joystick resta attivo
            var local = inv * (peek ? CameraPeek.Blend(ninaPos) : ninaPos);
            float halfH = _ortho * (1f - TopHud - BottomHud), halfW = _ortho * aspect;
            float hudBias = _ortho * (TopHud - BottomHud) * 0.5f;

            // Velocità Nina (XZ) → dead-zone solo in movimento
            bool moving = false;
            if (Nina && _prevNinaInit)
            {
                float dt = Mathf.Max(1e-4f, Time.deltaTime); // #191: stessa base tempi di Nina (Update)
                var v = (ninaPos - _prevNina) / dt; v.y = 0f;
                moving = v.sqrMagnitude > MoveSpeedEps * MoveSpeedEps;
                if (peek) { if (moving) CameraPeek.NotifyInput(); moving = false; } // durante la camera breve niente dead-zone
            }
            if (Nina) { _prevNina = ninaPos; _prevNinaInit = true; }

            if (!_aimInit || snap)
            {
                _aimX = local.x; _aimY = local.y; _aimInit = true;
            }
            else if (!moving)
            {
                // Idle: target = Nina, ricentro entro ~0.3 s (niente dead-zone)
                _aimX = local.x; _aimY = local.y;
            }
            else
            {
                // In movimento: dead-zone centrata intorno all'aim
                float dzX = halfW * Mathf.Clamp01(DeadZone);
                float dzY = halfH * Mathf.Clamp01(DeadZone);
                float dx = local.x - _aimX, dy = local.y - _aimY;
                if (Mathf.Abs(dx) > dzX) _aimX = local.x - Mathf.Sign(dx) * dzX;
                if (Mathf.Abs(dy) > dzY) _aimY = local.y - Mathf.Sign(dy) * dzY;
            }

            // Clamp mappa allargato: metà viewport oltre i bordi così possiamo sempre centrare Nina
            // se è dentro l'area di gioco. Poi correggiamo per tenere Nina nel 70% centrale.
            float cx = Mathf.Clamp(_aimX, _mn.x - halfW * 0.85f, _mx.x + halfW * 0.85f);
            float cy = Mathf.Clamp(_aimY, _mn.y - halfH * 0.85f, _mx.y + halfH * 0.85f);
            cy += hudBias;

            // Garantisce Nina nel band FrameMin..FrameMax dello schermo (spazio camera locale ≈ viewport)
            // In ortho, offset mondo locale → shift viewport: δ/half ≈ 2*(0.5 - uv) circa sull'asse.
            // Vogliamo |local - look| / (2*half) ≤ max(FrameMax-0.5, 0.5-FrameMin) = 0.35 → |δ| ≤ 0.70*half
            float maxOffX = halfW * (FrameMax - FrameMin); // 0.70 * halfW
            float maxOffY = halfH * (FrameMax - FrameMin);
            cx = Mathf.Clamp(cx, local.x - maxOffX, local.x + maxOffX);
            // cy include hudBias; confronta con local.y + hudBias come "centro utile"
            float cyLocal = cy - hudBias;
            cyLocal = Mathf.Clamp(cyLocal, local.y - maxOffY, local.y + maxOffY);
            cy = cyLocal + hudBias;

            _aimX = cx; _aimY = cyLocal;

            float dist = CameraDistance > 1f ? CameraDistance : UM_QualityTier.CameraDistance;
            var want = _rot * new Vector3(cx, cy, 0) - (_rot * Vector3.forward) * dist;
            float smooth = moving ? FollowSmoothMove : FollowSmoothIdle;
            float t = snap ? 1f : (1f - Mathf.Exp(-smooth * Time.deltaTime)); // #191: stessa base tempi di Nina
            Cm.transform.position = Vector3.Lerp(Cm.transform.position, want, t);
            var lens = Cm.Lens; lens.OrthographicSize = _ortho; Cm.Lens = lens;
            if (!_pcLooked) { _pcLooked = true; _pc = Cm.GetComponent<CinemachinePositionComposer>(); } // #191: niente GetComponent a ogni frame
            if (_pc && !Mathf.Approximately(_pc.CameraDistance, dist)) _pc.CameraDistance = dist;
            var cam = Camera.main; if (cam) cam.orthographicSize = _ortho;
        }
    }
}
