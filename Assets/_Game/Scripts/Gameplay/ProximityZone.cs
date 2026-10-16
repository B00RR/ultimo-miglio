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
        void OnTriggerEnter(Collider o) { if (PerDistanza) return; var n = o.GetComponent<NinaController>(); if (n) Entra(n); }
        void OnTriggerExit(Collider o) { if (PerDistanza) return; if (o.GetComponent<NinaController>()) Esci(); }
        void Entra(NinaController n) { Nina = n; Dwell = 0; _acc = 0; _started = false; if (!_active || _active.Priority < Priority) _active = this; }
        void Esci() { Nina = null; if (_active == this) _active = null; OnZoneExit(); }
        /// <summary>#171: true = la zona decide "dentro/fuori" con <see cref="DentroPerDistanza"/> a ogni frame invece che con OnTriggerEnter/Exit
        /// (il contatto trigger↔CharacterController scattava a 0,60-0,65 m su un disco di 0,70, e un collider riacceso con Kaia già dentro non manda Enter).</summary>
        protected virtual bool PerDistanza => false;
        protected virtual bool DentroPerDistanza(NinaController n) => false;
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
            if (PerDistanza)
            {
                var n = GameRoot.I.Nina; bool dentro = n && DentroPerDistanza(n);
                if (dentro && !Nina) Entra(n); else if (!dentro && Nina) Esci();
            }
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
                if (!mb) continue; // componente con script mancante o già distrutto (es. PadTag rimosso da InteractionPoint)
                var m = mb.GetType().GetMethod("ShowOnPad", new[] { typeof(bool) });
                if (m != null) { m.Invoke(mb, new object[] { on }); return; }
            }
        }
        protected abstract void OnZoneTick(float activeSec);
    }
}
