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
            var c = Queue.Head; if (!InAttesa(c)) { _serve = 0; return; } // #188: niente cliente in attesa = niente vendita (e il timer non si carica a vuoto)
            if ((_serve += TickSec) < ServeSec) return; _serve = 0;
            if (Nina.Stack != null && Nina.Stack.Count == 0) TryPullFromShelf();
            var dst = c.transform.position + Vector3.up * 1.0f;
            if (Nina.Stack == null || (!Nina.Stack.TryPop(out _, c.Wants, dst) && !Nina.Stack.TryPop(out _, null, dst))) return;
            Serve(c);
        }

        /// <summary>#188: si vende solo a un cliente presente (attivo, in testa alla fila, non in uscita) e fermo al posto davanti al Bancone.</summary>
        public bool InAttesa(Customer c) =>
            Queue && c != null && c.isActiveAndEnabled && !c.Leaving && Queue.Waiting > 0 && Queue.Head == c && c.AtCounter
            && (c.transform.position - Queue.Slot(0)).sqrMagnitude < 0.36f;

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

        int _clientiDiFila;
        public void Serve(Customer c)
        {
            if (!GameRoot.Ready || !InAttesa(c)) return; // #188: anche NONNO vende solo a un cliente in attesa
            double ln = GameRoot.I.Eco.Serve(GameRoot.I.ActiveTime);
            // #101/#106: moltiplicatore combo applicato a questo cliente (1 = combo azzerata → nuova serie). Il tutorial mostra "+N" (N = clienti di fila)
            // e al primo moltiplicatore ≥ 1,2 apre il gate comboReached.
            double mult = GameRoot.I.Eco.CustomerValue > 0 ? ln / GameRoot.I.Eco.CustomerValue : 1;
            _clientiDiFila = mult > 1 + 1e-6 ? _clientiDiFila + 1 : 1;
            if (TutorialManager.I) TutorialManager.I.NotifyCombo(_clientiDiFila, (float)mult);
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
