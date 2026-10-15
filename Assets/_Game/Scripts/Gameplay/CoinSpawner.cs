using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Floor coins (02 §2b): pooled, magnetic pickup radius, merged into one pickup above ~50 coins.
    /// Spawn() = real LN credited on pickup; Burst() = visual only (Cassa LN already credited).</summary>
    public sealed class CoinSpawner : MonoBehaviour
    {
        public GameObject CoinPrefab; public float MagnetRadius = 1.6f, MagnetSpeed = 9f, LnPerCoin = 2f; public int MergeAbove = 50;
        sealed class Coin { public Transform T; public double Ln; public bool Real; public float Age; public Vector3 Vel; public float HudT = -1; public Vector3 HudFrom; }
        UltimoMiglio.UI.HudController _hud; bool _hudLooked;
        readonly List<Coin> _live = new(); readonly Stack<Transform> _pool = new();
        int _pickupSfx;

        public void Spawn(Vector3 at, double ln) => Emit(at, ln, true);
        public void Burst(Vector3 at, double ln) => Emit(at, ln, false);
        void Emit(Vector3 at, double ln, bool real)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt((float)(ln / LnPerCoin)), 1, 12);
            if (_live.Count + n > MergeAbove) n = 1;
            for (int i = 0; i < n; i++)
            {
                var t = _pool.Count > 0 ? _pool.Pop() : Instantiate(CoinPrefab, transform).transform;
                t.gameObject.SetActive(true); t.position = at + Vector3.up * 0.6f;
                _live.Add(new Coin { T = t, Ln = ln / n, Real = real, Vel = new Vector3(Random.Range(-1.5f, 1.5f), 3.5f, Random.Range(-1.5f, 1.5f)) });
            }
            UM_FxPlayer.Play(UM_FxPlayer.Fx.Coins, at);
            AudioManager.I?.Play(AudioManager.Sfx.CoinDrop);
        }
        void Update()
        {
            if (!GameRoot.Ready) return;
            var nina = GameRoot.I.Nina ? GameRoot.I.Nina.transform.position : Vector3.one * 1e5f;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var c = _live[i]; c.Age += Time.deltaTime;
                if (c.Age < 0.5f) { c.Vel.y -= 12f * Time.deltaTime; c.T.position += c.Vel * Time.deltaTime; if (c.T.position.y < 0.1f) { c.T.position = new Vector3(c.T.position.x, 0.1f, c.T.position.z); c.Vel = Vector3.zero; } }
                c.T.Rotate(0, 360 * Time.deltaTime, 0);
                var d = nina + Vector3.up * 0.8f - c.T.position;
                if (c.HudT < 0 && c.Age > 0.5f && (d.sqrMagnitude < MagnetRadius * MagnetRadius || !c.Real))
                {
                    c.T.position += d.normalized * Mathf.Min(d.magnitude, MagnetSpeed * Time.deltaTime);
                    if (d.sqrMagnitude < 0.09f && c.HudT < 0) { c.HudT = 0; c.HudFrom = c.T.position; }
                }
                if (c.HudT >= 0)
                {   // final leg: hop up and fly to the LN icon in the HUD, then credit + pulse
                    c.HudT += Time.deltaTime / 0.45f; float u = Mathf.Clamp01(c.HudT);
                    var cam = Camera.main; if (!_hudLooked) { _hudLooked = true; _hud = FindAnyObjectByType<UltimoMiglio.UI.HudController>(); }
                    Vector3 dst = c.HudFrom + Vector3.up * 3f;
                    if (cam && _hud) { var sp = _hud.CurrencyScreenPos(); dst = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 4f)); }
                    c.T.position = Vector3.Lerp(c.HudFrom, dst, u * u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 1.2f);
                    c.T.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, u);
                    if (u >= 1f)
                    {
                        if (_hud) _hud.PulseCurrency();
                        c.T.localScale = Vector3.one;
                        if (c.Real) GameRoot.I.Eco.CollectActive(c.Ln, c.T.position.ToWP());
                        if ((_pickupSfx++ & 1) == 0) AudioManager.I?.Play(AudioManager.Sfx.CoinPickup);
                        c.T.gameObject.SetActive(false); _pool.Push(c.T); _live.RemoveAt(i);
                    }
                }
            }
        }
    }
}
