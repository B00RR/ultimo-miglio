using System;

// DTOs mirroring data/ch1/*.json exactly (camelCase field names = JSON keys). Engine-free; parsed with
// UnityEngine.JsonUtility in Gameplay/Editor. Generic Track<T> avoided: concrete *Track classes for JsonUtility safety.
namespace UltimoMiglio.Core.Data
{
    [Serializable] public class EconomyData { public double parcelValueLn, parcelValueGrowthPerChapter, customersPerMin, customerPatienceSec; public bool customersAtNight; public ComboData combo; public double insegnaValueBonusPerLevel, reputationVipValueBonusPerLevel, playerSpendShare; public NinaData nina; public RewardsData rewards; }
    // Ricompense intermedie cap. 1 (PR #75, issue #103/#104)
    [Serializable] public class RewardsData { public string source; public ParcelMilestone[] parcelMilestones; public string parcelMilestonesNote; public DebtMilestone[] debtMilestones; public double cassaFirstVisitTipLn; public NextPadArrowData nextPadArrow; }
    [Serializable] public class ParcelMilestone { public int parcels; public double bonusLn; }
    [Serializable] public class DebtMilestone { public double share; public string reward; public int amount; public string itemId; }
    [Serializable] public class NextPadArrowData { public double fromSec; public string[] candidates; public string rule; }
    [Serializable] public class ComboData { public double stepPerCustomer, maxMultiplier, resetGapSec, resetGapSecAssist; }
    [Serializable] public class NinaData { public double baseSpeedMps, fullStackSlowdown; public int startStackCapacity; }

    [Serializable] public class DebtData { public double trancheLn, requiredPadLn; public int dueCalendarDay, yearDays, tranchesTotal; public double windowActiveMin, minWindowFromPrevPaymentActiveMin, calendarDaysPerCycle; public EarlyData earlyPayment; public ProrogaData proroga; public LateData late; public AssistData assist; }
    [Serializable] public class EarlyData { public int minCyclesEarly, reputationLevels, bulloni; }
    [Serializable] public class ProrogaData { public int tokensGrantedAtActStart, tokensMax; public double extensionActiveMin; public bool appliesToCurrentTrancheOnly, autoUse, purchasable; }
    [Serializable] public class LateData { public double trancheSurcharge, pedaggioCoinPenalty; public bool pedaggioAppliesToOffline; }
    [Serializable] public class AssistData { public double trancheDiscount; public int trancheDiscountFromChapter; public double calendarDaysPerCycle, windowActiveMin, comboResetGapSec, offlineShareMax; }

    [Serializable] public class ZainoLevel { public int level, stackCapacity; public double costLn; }
    [Serializable] public class ScarpeLevel { public int level; public double speedBonus, costLn; }
    [Serializable] public class InsegnaLevel { public int level; public double customerValueBonus, costLn; }
    [Serializable] public class ZainoTrack { public ZainoLevel[] levels; public string costNote; }
    [Serializable] public class ScarpeTrack { public ScarpeLevel[] levels; }
    [Serializable] public class InsegnaTrack { public InsegnaLevel[] levels; }
    [Serializable] public class UpgradesData { public ZainoTrack zaino; public ScarpeTrack scarpe; public InsegnaTrack insegna; }

    [Serializable] public class NonnoLevelData { public int level; public double restockPpm, selfServicePpm, costLn; }
    [Serializable] public class NonnoData { public double unlockAtOnboardingSec, hireCostLn; public string padLabel; public double restockPpmBase, restockPpmPerLevel, selfServicePpmBase, selfServicePpmPerLevel; public int maxLevel; public double upgradeCostBaseLn, upgradeCostGrowth; public NonnoLevelData[] levels; }
    [Serializable] public class RobotsData { public NonnoData nonno; public bool ninaCanAlwaysRestockByHand; }

    [Serializable] public class DistancesData { public double arriviToShelves, shelvesToBancone; }
    [Serializable] public class TimingsData { public double zoneDwellBeforeAction, pickPerParcel, dropPerParcel, servePerCustomer, coinPickupPerTrip, padFillMax; }
    [Serializable] public class PadData { public string id; public double costLn, unlockAtOnboardingSec; public string costRef; }
    [Serializable] public class ZonesData { public DistancesData distancesM; public TimingsData timingsSec; public PadData[] pads; public bool padPaymentPersistent; public string[] zoneIds; }

    [Serializable] public class TurnoDiNotteData { public int unlockChapter; public double costLn, cassaCapacityShareOfTranche; }
    [Serializable] public class TotemData { public double speedBonus, stackBonus, durationSec; }
    [Serializable] public class TotemsData { public int unlockChapter, maxPerSession, maxPerDay; public TotemData caffeTurbo, zainoGonfio; }
    [Serializable] public class OfflineData { public string collectionMethod; public bool modalSummary; public double rateShareOfRobotThroughput, cassaCapacityShareOfTranche, cassaCapacityLn, offlineShareOfChapterCoinsMax, offlineShareOfChapterCoinsMaxAssist; public int adDoubleMaxPerDay; public bool adDoubleAlsoWithBulloni, pedaggioApplies; public TurnoDiNotteData turnoDiNotte; public TotemsData boostTotems; public string offlineAssistNote; }

    [Serializable] public class DayCycleData { public double cycleDurationSec, dayDurationSec, sunsetTransitionSec, nightDurationSec; public int nightFromChapter; public double nightDurationSecFromCh6, dayDurationSecFromCh6, calendarDaysPerCycle; public bool timeAdvancesOnlyWhileActive; }

    /// <summary>All chapter data. Turno di notte (cap 3) and totems (cap 4) are parsed but locked in cap 1.</summary>
    [Serializable] public class ChapterData
    {
        public int chapter = 1;
        public EconomyData economy; public DebtData debt; public UpgradesData upgrades; public RobotsData robots;
        public ZonesData zones; public OfflineData offline; public DayCycleData daycycle;
    }
}
