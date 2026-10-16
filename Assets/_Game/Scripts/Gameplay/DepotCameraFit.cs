using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Iso camera: soft-follows Nina con dead-zone centrata (QA v5 P2). Portrait default ortho 4.5.</summary>
    public sealed class DepotCameraFit : MonoBehaviour
    {
        public const string PrefZoom = "cam_zoom_v3";
        public CinemachineCamera Cm; public Transform Nina;
        public float Margin = 1.0f, TopHud = 0.20f, BottomHud = 0.08f, MinOrtho = 4.2f, MaxOrtho = 18f;
        /// <summary>FollowSmooth alto = meno ritardo. DeadZone = frazione di half-size: dentro la zona la camera sta ferma.</summary>
        public float FollowSmooth = 14f, DeadZone = 0.16f, CatchUpAt = 0.78f;
        public float DefaultPortraitOrtho = 4.5f, DefaultLandscapeOrtho = 4.8f;
        public float CameraDistance = UM_QualityTier.CameraDistance;
        readonly List<Vector3> _pts = new();
        int _w, _h; float _ortho, _fullOrtho; Vector3 _centreWorld; Quaternion _rot; Vector2 _mn, _mx;
        float _aimX, _aimY; bool _aimInit;

        public float OrthoSize { get => _ortho; set { _ortho = Mathf.Clamp(value, MinOrtho, MaxOrtho); Apply(); } }
        public float FullOrtho => _fullOrtho;

        void Start()
        {
            Collect(); _rot = Cm ? Cm.transform.rotation : Quaternion.identity;
            if (Cm) Cm.Follow = null;
            // QA v5 P2: forza follow reattivo + dead-zone (ignora valori serializzati lenti da scene vecchie)
            FollowSmooth = 14f; DeadZone = 0.16f; CatchUpAt = 0.78f;
            RecomputeBounds();
            float saved = PlayerPrefs.GetFloat(PrefZoom, 0f);
            bool portrait = Screen.height > Screen.width;
            if (portrait) _ortho = (saved >= 4.2f && saved <= 6.5f) ? saved : DefaultPortraitOrtho;
            else _ortho = (saved >= 5f && saved <= 10f) ? saved : DefaultLandscapeOrtho;
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
            Vector3 target = Nina ? Nina.position : _centreWorld;
            var local = inv * target;
            float halfH = _ortho * (1f - TopHud - BottomHud), halfW = _ortho * aspect;
            if (!_aimInit || snap) { _aimX = local.x; _aimY = local.y; _aimInit = true; }

            // Dead-zone centrata: la camera si muove solo quando Nina esce dalla zona intorno all'aim corrente.
            float dzX = halfW * Mathf.Clamp01(DeadZone);
            float dzY = halfH * Mathf.Clamp01(DeadZone);
            float dx = local.x - _aimX, dy = local.y - _aimY;
            if (Mathf.Abs(dx) > dzX) _aimX = local.x - Mathf.Sign(dx) * dzX;
            if (Mathf.Abs(dy) > dzY) _aimY = local.y - Mathf.Sign(dy) * dzY;

            float cx = Mathf.Clamp(_aimX, _mn.x + halfW, _mx.x - halfW);
            float cy = Mathf.Clamp(_aimY, _mn.y + halfH, _mx.y - halfH);
            cy += _ortho * (TopHud - BottomHud) * 0.5f; // sposta il centro utile sotto la TopHud
            _aimX = cx; _aimY = cy - _ortho * (TopHud - BottomHud) * 0.5f;

            float dist = CameraDistance > 1f ? CameraDistance : UM_QualityTier.CameraDistance;
            var want = _rot * new Vector3(cx, cy, 0) - Cm.transform.forward * dist;
            float t = snap ? 1f : (1f - Mathf.Exp(-FollowSmooth * Time.unscaledDeltaTime));
            // Catch-up se Nina sta uscendo dal quadro (ortho stretto 4.5)
            float outFrac = Mathf.Max(Mathf.Abs(dx) / Mathf.Max(0.01f, halfW), Mathf.Abs(dy) / Mathf.Max(0.01f, halfH));
            if (!snap && outFrac > CatchUpAt) t = Mathf.Max(t, 0.55f);
            Cm.transform.position = Vector3.Lerp(Cm.transform.position, want, t);
            var lens = Cm.Lens; lens.OrthographicSize = _ortho; Cm.Lens = lens;
            var pc = Cm.GetComponent<CinemachinePositionComposer>(); if (pc) pc.CameraDistance = dist;
            var cam = Camera.main; if (cam) cam.orthographicSize = _ortho;
        }

        public void ZoomBy(float delta) { OrthoSize = _ortho + delta; }
        public void SetOrthoSize(float v) { OrthoSize = v; }
        public float GetOrthoSize() => OrthoSize;
        public void SaveZoom() { PlayerPrefs.SetFloat(PrefZoom, _ortho); PlayerPrefs.Save(); }
    }
}
