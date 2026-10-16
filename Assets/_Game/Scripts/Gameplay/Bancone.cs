using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Bancone: serves the head customer once per ServeSec using a parcel from Nina's stack
    /// (or auto-pulled from a shelf if the stack is empty after depositing).</summary>
    public sealed class Bancone : ProximityZone
    {
        public CustomerQueue Queue; public CoinSpawner Coins; public float ServeSec = 1f; float _serve;
        protected override void OnZoneStart() { _serve = 0; }
        protected override void OnZoneTick(float t)
        {
            if (!Queue || !Nina) return;
            var c = Queue.Head; if (c == null || !c.AtCounter) return;
            if ((_serve += TickSec) < ServeSec) return; _serve = 0;
            if (Nina.Stack != null && Nina.Stack.Count == 0) TryPullFromShelf();
            var dst = c.transform.position + Vector3.up * 1.0f;
            if (Nina.Stack == null || (!Nina.Stack.TryPop(out _, c.Wants, dst) && !Nina.Stack.TryPop(out _, null, dst))) return;
            Serve(c);
        }

        void TryPullFromShelf()
        {
            if (Nina.Stack == null || Nina.Stack.Full) return;
            foreach (var sh in Object.FindObjectsByType<Shelf>(FindObjectsSortMode.None))
            {
                if (sh == null || sh.Count <= 0) continue;
                if (!sh.Take()) continue;
                Nina.Stack.TryPush(sh.Color);
                return;
            }
        }

        public void Serve(Customer c)
        {
            if (!GameRoot.Ready || c == null) return;
            double ln = GameRoot.I.Eco.Serve(GameRoot.I.ActiveTime);
            // Credit wallet immediately (40% spend share). Coins are visual-only so magnet/HUD fly can't soft-lock LN at 0.
            GameRoot.I.Eco.CollectActive(ln, transform.position.ToWP());
            UM_FxPlayer.Play(UM_FxPlayer.Fx.PackageDeliver, c.transform.position + Vector3.up);
            Queue.Dequeue(c, true);
            if (Coins) Coins.Burst(transform.position + Vector3.right * 0.8f, ln);
            AudioManager.I?.Play(AudioManager.Sfx.Served);
            if (Nina) Nina.Deliver();
            var combo = GameRoot.I.Eco.S.Combo;
            if (combo >= 2.0 - 1e-6) AudioManager.I?.Play(AudioManager.Sfx.ComboX2);
            else if (System.Math.Abs(combo - 1.5) < 0.05) AudioManager.I?.Play(AudioManager.Sfx.ComboX15);
        }
    }
}
