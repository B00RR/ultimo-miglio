// Ultimo Miglio - volo di una moneta: arco (Bézier quadratica) + easing verso un bersaglio che può muoversi (HUD).
using System;
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_CoinFlight : MonoBehaviour
    {
        public float duration = 0.6f, arcHeight = 1.6f, spinSpeed = 720f;
        Vector3 _start; Func<Vector3> _target; Action _onArrive; float _t, _delay; bool _flying;
        TrailRenderer _trail; ParticleSystem _sparkle; Transform _body;
        public bool Busy => _flying || gameObject.activeSelf;

        void Awake() { _trail = GetComponent<TrailRenderer>(); _sparkle = GetComponentInChildren<ParticleSystem>(true); _body = transform.childCount > 0 ? transform.GetChild(0) : transform; }

        public void Launch(Vector3 start, Func<Vector3> target, float delay, Action onArrive)
        {
            _start = start + UnityEngine.Random.insideUnitSphere * 0.15f; _target = target; _onArrive = onArrive; _t = 0; _delay = delay; _flying = true;
            transform.position = _start; transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            if (_trail) { _trail.Clear(); _trail.emitting = false; }
        }

        void Update()
        {
            if (!_flying) return;
            if (_delay > 0) { _delay -= Time.deltaTime; return; }
            if (_trail && !_trail.emitting) { _trail.emitting = true; if (_sparkle) _sparkle.Play(true); }
            _t += Time.deltaTime / Mathf.Max(0.05f, duration);
            float k = Mathf.Clamp01(_t);
            float e = k < 0.5f ? 2 * k * k : 1 - Mathf.Pow(-2 * k + 2, 2) / 2;  // easeInOutQuad
            Vector3 end = _target();
            Vector3 ctrl = (_start + end) * 0.5f + Vector3.up * arcHeight;
            Vector3 a = Vector3.Lerp(_start, ctrl, e), b = Vector3.Lerp(ctrl, end, e);
            transform.position = Vector3.Lerp(a, b, e);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e * e);
            _body.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.Self);
            if (k >= 1f)
            {
                _flying = false;
                if (_trail) _trail.emitting = false;
                if (_sparkle) _sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                gameObject.SetActive(false);
                _onArrive?.Invoke();
            }
        }
    }
}
