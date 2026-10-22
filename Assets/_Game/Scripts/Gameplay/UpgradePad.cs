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
        bool _paying, _waitExit;
        /// #240: true dopo un acquisto finché Kaia non esce dal pad (= _waitExit della #110): anello, freccia e prezzo spenti.
        public bool IsArmedOff => _waitExit; Renderer[] _renderers; Collider _col; int _avail = -1;
        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true); _col = GetComponent<Collider>();
            // Riferimento perso se la scena è stata generata con il prefab dell'anello rotto (m_Script fileID 0): ricollega dal figlio
            if (!FillRing) FillRing = GetComponentInChildren<UM_PadFillRing>(true);
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
        /// <summary>#171: il pad si attiva quando i piedi (pivot) di Kaia sono entro il raggio del disco visibile, distanza orizzontale dal centro.
        /// Il raggio è letto dal disco: scala reale dell'anello dell'InteractionPoint (mesh Ø 1 m), altrimenti Radius × VisualScale o il figlio "Pad".</summary>
        protected override bool PerDistanza => true;
        protected override bool DentroPerDistanza(NinaController n)
        {
            return FeetOnDisc(n.transform.position);
        }
        /// <summary>#171/#240: unica condizione dentro/fuori: piedi (pivot) entro il disco. La usano attivazione, _waitExit (#110) e il ritorno attivo del pad spento.</summary>
        public bool FeetOnDisc(Vector3 p)
        {
            if (!_col || !_col.enabled) return false; // pad non disponibile (costo infinito / non sbloccato): mai dentro
            var c = DiscCenter; float dx = p.x - c.x, dz = p.z - c.z;
            float d2 = dx * dx + dz * dz, d = Mathf.Sqrt(d2);
            float r = RaggioVerso(d > 1e-4f ? dx / d : 0f, d > 1e-4f ? dz / d : 0f, Occupied); // Occupied: isteresi d'uscita sul lato camera
            return d2 <= r * r;
        }
        /// <summary>#171 (Producer): conta ciò che vede il giocatore. Con la camera da sud il corpo di Kaia copre l'anello a schermo anche con i
        /// piedi appena fuori, quindi verso la camera il raggio cresce fino a <see cref="TolleranzaCamera"/> (pieno nella direzione della camera,
        /// 0 di lato e sul lato opposto). La direzione è la camera proiettata sul piano: nessun lato fisso. Zaino–Scarpe a 1,85 m: l'allargamento
        /// è verso sud, non verso il pad vicino.</summary>
        [Tooltip("#171: metri in più verso la camera (corpo di Kaia sopra l'anello a schermo)")] public float TolleranzaCamera = 0.3f;
        [Tooltip("#171: metri in più per uscire, solo sul lato camera (isteresi); negli altri lati resta il disco")] public float IsteresiUscita = 0.05f;
        static Transform _cam;
        /// <summary>#171: raggio d'attivazione nella direzione orizzontale (ux, uz) dal centro del disco. Lato camera: r + (TolleranzaCamera
        /// [+ IsteresiUscita se in uscita]) × cos(angolo dalla camera); di lato e sul lato opposto resta il raggio del disco (0,70). La camera è quella
        /// reale (Camera.main, rotazione #205 compresa), letta a ogni chiamata dal transform in cache.</summary>
        public float RaggioVerso(float ux, float uz, bool inUscita = false)
        {
            float r = DiscRadius;
            if (TolleranzaCamera <= 0f) return r;
            if (!_cam) { var cm = Camera.main; if (!cm) return r; _cam = cm.transform; } // in cache: Camera.main solo se manca
            var f = _cam.forward; f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) { f = _cam.up; f.y = 0f; } // camera a picco: l'alto dello schermo
            if (f.sqrMagnitude < 1e-4f) return r;
            f.Normalize();
            float verso = -(ux * f.x + uz * f.z); // 1 = dritto verso la camera
            return verso > 0f ? r + (TolleranzaCamera + (inUscita ? IsteresiUscita : 0f)) * verso : r;
        }
        InteractionPoint _ip; bool _ipCercato; Transform _disc;
        InteractionPoint Ip { get { if (!_ipCercato) { _ipCercato = true; _ip = GetComponent<InteractionPoint>(); } return _ip; } }
        /// <summary>#171: centro del disco visibile (anello dell'InteractionPoint, o il pad stesso).</summary>
        public Vector3 DiscCenter => Ip && Ip.RingVisual ? Ip.RingVisual.position : transform.position;
        /// <summary>#171: raggio del disco visibile, in metri (scala del mondo inclusa).</summary>
        public float DiscRadius
        {
            get
            {
                if (Ip && Ip.RingVisual) { var sc = Ip.RingVisual.lossyScale; return 0.5f * Mathf.Min(sc.x, sc.z); } // mesh dell'anello Ø 1 m a scala 1
                if (Ip) return Ip.Radius * InteractionPoint.VisualScale;
                if (!_disc) _disc = transform.Find("Pad");
                return _disc ? 0.5f * Mathf.Min(_disc.lossyScale.x, _disc.lossyScale.z) : 0.7f;
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
            if (done) { SetWaitExit(true); StopLoop(); AudioManager.I?.Play(AudioManager.Sfx.UpgradeComplete); UM_FxPlayer.Play(UM_FxPlayer.Fx.Upgrade, transform.position); UM_FxPlayer.Play(UM_FxPlayer.Fx.UpgradeComplete, transform.position); UM_FxPlayer.Play(UM_FxPlayer.Fx.Purchase, transform.position); Completed?.Invoke(Kind); Nina.Cheer(); }
        }
        protected override void OnZoneExit() { base.OnZoneExit(); StopLoop(); SetWaitExit(false); }
        /// #240: unico punto in cui cambia _waitExit, così la grafica segue sempre il blocco della #110.
        void SetWaitExit(bool v)
        {
            _waitExit = v;
            if (FillRing) FillRing.SetOff(v);
            foreach (var ip in GetComponentsInChildren<InteractionPoint>(true)) if (ip && ip.Pad == this) ip.SetPadOff(v);
        }
        /// Solo test: simula l'acquisto completato (done) e l'uscita di Kaia senza GameRoot.
        public void TestCompletePurchase() => SetWaitExit(true);
        /// Solo test: piedi di Kaia in p. Fuori dal disco = uscita (come ProximityZone.Update con PerDistanza), dentro = nessun cambio.
        public void TestFeetAt(Vector3 p) { if (!FeetOnDisc(p)) OnZoneExit(); }
        void StopLoop() { if (_paying) { _paying = false; AudioManager.I?.StopUpgradeLoop(); } }
    }
}
