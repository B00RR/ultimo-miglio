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
        public static ProximityZone Active => _active;
        public bool Occupied => Nina;
        protected NinaController Nina { get; private set; }
        protected float Dwell { get; private set; }
        float _acc; bool _started;

        protected virtual void Reset() { GetComponent<Collider>().isTrigger = true; }
        void OnTriggerEnter(Collider o) { var n = o.GetComponent<NinaController>(); if (n) { Nina = n; Dwell = 0; _acc = 0; _started = false; if (!_active || _active.Priority < Priority) _active = this; } }
        void OnTriggerExit(Collider o) { if (o.GetComponent<NinaController>()) { Nina = null; if (_active == this) _active = null; OnZoneExit(); } }
        /// <summary>#104: un collider disattivato (o la zona disabilitata) non manda OnTriggerExit. Senza questo Nina restava
        /// "dentro" il pad di NONNO appena assunto e la _active statica bloccava Arrivi, Scaffali e Bancone fino al reload.</summary>
        protected void ReleaseNina()
        {
            if (Nina) { Nina = null; OnZoneExit(); }
            if (_active == this) _active = null;
        }
        protected virtual void OnDisable() { ReleaseNina(); }

        void Update()
        {
            if (!GameRoot.Ready) return;
            OnIdleUpdate();
            if (!Nina) return;
            if (_active && _active != this && _active.Nina) return;
            _active = this;
            Dwell += Time.deltaTime;
            if (Dwell < DwellSec) { if (Ring) Ring.SetFill(Dwell / DwellSec); return; }
            if (!_started) { _started = true; OnZoneStart(); NotifyCard(true); }
            _acc += Time.deltaTime;
            while (_acc >= TickSec) { _acc -= TickSec; OnZoneTick(Dwell - DwellSec); }
        }
        protected virtual void OnIdleUpdate() { }
        protected virtual void OnZoneStart() { }
        protected virtual void OnZoneExit() { if (Ring) Ring.Drain(); NotifyCard(false); }
        void NotifyCard(bool on)
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("ShowOnPad", new[] { typeof(bool) });
                if (m != null) { m.Invoke(mb, new object[] { on }); return; }
            }
        }
        protected abstract void OnZoneTick(float activeSec);
    }
}
