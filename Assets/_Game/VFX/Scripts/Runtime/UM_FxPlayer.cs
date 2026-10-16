// Ultimo Miglio - player FX con pool (nessuna allocazione a runtime dopo Awake).
// UM_FxPlayer.Play(Fx.PackagePickup, pos) · UM_FxPlayer.FlyCoins(start, hudRect, 5, onEach, onAll)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_FxPlayer : MonoBehaviour
    {
        public enum Fx { PackagePop, Coins, Upgrade, PackagePickup, PackageDeliver, UpgradeComplete, Dust, Purchase }
        [Header("Prefab (Assets/_Game/VFX/Prefabs)")]
        public GameObject packagePop, coins, upgrade, packagePickup, packageDeliver, upgradeComplete, purchase;
        public GameObject coinFlyer;
        [Range(1, 8)] public int poolSize = 4;
        [Range(4, 24)] public int coinFlyerPool = 12;
        [Tooltip("Camera di gioco; vuoto = Camera.main")] public Camera worldCamera;
        [Tooltip("Pausa tra una moneta e la successiva (s)")] public float coinStagger = 0.06f;

        static UM_FxPlayer _i;
        public static UM_FxPlayer Instance => _i;
        readonly Dictionary<Fx, Queue<ParticleSystem>> _pools = new();
        readonly List<UM_CoinFlight> _flyers = new();

        void Awake()
        {
            _i = this;
            Fill(Fx.PackagePop, packagePop); Fill(Fx.Coins, coins); Fill(Fx.Upgrade, upgrade);
            Fill(Fx.PackagePickup, packagePickup); Fill(Fx.PackageDeliver, packageDeliver); Fill(Fx.UpgradeComplete, upgradeComplete); Fill(Fx.Purchase, purchase);
            if (coinFlyer)
            {
                int n = Mathf.Max(4, Mathf.RoundToInt(coinFlyerPool * UM_QualityTier.ParticleScale));
                for (int k = 0; k < n; k++) { var g = Instantiate(coinFlyer, transform); g.SetActive(false); _flyers.Add(g.GetComponent<UM_CoinFlight>()); }
            }
        }
        void Fill(Fx k, GameObject prefab)
        {
            var q = new Queue<ParticleSystem>();
            if (prefab) for (int n = 0; n < poolSize; n++) { var g = Instantiate(prefab, transform); g.SetActive(false); q.Enqueue(g.GetComponent<ParticleSystem>()); }
            _pools[k] = q;
        }

        /// Burst one-shot in una posizione del mondo.
        public static void Play(Fx k, Vector3 pos)
        {
            if (!_i || !_i._pools.TryGetValue(k, out var q) || q.Count == 0) return;
            var ps = q.Dequeue(); q.Enqueue(ps);
            ps.transform.position = pos;
            ps.gameObject.SetActive(true);
            ps.Clear(true); ps.Play(true);
        }

        /// Monete volanti verso un elemento HUD uGUI (Canvas Screen Space Overlay o Camera).
        public static int FlyCoins(Vector3 worldStart, RectTransform hudTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null)
        {
            if (!_i || !hudTarget) return 0;
            var canvas = hudTarget.GetComponentInParent<Canvas>();
            Camera uiCam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return FlyCoinsToScreen(worldStart, () => RectTransformUtility.WorldToScreenPoint(uiCam, hudTarget.position), count, onEachArrive, onAllArrived);
        }

        /// Monete verso un punto schermo in pixel (origine in basso a sinistra), es. HUD UI Toolkit:
        /// () => { var p = el.worldBound.center; return new Vector2(p.x * scale, Screen.height - p.y * scale); }
        public static int FlyCoinsToScreen(Vector3 worldStart, Func<Vector2> screenPoint, int count = 1, Action onEachArrive = null, Action onAllArrived = null)
        {
            if (!_i) return 0;
            var cam = _i.worldCamera ? _i.worldCamera : Camera.main;
            if (!cam) return 0;
            float depth = Vector3.Dot(worldStart - cam.transform.position, cam.transform.forward) * 0.5f; // a metà strada verso la camera: non passa dietro i muri
            return FlyCoinsTo(worldStart, () => { var s = screenPoint(); return cam.ScreenToWorldPoint(new Vector3(s.x, s.y, depth)); }, count, onEachArrive, onAllArrived);
        }

        /// Monete verso un punto del mondo (fisso o mobile, es. la Cassa o Nina).
        public static int FlyCoinsTo(Vector3 worldStart, Vector3 worldTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null)
            => FlyCoinsTo(worldStart, () => worldTarget, count, onEachArrive, onAllArrived);

        /// Versione generale: il bersaglio è rivalutato ogni frame. Ritorna quante monete sono partite (limitate dal pool;
        /// le monete in eccesso arrivano subito come callback, così il conteggio economico resta corretto).
        public static int FlyCoinsTo(Vector3 worldStart, Func<Vector3> worldTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null)
        {
            if (!_i || count <= 0) { for (int k = 0; k < count; k++) onEachArrive?.Invoke(); onAllArrived?.Invoke(); return 0; }
            int launched = 0, remaining = count;
            void Arrived() { onEachArrive?.Invoke(); if (--remaining == 0) onAllArrived?.Invoke(); }
            foreach (var f in _i._flyers)
            {
                if (launched >= count) break;
                if (f.gameObject.activeSelf) continue;
                f.Launch(worldStart, worldTarget, launched * _i.coinStagger, Arrived); launched++;
            }
            for (int k = launched; k < count; k++) Arrived();
            return launched;
        }
    }
}
