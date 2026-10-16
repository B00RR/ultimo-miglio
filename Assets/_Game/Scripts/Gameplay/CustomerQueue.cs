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
            if (_t >= every && _line.Count < MaxLine && DoorFree()) { _t = 0; Spawn(); }
            for (int i = 0; i < _line.Count; i++) _line[i].Target = CounterSlot.position + LineStep * i;
        }
        static readonly Collider[] DoorHits = new Collider[8];
        /// <summary>#111: niente spawn sopra un cliente ancora sulla porta (prima nasceva sovrapposto).</summary>
        bool DoorFree()
        {
            int n = Physics.OverlapSphereNonAlloc(Door.position + Vector3.up * 0.8f, 2f * Customer.BodyRadius, DoorHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (DoorHits[i].enabled && DoorHits[i].GetComponentInParent<Customer>()) return false;
            return true;
        }
        static void SetBody(Customer c, bool on) { var col = c.GetComponent<Collider>(); if (col) col.enabled = on; }
        void Spawn()
        {
            var c = _pool.Count > 0 ? _pool.Pop() : Instantiate(Variants != null && Variants.Length > 0 ? Variants[Random.Range(0, Variants.Length)] : Prefab, transform);
            c.Owner = this; c.transform.position = Door.position; c.Leaving = false; c.Waited = 0; c.Wants = (ParcelColor)Random.Range(0, 3);
            SetBody(c, true); c.gameObject.SetActive(true); _line.Add(c);
            if (_line.Count == 1) AudioManager.I?.Play(AudioManager.Sfx.Bell);
        }
        public void Dequeue(Customer c, bool served)
        {
            if (!_line.Remove(c)) return;
            if (!served) GameRoot.I.Eco.CustomerLost();
            c.OnLeave(served); c.Leaving = true; c.Target = Door.position; SetBody(c, false); // #111: chi esce non blocca la fila
        }
        public void Recycle(Customer c) { c.gameObject.SetActive(false); _pool.Push(c); }
    }
}
