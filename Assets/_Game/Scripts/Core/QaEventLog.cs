using System;
using System.Globalization;
using System.Text;

namespace UltimoMiglio.Core
{
    /// <summary>QA gameplay log, 08_TEST_PLAN_S1 §(c): one JSON object per line (jsonl).
    /// Line: {"t":unixMs,"sid":"..","ev":"serve","active_min":..,"day":..,"zaino":..,"scarpe":..,"insegna":..,"nonno":..,"eff_est":.., ...fields}
    /// Sinks (file in persistentDataPath/qa_log.jsonl + logcat tag "UMQA") are attached by the Gameplay layer.</summary>
    public sealed class QaEventLog
    {
        public event Action<string> Sink;
        readonly EconomyCore _eco; readonly string _sid; 
        public double TheoreticalPpm = 25; // sim reference (packages/min at eff 1.0, cap 1)

        public QaEventLog(EconomyCore eco, string sessionId) { _eco = eco; _sid = sessionId; eco.OnEvent += Log; }

        public double CalendarDay => _eco.S.ActiveSec / _eco.Cfg.CycleSec * _eco.Cfg.CalendarDaysPerCycle;
        public double EffEst => _eco.S.ActiveSec < 1 ? 0 : (_eco.S.CustomersServed / (_eco.S.ActiveSec / 60.0)) / TheoreticalPpm;

        public void Log(string ev, string fields)
        {
            var s = _eco.S; var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);
            sb.Append("{\"t\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
              .Append(",\"sid\":\"").Append(_sid).Append("\",\"ev\":\"").Append(ev).Append('"')
              .Append(",\"active_min\":").Append((s.ActiveSec / 60).ToString("0.000", ci))
              .Append(",\"day\":").Append(CalendarDay.ToString("0.00", ci))
              .Append(",\"zaino\":").Append(s.Zaino).Append(",\"scarpe\":").Append(s.Scarpe)
              .Append(",\"insegna\":").Append(s.Insegna).Append(",\"nonno\":").Append(s.NonnoLevel)
              .Append(",\"wallet\":").Append(s.Wallet.ToString("0.#", ci))
              .Append(",\"debt_paid\":").Append(s.DebtPaid.ToString("0.#", ci))
              .Append(",\"eff_est\":").Append(EffEst.ToString("0.000", ci));
            if (!string.IsNullOrEmpty(fields)) sb.Append(',').Append(fields);
            sb.Append('}');
            Sink?.Invoke(sb.ToString());
        }
    }
}
