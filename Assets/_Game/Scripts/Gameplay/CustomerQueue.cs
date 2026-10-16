using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Spawns customers at CustomersPerMin (always more than Nina can serve) into a line in front of the Bancone.</summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        public Customer Prefab; [Tooltip("#2: varianti (6 clienti riggati); vuoto = solo Prefab")] public Customer[] Variants; public Transform Door, CounterSlot; public Vector3 LineStep = new(0, 0, -0.9f); public int MaxLine = 8;
        readonly List<Customer> _line = new(); readonly Stack<Customer> _pool = new(); float _t;
        public Customer Head => _line.Count > 0 ? _line[0] : null;
        public int Waiting => _line.Count;
        void Update()
        {
            if (!GameRoot.Ready || !Prefab || !Door || !CounterSlot) return;
            _t += Time.deltaTime; float every = 60f / (float)GameRoot.I.Eco.Cfg.CustomersPerMin;
            if (_slots == null) BuildSlots();
            if (_t >= every && _line.Count < MaxLine && DoorFree()) { _t = 0; Spawn(); }
            for (int i = 0; i < _line.Count; i++) _line[i].Target = Slot(i);
        }

        // ---- #111: fila e ingresso raggiungibili senza attraversare i solidi ----
        // La porta della scena (Door, x 8,5) sta fuori dal muro E del deposito (x 7,6, archi compresi hanno BoxCollider pieno) e la fila
        // diagonale dal 6° posto in poi finisce oltre il bordo S: prima i clienti ci arrivavano solo col teletrasporto anti-stallo.
        Vector3[] _slots; Vector3 _spawn;
        /// <summary>Posto i della fila (0 = davanti al Bancone).</summary>
        public Vector3 Slot(int i) { if (_slots == null) BuildSlots(); return _slots[Mathf.Clamp(i, 0, _slots.Length - 1)]; }
        /// <summary>Punto in cui nascono ed escono i clienti: la porta se è dentro l'area raggiungibile, altrimenti il fondo della fila.</summary>
        public Vector3 SpawnPoint { get { if (_slots == null) BuildSlots(); return _spawn; } }
        static readonly Collider[] SolidHits = new Collider[16];
        static bool Solid(Vector3 p)
        {
            int n = Physics.OverlapSphereNonAlloc(p + Vector3.up * 0.8f, Customer.BodyRadius + 0.05f, SolidHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var c = SolidHits[i]; if (c.attachedRigidbody || AgentMove.IsAgent(c)) continue; return true; }
            return false;
        }
        static bool Reachable(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0.8f; var d = b - a; if (d.sqrMagnitude < 1e-6f) return true;
            var hits = Physics.SphereCastAll(a, Customer.BodyRadius, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) if (!h.collider.attachedRigidbody && !AgentMove.IsAgent(h.collider)) return false;
            return true;
        }
        void BuildSlots()
        {
            var list = new List<Vector3> { CounterSlot.position };
            float step = Mathf.Max(LineStep.magnitude, 2f * Customer.BodyRadius + 0.05f);
            var dirs = new[] { LineStep.normalized, new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(-1, 0, 0) };
            for (int i = 1; i < Mathf.Max(1, MaxLine); i++)
            {
                var prev = list[i - 1]; Vector3? pick = null;
                foreach (var d in dirs)
                {
                    var c = prev + d * step; bool ok = !Solid(c) && Reachable(prev, c);
                    for (int k = 0; ok && k < list.Count - 1; k++) if ((list[k] - c).sqrMagnitude < (2f * Customer.BodyRadius + 0.05f) * (2f * Customer.BodyRadius + 0.05f)) ok = false;
                    if (ok) { pick = c; break; }
                }
                list.Add(pick ?? prev + LineStep); // nessun posto libero: come prima (caso limite)
            }
            _slots = list.ToArray();
            _spawn = Door && !Solid(Door.position) && Reachable(Door.position, _slots[_slots.Length - 1]) ? Door.position : _slots[_slots.Length - 1];
        }
        static readonly Collider[] DoorHits = new Collider[8];
        /// <summary>#111: niente spawn sopra un cliente ancora sulla porta (prima nasceva sovrapposto).</summary>
        bool DoorFree()
        {
            int n = Physics.OverlapSphereNonAlloc(SpawnPoint + Vector3.up * 0.8f, 2f * Customer.BodyRadius, DoorHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (DoorHits[i].enabled && DoorHits[i].GetComponentInParent<Customer>()) return false;
            return true;
        }
        static void SetBody(Customer c, bool on) { var col = c.GetComponent<Collider>(); if (col) col.enabled = on; }
        void Spawn()
        {
            var c = _pool.Count > 0 ? _pool.Pop() : Instantiate(Variants != null && Variants.Length > 0 ? Variants[Random.Range(0, Variants.Length)] : Prefab, transform);
            c.Owner = this; c.transform.position = SpawnPoint; c.Leaving = false; c.Waited = 0; c.Wants = (ParcelColor)Random.Range(0, 3);
            SetBody(c, true); c.gameObject.SetActive(true); _line.Add(c);
            if (_line.Count == 1) AudioManager.I?.Play(AudioManager.Sfx.Bell);
        }
        public void Dequeue(Customer c, bool served)
        {
            if (!_line.Remove(c)) return;
            if (!served) GameRoot.I.Eco.CustomerLost();
            c.OnLeave(served); c.Leaving = true; c.Target = SpawnPoint; SetBody(c, false); // #111: chi esce non blocca la fila
        }
        public void Recycle(Customer c) { c.gameObject.SetActive(false); _pool.Push(c); }
    }
}
