using System;
using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#103 nextPadArrow (economy.json → rewards): da 480 s di gioco attivo indica il pad più economico tra i candidati
    /// (scarpe:2, nonno:2) se il portafoglio ne copre il costo. Sparisce all'acquisto o dopo VisibleSec, e non torna per quel pad.
    /// Solo logica + evento: la UI (freccia a bordo schermo) si aggancia a <see cref="Changed"/>; niente asset nuovi.</summary>
    public sealed class NextPadArrow : MonoBehaviour
    {
        public float VisibleSec = 20f;
        public static event Action<UpgradePad> Changed; // null = nascondi
        public UpgradePad Target { get; private set; }
        public static NextPadArrow I { get; private set; }
        float _shownAt; int _lastLevel; bool _done; UpgradeKind _kind;

        void Awake() { I = this; }
        void Update()
        {
            if (_done || !GameRoot.Ready) return;
            var eco = GameRoot.I.Eco;
            if (Target)
            {
                bool bought = eco.NextLevel(_kind) != _lastLevel;
                if (bought || Time.time - _shownAt >= VisibleSec) { Hide(bought ? "acquisto" : "timeout"); _done = true; }
                return;
            }
            var k = eco.NextPadArrowTarget(); if (k == null) return;
            foreach (var p in FindObjectsByType<UpgradePad>(FindObjectsSortMode.None))
                if (p.Kind == k.Value) { Show(p, k.Value, eco.NextLevel(k.Value)); break; }
        }
        void Show(UpgradePad p, UpgradeKind k, int lvl)
        {
            Target = p; _kind = k; _lastLevel = lvl; _shownAt = Time.time;
            Debug.Log($"[REWARD] next_pad_arrow show pad={k} t={GameRoot.I.Eco.S.ActiveSec:0.0}");
            Changed?.Invoke(p);
        }
        void Hide(string why)
        {
            Debug.Log($"[REWARD] next_pad_arrow hide pad={_kind} why={why}");
            Target = null; Changed?.Invoke(null);
        }
    }
}
