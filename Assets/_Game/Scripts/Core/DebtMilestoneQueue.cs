namespace UltimoMiglio.Core
{
    /// <summary>#136/#203: coda delle tappe del debito (25/50/75%). Ogni tappa scatta una sola volta per salvataggio
    /// (bitmask nel campo <c>debt_ms</c> di EconomyState, quindi dentro il salvataggio um_save_v1); se un versamento ne supera più di una, escono in ordine a 1,0 s di distanza.
    /// Logica pura, senza allocazioni: la usa DebtMilestoneFeedback (Gameplay).</summary>
    public sealed class DebtMilestoneQueue
    {
        public const int Count = 3;
        public const double SpacingSec = 1.0;
        public const float LineSec = 3f;
        public static readonly string[] LineKeys = { "onb.debt.milestone1", "onb.debt.milestone2", "onb.debt.milestone3" };

        public int Mask { get; private set; }
        readonly int[] _pending = new int[Count];
        int _head, _len;
        double _nextAt = double.NegativeInfinity;

        public DebtMilestoneQueue(int mask = 0) { Mask = mask & ((1 << Count) - 1); }

        /// <summary>Coda dallo stato salvato: debt_ms più le tappe già contate da EconomyCore (salvataggi precedenti alla #136).</summary>
        public static DebtMilestoneQueue FromState(EconomyState s)
        {
            int mask = s.debt_ms;
            for (int i = 0; i < s.DebtMilestonesHit && i < Count; i++) mask |= 1 << i;
            return new DebtMilestoneQueue(mask);
        }

        public bool Reached(int i) => i >= 0 && i < Count && (Mask & (1 << i)) != 0;
        public bool HasPending => _len > 0;

        /// <summary>Segna la tappa come raggiunta e la mette in coda. False se già raggiunta (non riscatta).</summary>
        public bool Enqueue(int i)
        {
            if (i < 0 || i >= Count || Reached(i)) return false;
            Mask |= 1 << i;
            _pending[(_head + _len) % Count] = i; _len++;
            return true;
        }

        /// <summary>Prossima tappa da mostrare all'istante <paramref name="now"/> (s), rispettando 1,0 s tra una e l'altra.</summary>
        public bool TryNext(double now, out int i)
        {
            i = -1;
            if (_len == 0 || now < _nextAt) return false;
            i = _pending[_head]; _head = (_head + 1) % Count; _len--;
            _nextAt = now + SpacingSec;
            return true;
        }
    }
}
