using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{
    /// <summary>
    /// Iso camera: target = Nina. Dead-zone solo in movimento; a idle ricentra in ~0.3 s (QA v6).
    /// Il clamp mappa non può mai spingere Nina fuori dal 70% centrale dello schermo.
    /// </summary>
    public sealed class DepotCameraFit : MonoBehaviour
    {
        public const string PrefZoom = "cam_zoom_v3";
        /// <summary>#104: range dello zoom scelto dal giocatore (pannello Impostazioni) = range accettato al boot da cam_zoom_v3.</summary>
        public const float UserMinOrtho = 4.2f, UserMaxOrtho = 9f;
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

        // Compat: vecchio campo serializzato FollowSmooth → Move
        public float FollowSmooth { get => FollowSmoothMove; set => FollowSmoothMove = value; }

        public float OrthoSize { get => _ortho; set { _ortho = Mathf.Clamp(value, MinOrtho, MaxOrtho); Apply(); } }
        public float FullOrtho => _fullOrtho;

        void Start()
        {
            Collect(); _rot = Cm ? Cm.transform.rotation : Quaternion.identity;
            if (Cm) Cm.Follow = null;
            // QA v6: knobs forzati (scene serializzate non devono tenere il follow lento / clamp aggressivo)
            FollowSmoothMove = 14f; FollowSmoothIdle = 12f; DeadZone = 0.14f; MoveSpeedEps = 0.35f;
            FrameMin = 0.15f; FrameMax = 0.85f;
            RecomputeBounds();
            float saved = PlayerPrefs.GetFloat(PrefZoom, 0f);
            bool portrait = Screen.height > Screen.width;
            // #104: prima in verticale si accettava solo 4.2-6.5 e lo zoom salvato dal pannello (fino a 9) si perdeva al reload
            _ortho = (saved >= UserMinOrtho && saved <= UserMaxOrtho) ? saved : (portrait ? DefaultPortraitOrtho : DefaultLandscapeOrtho);
            _ortho = Mathf.Clamp(_ortho, MinOrtho, MaxOrtho);
            Apply(true);
        }

        void Collect()
        {
            _pts.Clear();
            var tut = FindAnyObjectByType<TutorialManager>();
            if (tut) foreach (var t in new[] { tut.Arrivi, tut.Scaffale, tut.Bancone, tut.Monete, tut.PadZaino, tut.PadNonno }) if (t) { _pts.Add(t.position + new Vector3(-1.2f, 0, -1.2f)); _pts.Add(t.position + new Vector3(1.2f, 0, 1.2f)); }
            foreach (var z in FindObjectsByType<ProximityZone>(FindObjectsSortMode.None)) { _pts.Add(z.transform.position + new Vector3(-1.2f, 0, -1.2f)); _pts.Add(z.transform.position + new Vector3(1.2f, 0, 1.2f)); }
        }

        void LateUpdate()
        {
            if (Screen.width != _w || Screen.height != _h) { RecomputeBounds(); Apply(true); }
            else Apply(false);
        }

        void RecomputeBounds()
        {
            _w = Screen.width; _h = Screen.height;
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
            var local = inv * ninaPos;
            float halfH = _ortho * (1f - TopHud - BottomHud), halfW = _ortho * aspect;
            float hudBias = _ortho * (TopHud - BottomHud) * 0.5f;

            // Velocità Nina (XZ) → dead-zone solo in movimento
            bool moving = false;
            if (Nina && _prevNinaInit)
            {
                float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime);
                var v = (ninaPos - _prevNina) / dt; v.y = 0f;
                moving = v.sqrMagnitude > MoveSpeedEps * MoveSpeedEps;
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
            var want = _rot * new Vector3(cx, cy, 0) - Cm.transform.forward * dist;
            float smooth = moving ? FollowSmoothMove : FollowSmoothIdle;
            float t = snap ? 1f : (1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            Cm.transform.position = Vector3.Lerp(Cm.transform.position, want, t);
            var lens = Cm.Lens; lens.OrthographicSize = _ortho; Cm.Lens = lens;
            var pc = Cm.GetComponent<CinemachinePositionComposer>(); if (pc) pc.CameraDistance = dist;
            var cam = Camera.main; if (cam) cam.orthographicSize = _ortho;
        }

        public void ZoomBy(float delta) { OrthoSize = Mathf.Clamp(_ortho + delta, UserMinOrtho, UserMaxOrtho); }
        public void SetOrthoSize(float v) { OrthoSize = Mathf.Clamp(v, UserMinOrtho, UserMaxOrtho); }
        public float GetOrthoSize() => OrthoSize;
        public void SaveZoom() { PlayerPrefs.SetFloat(PrefZoom, _ortho); PlayerPrefs.Save(); }
    }
}
