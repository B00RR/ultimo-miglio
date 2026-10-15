using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Spawns customers at CustomersPerMin (always more than Nina can serve) into a line in front of the Bancone.</summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        public Customer Prefab; public Transform Door, CounterSlot; public Vector3 LineStep = new(0, 0, -0.9f); public int MaxLine = 8;
        readonly List<Customer> _line = new(); readonly Stack<Customer> _pool = new(); float _t;
        public Customer Head => _line.Count > 0 ? _line[0] : null;
        public int Waiting => _line.Count;
        void Update()
        {
            _t += Time.deltaTime; float every = 60f / (float)GameRoot.I.Eco.Cfg.CustomersPerMin;
            if (_t >= every && _line.Count < MaxLine) { _t = 0; Spawn(); }
            for (int i = 0; i < _line.Count; i++) _line[i].Target = CounterSlot.position + LineStep * i;
        }
        void Spawn()
        {
            var c = _pool.Count > 0 ? _pool.Pop() : Instantiate(Prefab, transform);
            c.Owner = this; c.transform.position = Door.position; c.Leaving = false; c.Waited = 0; c.Wants = (ParcelColor)Random.Range(0, 3);
            c.gameObject.SetActive(true); _line.Add(c);
            if (_line.Count == 1) AudioManager.I?.Play(AudioManager.Sfx.Bell);
        }
        public void Dequeue(Customer c, bool served)
        {
            if (!_line.Remove(c)) return;
            if (!served) GameRoot.I.Eco.CustomerLost();
            c.Leaving = true; c.Target = Door.position;
        }
        public void Recycle(Customer c) { c.gameObject.SetActive(false); _pool.Push(c); }
    }
}
