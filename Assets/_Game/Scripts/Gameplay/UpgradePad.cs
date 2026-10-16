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
        public UltimoMiglio.UI.World.UpgradePadCard Card; public UM_PadFillRing FillRing;
        Transform _padVis; float _pulse; Vector3 _padBase = Vector3.zero;
        void LateUpdate() { if (!_padVis) return; if (_padBase == Vector3.zero) _padBase = _padVis.localScale; _pulse = Mathf.MoveTowards(_pulse, 0, Time.deltaTime * 6f); _padVis.localScale = _padBase * (1f + 0.08f * Mathf.Sin(_pulse * Mathf.PI)); }
        // #110: dopo un acquisto il pad aspetta che Nina esca prima di pagare il livello successivo (_waitExit)
        bool _paying, _waitExit; Renderer[] _renderers; Collider _col; int _avail = -1;
        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true); _col = GetComponent<Collider>();
            EnsurePriceVisible();
        }
        void OnEnable() { EnsurePriceVisible(); }
        /// <summary>#110: la scheda pad è nascosta dall'HUD minimo (QA #41). La pillola prezzo resta accesa anche con Nina sopra,
        /// anche se l'InteractionPoint viene collegato dopo l'Awake o il pad viene riacceso (reload / HudReveal).</summary>
        public void EnsurePriceVisible()
        {
            foreach (var ip in GetComponentsInChildren<InteractionPoint>(true))
            {
                if (!ip) continue;
                if (!ip.Pad) ip.Pad = this;
                if (ip.Pad == this) ip.HasCard = false;
            }
        }
        protected override void OnZoneStart() { base.OnZoneStart(); if (GameRoot.Ready) GameRoot.I.Eco.BeginPadStay(Kind); }
        protected override void OnIdleUpdate()
        {
            EnsurePriceVisible();
            var eco = GameRoot.I.Eco; bool avail = !double.IsInfinity(eco.Cost(Kind)) && GameRoot.I.ActiveTime >= UnlockAtActiveSec;
            if (_avail != (avail ? 1 : 0)) { _avail = avail ? 1 : 0; foreach (var r in _renderers) if (r) r.enabled = avail; if (!avail) ReleaseNina(); _col.enabled = avail; } // was 2 GetComponent* per pad per frame
            if (Ring && !Nina) Ring.SetFill((float)eco.PadFill(Kind));
            if (FillRing) { FillRing.gameObject.SetActive(avail); if (avail) FillRing.SetProgress((float)eco.PadFill(Kind)); }
            var icon = GetComponent<UM_PadIcon>(); if (icon) icon.SetPlayerNear(Nina);
            if (Card) { Card.gameObject.SetActive(avail); if (avail) Card.SetProgress(eco.S.PadPaid[(int)Kind], eco.Cost(Kind)); }
            var tag = GetComponent<UltimoMiglio.UI.World.PadTag>(); if (tag) tag.enabled = false; // QA #41: no floating price chips
        }
        protected override void OnZoneTick(float t)
        {
            var eco = GameRoot.I.Eco;
            if (_waitExit) return; // #110: livello successivo solo dopo essere usciti e rientrati
            if (eco.S.Wallet <= 0) { StopLoop(); return; }
            double prima = eco.S.Wallet;
            bool done = eco.PadTick(Kind, t);
            if (!done && eco.S.Wallet == prima) { StopLoop(); return; } // #110: sosta chiusa, non scalare il livello successivo
            if (!_paying) { _paying = true; AudioManager.I?.StartUpgradeLoop(); }
            if (!_padVis) _padVis = transform.Find("Pad"); if (_padVis) { _pulse = 1f; }
            if (Ring) Ring.SetFill((float)eco.PadFill(Kind));
            if (done) { _waitExit = true; StopLoop(); AudioManager.I?.Play(AudioManager.Sfx.UpgradeComplete); UM_FxPlayer.Play(UM_FxPlayer.Fx.Upgrade, transform.position); UM_FxPlayer.Play(UM_FxPlayer.Fx.UpgradeComplete, transform.position); UM_FxPlayer.Play(UM_FxPlayer.Fx.Purchase, transform.position); Completed?.Invoke(Kind); Nina.Cheer(); }
        }
        protected override void OnZoneExit() { base.OnZoneExit(); StopLoop(); _waitExit = false; }
        void StopLoop() { if (_paying) { _paying = false; AudioManager.I?.StopUpgradeLoop(); } }
    }
}
