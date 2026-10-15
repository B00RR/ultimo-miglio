using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Trigger zone (02 §2b): 0.5 s dwell, then OnZoneTick every 0.1 s while Nina stays. Highest priority wins on overlap.</summary>
    [RequireComponent(typeof(Collider))]
    public abstract class ProximityZone : MonoBehaviour
    {
        public int Priority; public float DwellSec = 0.5f, TickSec = 0.1f;
        public UltimoMiglio.UI.ZoneFillRing Ring;
        static ProximityZone _active;
        protected NinaController Nina { get; private set; }
        protected float Dwell { get; private set; }
        float _acc; bool _started;

        protected virtual void Reset() { GetComponent<Collider>().isTrigger = true; }
        void OnTriggerEnter(Collider o) { var n = o.GetComponent<NinaController>(); if (n) { Nina = n; Dwell = 0; _acc = 0; _started = false; if (!_active || _active.Priority < Priority) _active = this; } }
        void OnTriggerExit(Collider o) { if (o.GetComponent<NinaController>()) { Nina = null; if (_active == this) _active = null; OnZoneExit(); } }

        void Update()
        {
            OnIdleUpdate();
            if (!Nina) return;
            if (_active && _active != this && _active.Nina) return;
            _active = this;
            Dwell += Time.deltaTime;
            if (Dwell < DwellSec) { if (Ring) Ring.SetFill(Dwell / DwellSec); return; }
            if (!_started) { _started = true; OnZoneStart(); }
            _acc += Time.deltaTime;
            while (_acc >= TickSec) { _acc -= TickSec; OnZoneTick(Dwell - DwellSec); }
        }
        protected virtual void OnIdleUpdate() { }
        protected virtual void OnZoneStart() { }
        protected virtual void OnZoneExit() { if (Ring) Ring.Drain(); }
        protected abstract void OnZoneTick(float activeSec);
    }
}
