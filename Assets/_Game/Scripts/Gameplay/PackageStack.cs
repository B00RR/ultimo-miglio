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

        public bool TryPush(ParcelColor c)
        {
            if (Full) return false;
            var go = _pool.Count > 0 ? _pool.Pop() : Instantiate(ParcelPrefab, Anchor);
            go.SetActive(true); Tint(go, c); _items.Add((go, c));
            AudioManager.I?.PlayPickup(Count);
            OnPicked?.Invoke(Count);
            if (Full) AudioManager.I?.Play(AudioManager.Sfx.StackMax);
            GameRoot.I.Qa.Log("pickup", $"\"stack\":{Count},\"color\":\"{c}\"");
            return true;
        }
        public bool TryPop(out ParcelColor c, ParcelColor? only = null)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (only.HasValue && _items[i].c != only.Value) continue;
                c = _items[i].c; _items[i].go.SetActive(false); _pool.Push(_items[i].go); _items.RemoveAt(i); return true;
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
        void LateUpdate()
        {
            Vector3 v = (Anchor.position - _lastPos) / Mathf.Max(Time.deltaTime, 1e-4f); _lastPos = Anchor.position;
            _lag = Vector3.Lerp(_lag, -v * SwayPerLevel, 1 - Mathf.Exp(-Stiffness * Time.deltaTime));
            for (int i = 0; i < _items.Count; i++)
            {
                var t = _items[i].go.transform;
                t.localPosition = new Vector3(0, i * Height, 0) + Anchor.InverseTransformVector(_lag) * (i * 0.15f) + new Vector3(0, 0, Mathf.Sin(Time.time * 3 + i) * 0.003f * i);
                t.localRotation = Quaternion.Euler(0, (i * 37) % 11 - 5, 0);
            }
        }
    }
}
