// Ultimo Miglio - lanterna: emissivo + luce puntiforme che si accendono in dissolvenza al tramonto e si spengono al mattino.
// Le luci reali sono limitate per tier (le più vicine alla camera); le altre restano solo emissive (costo zero).
using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_Lantern : MonoBehaviour
    {
        public Light pointLight;               // opzionale
        public Renderer[] emissiveRenderers;   // materiale M_Diorama_Atlas_Emissive
        public float maxIntensity = 1.7f;      // valore approvato dal Concept Artist
        public Color emission = new Color(1f, 0.714f, 0.361f) * 0.8f; // #FFB65C
        public float fadeTime = 1.2f;
        [Tooltip("Ritardo casuale max (s): le lanterne non si accendono tutte insieme")] public float randomDelay = 0.6f;

        static readonly List<UM_Lantern> All = new();
        static readonly int EmId = Shader.PropertyToID("_EmissionColor");
        static float _nextCull;
        MaterialPropertyBlock _mpb; float _level, _target, _delay; bool _lightAllowed = true;

        /// Luci puntiformi reali massime per tier: Web 2, Alto 4, Standard/Base 0 (solo emissivo).
        public static int MaxLights => UM_QualityTier.Current switch { UMTier.Web => 2, UMTier.Alto => 4, _ => 0 };

        void OnEnable() { All.Add(this); _mpb ??= new MaterialPropertyBlock(); if (UM_DayCycle.Instance) { _target = UM_DayCycle.Instance.LanternsOn ? 1 : 0; _level = _target; } Push(); }
        void OnDisable() { All.Remove(this); }

        public static void SetAll(bool on) { foreach (var l in All) { l._target = on ? 1 : 0; l._delay = Random.Range(0f, l.randomDelay); } }
        public static void SnapAll() { var on = UM_DayCycle.Instance && UM_DayCycle.Instance.LanternsOn; foreach (var l in All) { l._target = l._level = on ? 1 : 0; l._delay = 0; l.Push(); } }
        public void SetOn(bool on, bool instant = false) { _target = on ? 1 : 0; if (instant) { _level = _target; Push(); } }

        void Update()
        {
            if (Time.unscaledTime >= _nextCull) { _nextCull = Time.unscaledTime + 0.5f; CullLights(); }
            if (_delay > 0) { _delay -= Time.deltaTime; return; }
            if (!Mathf.Approximately(_level, _target)) { _level = Mathf.MoveTowards(_level, _target, Time.deltaTime / Mathf.Max(0.01f, fadeTime)); Push(); }
        }

        static readonly List<UM_Lantern> _sorted = new();
        static void CullLights()
        {
            var cam = Camera.main; int max = MaxLights;
            _sorted.Clear(); _sorted.AddRange(All);
            if (cam) { var p = cam.transform.position; var f = cam.transform.forward;
                // camera ortografica: distanza dal raggio centrale della vista
                _sorted.Sort((a, b) => Vector3.Cross(f, a.transform.position - p).sqrMagnitude.CompareTo(Vector3.Cross(f, b.transform.position - p).sqrMagnitude)); }
            for (int i = 0; i < _sorted.Count; i++) { _sorted[i]._lightAllowed = i < max; _sorted[i].Push(); }
        }

        void Push()
        {
            float e = Mathf.SmoothStep(0, 1, _level);
            if (pointLight) { pointLight.intensity = maxIntensity * e; pointLight.enabled = _lightAllowed && e > 0.01f; }
            if (emissiveRenderers == null) return;
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in emissiveRenderers) { if (!r) continue; r.GetPropertyBlock(_mpb); _mpb.SetColor(EmId, emission * e); r.SetPropertyBlock(_mpb); }
        }
    }
}
