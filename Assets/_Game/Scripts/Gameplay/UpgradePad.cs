using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Upgrade/hire pad: pays in installments every 0.1 s with an accelerating curve, saved in EconomyState.PadPaid.</summary>
    public sealed class UpgradePad : ProximityZone
    {
        public UpgradeKind Kind; public GameObject ShowWhenAvailable; public float UnlockAtActiveSec;
        public System.Action<UpgradeKind> Completed;
        bool _paying;
        protected override void OnIdleUpdate()
        {
            var eco = GameRoot.I.Eco; bool avail = !double.IsInfinity(eco.Cost(Kind)) && GameRoot.I.ActiveTime >= UnlockAtActiveSec;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = avail;
            GetComponent<Collider>().enabled = avail;
            if (Ring && !Nina) Ring.SetFill((float)eco.PadFill(Kind));
        }
        protected override void OnZoneTick(float t)
        {
            var eco = GameRoot.I.Eco;
            if (eco.S.Wallet <= 0) { StopLoop(); return; }
            if (!_paying) { _paying = true; AudioManager.I?.StartUpgradeLoop(); }
            bool done = eco.PadTick(Kind, t);
            if (Ring) Ring.SetFill((float)eco.PadFill(Kind));
            if (done) { StopLoop(); AudioManager.I?.Play(AudioManager.Sfx.UpgradeComplete); UM_FxPlayer.Play(UM_FxPlayer.Fx.Upgrade, transform.position); Completed?.Invoke(Kind); }
        }
        protected override void OnZoneExit() { base.OnZoneExit(); StopLoop(); }
        void StopLoop() { if (_paying) { _paying = false; AudioManager.I?.StopUpgradeLoop(); } }
    }
}
