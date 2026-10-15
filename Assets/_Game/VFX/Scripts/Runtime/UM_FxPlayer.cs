// Ultimo Miglio - tiny pooled FX player. UM_FxPlayer.Play(UM_FxPlayer.Fx.Coins, pos)
using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_FxPlayer : MonoBehaviour
    {
        public enum Fx { PackagePop, Coins, Upgrade }
        public GameObject packagePop, coins, upgrade; // prefabs from Assets/_Game/VFX/Prefabs
        [Range(1, 8)] public int poolSize = 4;
        static UM_FxPlayer _i;
        readonly Dictionary<Fx, Queue<ParticleSystem>> _pools = new();

        void Awake()
        {
            _i = this;
            Fill(Fx.PackagePop, packagePop); Fill(Fx.Coins, coins); Fill(Fx.Upgrade, upgrade);
        }
        void Fill(Fx k, GameObject prefab)
        {
            var q = new Queue<ParticleSystem>();
            if (prefab) for (int n = 0; n < poolSize; n++) { var g = Instantiate(prefab, transform); g.SetActive(false); q.Enqueue(g.GetComponent<ParticleSystem>()); }
            _pools[k] = q;
        }
        public static void Play(Fx k, Vector3 pos)
        {
            if (!_i || !_i._pools.TryGetValue(k, out var q) || q.Count == 0) return;
            var ps = q.Dequeue(); q.Enqueue(ps); // round robin
            ps.transform.position = pos;
            ps.gameObject.SetActive(true);
            ps.Clear(true); ps.Play(true);
        }
    }
}
