using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Bancone: serves the head customer once per ServeSec using a parcel of the requested colour.</summary>
    public sealed class Bancone : ProximityZone
    {
        public CustomerQueue Queue; public CoinSpawner Coins; public float ServeSec = 1f; float _serve;
        protected override void OnZoneStart() { _serve = 0; }
        protected override void OnZoneTick(float t)
        {
            var c = Queue.Head; if (c == null || !c.AtCounter) return;
            if ((_serve += TickSec) < ServeSec) return; _serve = 0;
            var dst = c.transform.position + Vector3.up * 1.0f; if (!Nina.Stack.TryPop(out _, c.Wants, dst) && !Nina.Stack.TryPop(out _, null, dst)) return;
            Serve(c);
        }
        public void Serve(Customer c)
        {
            double ln = GameRoot.I.Eco.Serve(GameRoot.I.ActiveTime);
            UltimoMiglio.VFX.UM_FxPlayer.Play(UltimoMiglio.VFX.UM_FxPlayer.Fx.PackageDeliver, c.transform.position + Vector3.up);
            Queue.Dequeue(c, true); Coins.Spawn(transform.position + Vector3.right * 0.8f, ln);
            AudioManager.I?.Play(AudioManager.Sfx.Served); if (Nina) Nina.Deliver();
            var combo = GameRoot.I.Eco.S.Combo; if (combo >= 2.0 - 1e-6) AudioManager.I?.Play(AudioManager.Sfx.ComboX2); else if (System.Math.Abs(combo - 1.5) < 0.05) AudioManager.I?.Play(AudioManager.Sfx.ComboX15);
        }
    }
}
