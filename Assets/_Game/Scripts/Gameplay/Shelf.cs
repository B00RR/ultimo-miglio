using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Colour-coded shelf: Nina drops matching parcels, and takes any parcel when empty-handed of that colour.</summary>
    public sealed class Shelf : ProximityZone
    {
        public ParcelColor Color; public int Count, Capacity = 12; public Transform[] Slots;
        protected override void OnZoneTick(float t)
        {
            var s = Nina.Stack;
            if (Count < Capacity && s.TryPop(out _, Color)) { Add(1); AudioManager.I?.Play(AudioManager.Sfx.Deposit); GameRoot.I.Qa.Log("deposit", $"\"shelf\":\"{Color}\",\"count\":{Count}"); }
        }
        public void Add(int n) { Count = Mathf.Min(Capacity, Count + n); Refresh(); }
        public bool Take() { if (Count <= 0) return false; Count--; Refresh(); return true; }
        void Refresh() { if (Slots == null) return; for (int i = 0; i < Slots.Length; i++) Slots[i].gameObject.SetActive(i < Count); }
    }
}
