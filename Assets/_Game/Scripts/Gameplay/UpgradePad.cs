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
        public UltimoMiglio.UI.World.UpgradePadCard Card;
        bool _paying; Renderer[] _renderers; Collider _col; int _avail = -1;
        void Awake() { _renderers = GetComponentsInChildren<Renderer>(true); _col = GetComponent<Collider>(); }
        protected override void OnIdleUpdate()
        {
            var eco = GameRoot.I.Eco; bool avail = !double.IsInfinity(eco.Cost(Kind)) && GameRoot.I.ActiveTime >= UnlockAtActiveSec;
            if (_avail != (avail ? 1 : 0)) { _avail = avail ? 1 : 0; foreach (var r in _renderers) if (r) r.enabled = avail; _col.enabled = avail; } // was 2 GetComponent* per pad per frame
            if (Ring && !Nina) Ring.SetFill((float)eco.PadFill(Kind));
            if (Card) { Card.gameObject.SetActive(avail); if (avail) Card.SetProgress(eco.S.PadPaid[(int)Kind], eco.Cost(Kind)); }
        }
        protected override void OnZoneTick(float t)
        {
            var eco = GameRoot.I.Eco;
            if (eco.S.Wallet <= 0) { StopLoop(); return; }
            if (!_paying) { _paying = true; AudioManager.I?.StartUpgradeLoop(); }
            bool done = eco.PadTick(Kind, t);
            if (Ring) Ring.SetFill((float)eco.PadFill(Kind));
            if (done) { StopLoop(); AudioManager.I?.Play(AudioManager.Sfx.UpgradeComplete); UM_FxPlayer.Play(UM_FxPlayer.Fx.Upgrade, transform.position); Completed?.Invoke(Kind); Nina.Cheer(); }
        }
        protected override void OnZoneExit() { base.OnZoneExit(); StopLoop(); }
        void StopLoop() { if (_paying) { _paying = false; AudioManager.I?.StopUpgradeLoop(); } }
    }
}
