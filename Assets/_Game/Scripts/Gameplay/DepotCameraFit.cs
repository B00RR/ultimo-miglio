using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Frames the whole depot (all Zona_* / Pad_* objects + Nina) with an iso 3/4 view.
    /// Ortho size is computed from the screen aspect and recomputed on resize, so portrait phones see
    /// Arrivi, scaffali, Bancone and pads at once. HUD bands (top/bottom) are reserved as screen fractions.</summary>
    public sealed class DepotCameraFit : MonoBehaviour
    {
        public CinemachineCamera Cm; public Transform Nina;
        public float Margin = 1.5f, TopHud = 0.16f, BottomHud = 0.06f, MinOrtho = 5.5f;
        readonly List<Vector3> _pts = new();
        int _w, _h;

        void Start()
        {
            if (Nina) _pts.Add(Nina.position);
            var tut = FindAnyObjectByType<TutorialManager>();
            if (tut) foreach (var t in new[] { tut.Arrivi, tut.Scaffale, tut.Bancone, tut.Monete, tut.PadZaino, tut.PadNonno }) if (t) { _pts.Add(t.position + new Vector3(-1.5f, 0, -1.5f)); _pts.Add(t.position + new Vector3(1.5f, 0, 1.5f)); }
            foreach (var z in FindObjectsByType<ProximityZone>(FindObjectsSortMode.None)) { _pts.Add(z.transform.position + new Vector3(-1.5f, 0, -1.5f)); _pts.Add(z.transform.position + new Vector3(1.5f, 0, 1.5f)); }
            if (Cm) Cm.Follow = null;
            Fit();
        }

        void LateUpdate() { if (Screen.width != _w || Screen.height != _h) Fit(); }

        static Vector2 c0(Vector2 a, Vector2 b) => (a + b) * 0.5f;
        public void Fit()
        {
            _w = Screen.width; _h = Screen.height;
            if (!Cm || _pts.Count == 0) return;
            var rot = Cm.transform.rotation; var inv = Quaternion.Inverse(rot);
            Vector2 mn = new(float.MaxValue, float.MaxValue), mx = new(float.MinValue, float.MinValue);
            foreach (var p in _pts)
                for (int i = 0; i < 2; i++) // ground point + 1.6 m height so walls/characters fit
                {
                    var l = inv * (p + Vector3.up * (i * 1.6f));
                    mn = Vector2.Min(mn, new Vector2(l.x, l.y)); mx = Vector2.Max(mx, new Vector2(l.x, l.y));
                }
            mn -= Vector2.one * Margin; mx += Vector2.one * Margin;
            float aspect = Mathf.Max(0.2f, (float)_w / Mathf.Max(1, _h));
            float usable = 1f - TopHud - BottomHud;
            float halfH = (mx.y - mn.y) * 0.5f / usable, halfW = (mx.x - mn.x) * 0.5f / aspect;
            float ortho = Mathf.Max(MinOrtho, halfH, halfW);
            Debug.Log($"[CamFit] pts={_pts.Count} aspect={aspect:F2} ortho={ortho:F1} c={c0(mn,mx)}");
            // shift centre so the reserved HUD bands are outside the depot
            var c = new Vector2((mn.x + mx.x) * 0.5f, (mn.y + mx.y) * 0.5f + ortho * (TopHud - BottomHud));
            var lens = Cm.Lens; lens.OrthographicSize = ortho; Cm.Lens = lens;
            var centreLocal = new Vector3(c.x, c.y, 0);
            Cm.transform.position = rot * centreLocal - Cm.transform.forward * 40f;
            var cam = Camera.main; if (cam) cam.orthographicSize = ortho;
        }
    }
}
