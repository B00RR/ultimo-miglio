using UltimoMiglio.Core.Data;

namespace UltimoMiglio.UI.Data
{
    /// <summary>
    /// Read-only view of the few numbers the HUD *displays* (never simulates). Backed by the Tech Lead's
    /// ChapterData (data/ch1/*.json → Ch1Data.asset via 'Ultimo Miglio/Data/Import Ch1 Data').
    /// No fallback numbers: unknown values are NaN / -1 and the HUD hides them.
    /// </summary>
    public interface IHudConfig
    {
        int Chapter { get; }
        /// <summary>Pedaggio share taken in mora (debt.late.pedaggioCoinPenalty, e.g. 0.15). NaN if unknown.</summary>
        float TollFraction { get; }
        int DaysPerYear { get; }
        int InstallmentsPerYear { get; }
        /// <summary>LN of installment k (1-based). Sprint 1: one tranche per chapter = debt.trancheLn.</summary>
        double InstallmentAmount(int k);
        /// <summary>Due calendar day of installment k: round(yearDays·k/tranchesTotal); k=1 uses debt.dueCalendarDay.</summary>
        int InstallmentDueDay(int k);
        double CassaCapacityLn { get; }
    }

    public sealed class ChapterDataHudConfig : IHudConfig
    {
        readonly ChapterData d;
        public ChapterDataHudConfig(ChapterData data) { d = data; }

        public int Chapter => d != null ? d.chapter : 0;
        public float TollFraction => d?.debt?.late != null ? (float)d.debt.late.pedaggioCoinPenalty : float.NaN;
        public int DaysPerYear => d?.debt != null && d.debt.yearDays > 0 ? d.debt.yearDays : -1;
        public int InstallmentsPerYear => d?.debt != null ? d.debt.tranchesTotal : 0;

        public double InstallmentAmount(int k) => d?.debt != null && k == Chapter ? d.debt.trancheLn : double.NaN;

        public int InstallmentDueDay(int k)
        {
            if (d?.debt == null) return -1;
            if (k == Chapter && d.debt.dueCalendarDay > 0) return d.debt.dueCalendarDay;
            if (DaysPerYear > 0 && InstallmentsPerYear > 0) return (int)System.Math.Round((double)DaysPerYear * k / InstallmentsPerYear, System.MidpointRounding.AwayFromZero);
            return -1;
        }

        public double CassaCapacityLn => d?.offline != null
            ? (d.offline.cassaCapacityLn > 0 ? d.offline.cassaCapacityLn : d.debt.trancheLn * d.offline.cassaCapacityShareOfTranche)
            : double.NaN;
    }
}
