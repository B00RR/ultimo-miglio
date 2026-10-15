using System;
using System.Linq;
using UltimoMiglio.Core.Data;

namespace UltimoMiglio.Core
{
    /// <summary>Flattened view of ChapterData for EconomyCore. No balance numbers live here: everything comes from data/ch1/*.json.</summary>
    [Serializable]
    public sealed class EconomyConfig
    {
        public int Chapter;
        public double ParcelValueLn, CustomersPerMin, CustomerPatienceSec;
        public double ComboStep, ComboMax, ComboResetGapSec;
        public double InsegnaBonusPerLevel, PlayerSpendShare, TrancheLn;
        public double NinaBaseSpeed, FullStackSlowdown;
        public int[] ZainoCapacity; public double[] ZainoCost, ScarpeBonus, ScarpeCost, InsegnaBonus, InsegnaCost;
        public double NonnoHireCost, NonnoUnlockSec;
        public double[] NonnoRestockPpm, NonnoSelfServicePpm, NonnoCost; public int NonnoMaxLevel;
        public double OfflineRateShare, CassaCapacityShareOfTranche, OfflineShareOfChapterMax; public int AdDoubleMaxPerDay;
        public double MaxOfflineSec = 7 * 24 * 3600; // tech guard (not balance)
        public double ZoneDwellSec, ZoneTickSec, ServeSec, PadFillMaxSec, PickPerParcelSec, DropPerParcelSec;
        public double CycleSec, CalendarDaysPerCycle;

        public double CassaCapacity => TrancheLn * CassaCapacityShareOfTranche;
        public double NonnoUpgradeCost(int toLevel) => NonnoCost[toLevel];

        public static EconomyConfig From(ChapterData d)
        {
            var e = d.economy; var n = d.robots.nonno; var t = d.zones.timingsSec; var u = d.upgrades;
            return new EconomyConfig
            {
                Chapter = d.chapter,
                ParcelValueLn = e.parcelValueLn * Math.Pow(e.parcelValueGrowthPerChapter, d.chapter - 1),
                CustomersPerMin = e.customersPerMin, CustomerPatienceSec = e.customerPatienceSec,
                ComboStep = e.combo.stepPerCustomer, ComboMax = e.combo.maxMultiplier, ComboResetGapSec = e.combo.resetGapSec,
                InsegnaBonusPerLevel = e.insegnaValueBonusPerLevel, PlayerSpendShare = e.playerSpendShare,
                NinaBaseSpeed = e.nina.baseSpeedMps, FullStackSlowdown = e.nina.fullStackSlowdown,
                TrancheLn = d.debt.trancheLn,
                ZainoCapacity = u.zaino.levels.Select(l => l.stackCapacity).ToArray(), ZainoCost = u.zaino.levels.Select(l => l.costLn).ToArray(),
                ScarpeBonus = u.scarpe.levels.Select(l => l.speedBonus).ToArray(), ScarpeCost = u.scarpe.levels.Select(l => l.costLn).ToArray(),
                InsegnaBonus = u.insegna.levels.Select(l => l.customerValueBonus).ToArray(), InsegnaCost = u.insegna.levels.Select(l => l.costLn).ToArray(),
                NonnoHireCost = n.hireCostLn, NonnoUnlockSec = n.unlockAtOnboardingSec, NonnoMaxLevel = n.maxLevel,
                NonnoRestockPpm = n.levels.Select(l => l.restockPpm).ToArray(), NonnoSelfServicePpm = n.levels.Select(l => l.selfServicePpm).ToArray(), NonnoCost = n.levels.Select(l => l.costLn).ToArray(),
                OfflineRateShare = d.offline.rateShareOfRobotThroughput, CassaCapacityShareOfTranche = d.offline.cassaCapacityShareOfTranche,
                OfflineShareOfChapterMax = d.offline.offlineShareOfChapterCoinsMax, AdDoubleMaxPerDay = d.offline.adDoubleMaxPerDay,
                ZoneDwellSec = t.zoneDwellBeforeAction, ZoneTickSec = t.pickPerParcel, ServeSec = t.servePerCustomer, PadFillMaxSec = t.padFillMax,
                PickPerParcelSec = t.pickPerParcel, DropPerParcelSec = t.dropPerParcel,
                CycleSec = d.daycycle.cycleDurationSec, CalendarDaysPerCycle = d.daycycle.calendarDaysPerCycle,
            };
        }
    }
}
