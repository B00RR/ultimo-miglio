using UltimoMiglio.VFX;
using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    public enum ParcelColor { A_Terracotta, B_Menta, C_Limone }

    /// <summary>Stack on Nina's back: pooled parcels, procedural sway (no joints). Capacity from EconomyCore (Zaino).</summary>
    public sealed class PackageStack : MonoBehaviour
    {
        public GameObject ParcelPrefab; public Transform Anchor;
        public float Height = 0.32f, SwayPerLevel = 0.02f, Stiffness = 18f;
        readonly List<(GameObject go, ParcelColor c)> _items = new();
        readonly Stack<GameObject> _pool = new();
        Vector3 _lastPos, _lag;
        public int Count => _items.Count;
        public int Capacity => (int)GameRoot.I.Eco.StackCapacity;
        public bool Full => Count >= Capacity;
        public System.Action<int> OnPicked;

        // ---- game feel: arc tweens + per-level damped spring sway ----
        sealed class Fly { public Transform T; public Vector3 From, To; public float Age, Dur, ArcH; public bool ToStack; public int Slot; }
        readonly List<Fly> _flies = new(); readonly List<Vector3> _off = new(), _vel = new(); readonly List<float> _punch = new();
        public float SpringK = 120f, SpringDamp = 9f, Lean = 0.06f, FlyDur = 0.28f;

        public bool TryPush(ParcelColor c) => TryPush(c, null);
        public bool TryPush(ParcelColor c, Vector3? from)
        {
            if (Full) return false;
            var go = _pool.Count > 0 ? _pool.Pop() : Instantiate(ParcelPrefab, Anchor);
            go.SetActive(true); Tint(go, c); _items.Add((go, c)); _off.Add(Vector3.zero); _vel.Add(Vector3.zero); _punch.Add(0f);
            if (from.HasValue) { go.transform.SetParent(null, true); go.transform.position = from.Value; _flies.Add(new Fly { T = go.transform, From = from.Value, Dur = FlyDur, ArcH = 1.2f, ToStack = true, Slot = _items.Count - 1 }); }
            AudioManager.I?.PlayPickup(Count);
            OnPicked?.Invoke(Count);
            if (Full) AudioManager.I?.Play(AudioManager.Sfx.StackMax);
            GameRoot.I.Qa.Log("pickup", $"\"stack\":{Count},\"color\":\"{c}\"");
            return true;
        }
        public bool TryPop(out ParcelColor c, ParcelColor? only = null) => TryPop(out c, only, null);
        public bool TryPop(out ParcelColor c, ParcelColor? only, Vector3? to)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (only.HasValue && _items[i].c != only.Value) continue;
                c = _items[i].c; var go = _items[i].go;
                _flies.RemoveAll(f => f.T == go.transform);
                if (to.HasValue) { go.transform.SetParent(null, true); _flies.Add(new Fly { T = go.transform, From = go.transform.position, To = to.Value, Dur = FlyDur, ArcH = 1.0f }); }
                else { go.SetActive(false); _pool.Push(go); }
                _items.RemoveAt(i); _off.RemoveAt(i); _vel.RemoveAt(i); _punch.RemoveAt(i);
                for (int k = 0; k < _flies.Count; k++) if (_flies[k].ToStack && _flies[k].Slot > i) _flies[k].Slot--;
                return true;
            }
            c = default; return false;
        }
        static void Tint(GameObject go, ParcelColor c)
        {
            var r = go.GetComponentInChildren<Renderer>(); if (!r) return;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c == ParcelColor.A_Terracotta ? VFX.UM_Palette.Terracotta : c == ParcelColor.B_Menta ? VFX.UM_Palette.Menta : VFX.UM_Palette.Limone);
            r.SetPropertyBlock(mpb);
        }
        Vector3 SlotLocal(int i) => new Vector3(0, i * Height, 0) + _off[i];
        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 v = (Anchor.position - _lastPos) / dt; _lastPos = Anchor.position;
            Vector3 accLocal = Anchor.InverseTransformVector(-v) * Lean; // inertia: top lags behind movement
            for (int i = 0; i < _items.Count; i++)
            {
                // each level springs toward (level below + inertial lean): wobble grows with height, delayed
                Vector3 below = i > 0 ? _off[i - 1] : Vector3.zero;
                Vector3 target = below + accLocal * (i * 0.12f); target.y = 0;
                _vel[i] += ((target - _off[i]) * SpringK - _vel[i] * SpringDamp) * dt;
                _off[i] += _vel[i] * dt;
                _punch[i] = Mathf.MoveTowards(_punch[i], 0, dt * 4f);
                var t = _items[i].go.transform;
                if (t.parent != Anchor) continue; // in flight
                t.localPosition = SlotLocal(i);
                t.localRotation = Quaternion.Euler(-_off[i].z * 60f, (i * 37) % 11 - 5, _off[i].x * 60f);
                t.localScale = Vector3.one * (1f + Mathf.Sin(_punch[i] * Mathf.PI) * 0.25f);
            }
            for (int k = _flies.Count - 1; k >= 0; k--)
            {
                var f = _flies[k]; f.Age += Time.deltaTime; float u = Mathf.Clamp01(f.Age / f.Dur);
                Vector3 to = f.ToStack && f.Slot < _items.Count ? Anchor.TransformPoint(SlotLocal(f.Slot)) : f.To;
                float e = 1 - (1 - u) * (1 - u);
                f.T.position = Vector3.Lerp(f.From, to, e) + Vector3.up * (4 * f.ArcH * u * (1 - u));
                f.T.rotation = Quaternion.Euler(0, u * 360f, 0);
                if (u >= 1)
                {
                    _flies.RemoveAt(k);
                    if (f.ToStack && f.Slot < _items.Count && _items[f.Slot].go.transform == f.T) { f.T.SetParent(Anchor, true); _punch[f.Slot] = 1f; }
                    else if (!f.ToStack) { f.T.gameObject.SetActive(false); f.T.SetParent(Anchor, false); f.T.localScale = Vector3.one; _pool.Push(f.T.gameObject); UM_FxPlayer.Play(UM_FxPlayer.Fx.PackagePop, f.To); }
                }
            }
        }
    }
}
