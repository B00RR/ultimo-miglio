using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Iso camera: frames the depot, soft-follows Nina inside those bounds, and exposes OrthoSize for Impostazioni.
    /// Default zoom in portrait is ~13–14 so Nina is readable and the depot is still nearly full.</summary>
    public sealed class DepotCameraFit : MonoBehaviour
    {
        public const string PrefZoom = "cam_zoom";
        public CinemachineCamera Cm; public Transform Nina;
        public float Margin = 1.2f, TopHud = 0.18f, BottomHud = 0.08f, MinOrtho = 8f, MaxOrtho = 28f;
        public float FollowSmooth = 6f, DefaultPortraitOrtho = 13.5f;
        readonly List<Vector3> _pts = new();
        int _w, _h; float _ortho, _fullOrtho; Vector3 _centreWorld; Quaternion _rot; Vector2 _mn, _mx;

        public float OrthoSize { get => _ortho; set { _ortho = Mathf.Clamp(value, MinOrtho, MaxOrtho); Apply(); } }
        public float FullOrtho => _fullOrtho;

        void Start()
        {
            Collect(); _rot = Cm ? Cm.transform.rotation : Quaternion.identity;
            if (Cm) Cm.Follow = null;
            RecomputeBounds();
            float saved = PlayerPrefs.GetFloat(PrefZoom, 0f);
            bool portrait = Screen.height > Screen.width;
            // Portrait default 13.5: ignore stale high zooms from the old full-depot fit (~20)
            if (portrait) _ortho = (saved >= 9f && saved <= 16f) ? saved : DefaultPortraitOrtho;
            else _ortho = saved >= 8f ? saved : Mathf.Min(_fullOrtho > 0 ? _fullOrtho : 11f, 11f);
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
            // keep Nina in view with a soft dead-zone, never leave the depot bounds
            float halfH = _ortho * (1f - TopHud - BottomHud), halfW = _ortho * aspect;
            float cx = Mathf.Clamp(local.x, _mn.x + halfW, _mx.x - halfW);
            float cy = Mathf.Clamp(local.y, _mn.y + halfH, _mx.y - halfH);
            // bias for HUD bands
            cy += _ortho * (TopHud - BottomHud) * 0.5f;
            var want = _rot * new Vector3(cx, cy, 0) - Cm.transform.forward * 40f;
            Cm.transform.position = snap ? want : Vector3.Lerp(Cm.transform.position, want, 1f - Mathf.Exp(-FollowSmooth * Time.unscaledDeltaTime));
            var lens = Cm.Lens; lens.OrthographicSize = _ortho; Cm.Lens = lens;
            var pc = Cm.GetComponent<CinemachinePositionComposer>(); if (pc) pc.CameraDistance = 40f;
            var cam = Camera.main; if (cam) cam.orthographicSize = _ortho;
        }

        public void ZoomBy(float delta) { OrthoSize = _ortho + delta; }
        public void SetOrthoSize(float v) { OrthoSize = v; }
        public float GetOrthoSize() => OrthoSize;
        public void SaveZoom() { PlayerPrefs.SetFloat(PrefZoom, _ortho); PlayerPrefs.Save(); }
    }
}
