using System;
using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#136/#203: feedback delle tappe del debito. Ascolta EconomyCore.RewardGranted("debt") (evento, niente polling):
    /// frase onb.debt.milestone1-3 nel fumetto di Oma Tilde per 3 s, sfx_debt_stamp_01..03 e ricompensa nello stesso istante.
    /// Nessuna barra del debito nell'HUD. Riferimenti in cache, nessuna allocazione per frame.</summary>
    public sealed class DebtMilestoneFeedback : MonoBehaviour
    {
        /// <summary>Hook visivo della ricompensa (0 = timbro sulla card del debito, 1 = +1 Reputazione, 2 = foto di Oma Tilde sul muro).</summary>
        public static event Action<int> MilestoneShown;

        DebtMilestoneQueue _q;
        EconomyCore _eco;
        UltimoMiglio.UI.HudController _hud;
        TutorialManager _tut;

        void Start()
        {
            if (!GameRoot.Ready) { enabled = false; return; }
            _eco = GameRoot.I.Eco;
            int mask = PlayerPrefs.GetInt(DebtMilestoneQueue.PrefKey, 0);
            for (int i = 0; i < _eco.S.DebtMilestonesHit && i < DebtMilestoneQueue.Count; i++) mask |= 1 << i; // salvataggi precedenti: già raggiunte
            _q = new DebtMilestoneQueue(mask);
            _hud = FindAnyObjectByType<UltimoMiglio.UI.HudController>();
            _tut = FindAnyObjectByType<TutorialManager>();
            _eco.RewardGranted += OnReward;
        }

        void OnDestroy() { if (_eco != null) _eco.RewardGranted -= OnReward; }

        void OnReward(string kind, int i, double ln)
        {
            if (kind != "debt" || !_q.Enqueue(i)) return;
            PlayerPrefs.SetInt(DebtMilestoneQueue.PrefKey, _q.Mask); PlayerPrefs.Save();
        }

        void Update()
        {
            if (_q == null || !_q.HasPending) return;
            if (!_q.TryNext(Time.unscaledTimeAsDouble, out int i)) return;
            string key = DebtMilestoneQueue.LineKeys[i];
            string txt = _tut ? _tut.Hint(key) : null;
            if (_hud) _hud.ShowAdaNote(DisplayNames.Apply(string.IsNullOrEmpty(txt) ? key : txt), sticky: false, seconds: DebtMilestoneQueue.LineSec);
            if (AudioManager.I) AudioManager.I.PlayDebtStamp(i);
            MilestoneShown?.Invoke(i);
            int pct = (int)Math.Round(Math.Min(1.0, _eco.DebtShare) * 100);
            Debug.Log($"[ONB] debtMilestone id={i + 1} pct={pct} t={TutorialManager.OnbTime:0.#}");
        }
    }
}
