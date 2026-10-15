# Dati Capitolo 1 (01_CORE_LOOP v3.3) - import in Unity 6

Sono JSON piatti con chiavi camelCase; l'unità di misura è nel nome della chiave (`Sec`, `Min`, `M`, `Mps`, `Ln`, `Ppm` = pacchi/min). Le frazioni sono decimali (0.15 = 15%).
Fonte: `01_CORE_LOOP.md` v3.3; verifica in `qa/econ_sim_v3.py`. Validazione: `python3 -m json.tool <file>`.

| File | ScriptableObject | Contenuto |
|---|---|---|
| economy.json | `EconomyConfig` | valore pacco, clienti/min, pazienza, combo, Nina base |
| debt.json | `DebtConfig` | tranche, finestra, Proroga, mora/Pedaggio, Assist |
| upgrades.json | `UpgradeTrackConfig` ×3 | Zaino, Scarpe, Insegna (livelli, costi, effetti) |
| robots.json | `RobotConfig` | NONNO: pad 50 LN a 130 s, restock e banco, livelli |
| zones.json | `ZoneConfig` | distanze, tempi per zona, pad |
| offline.json | `OfflineConfig` | Cassa, tetti, x2, Turno di notte, totem |
| daycycle.json | `DayCycleConfig` | durate del ciclo e calendario |

Import: `JsonUtility.FromJsonOverwrite(textAsset.text, so)` in un editor script (es. `Tools/Import Ch1 Data`). JsonUtility ignora `source`/`chapter` se non mappati e richiede campi pubblici o `[SerializeField]` con lo stesso nome.

```csharp
using System; using UnityEngine;

[CreateAssetMenu(menuName="UltimoMiglio/EconomyConfig")]
public class EconomyConfig : ScriptableObject {
  public int parcelValueLn; public float parcelValueGrowthPerChapter;
  public float customersPerMin; public float customerPatienceSec; public bool customersAtNight;
  public Combo combo; public float insegnaValueBonusPerLevel, reputationVipValueBonusPerLevel, playerSpendShare;
  public NinaBase nina;
  [Serializable] public class Combo { public float stepPerCustomer, maxMultiplier, resetGapSec, resetGapSecAssist; }
  [Serializable] public class NinaBase { public float baseSpeedMps, fullStackSlowdown; public int startStackCapacity; }
}

[CreateAssetMenu(menuName="UltimoMiglio/DebtConfig")]
public class DebtConfig : ScriptableObject {
  public long trancheLn, requiredPadLn; public int dueCalendarDay, yearDays, tranchesTotal;
  public float windowActiveMin, minWindowFromPrevPaymentActiveMin, calendarDaysPerCycle;
  public Early earlyPayment; public Proroga proroga; public Late late; public Assist assist;
  [Serializable] public class Early { public int minCyclesEarly, reputationLevels, bulloni; }
  [Serializable] public class Proroga { public int tokensGrantedAtActStart, tokensMax; public float extensionActiveMin; public bool appliesToCurrentTrancheOnly, autoUse, purchasable; }
  [Serializable] public class Late { public float trancheSurcharge, pedaggioCoinPenalty; public bool pedaggioAppliesToOffline; }
  [Serializable] public class Assist { public float trancheDiscount; public int trancheDiscountFromChapter; public float calendarDaysPerCycle, windowActiveMin, comboResetGapSec, offlineShareMax; }
}

[Serializable] public class ZainoLevel { public int level, stackCapacity; public long costLn; }
[Serializable] public class ScarpeLevel { public int level; public float speedBonus; public long costLn; }
[Serializable] public class InsegnaLevel { public int level; public float customerValueBonus; public long costLn; }
[CreateAssetMenu(menuName="UltimoMiglio/UpgradesConfig")]
public class UpgradesConfig : ScriptableObject {
  public Track<ZainoLevel> zaino; public Track<ScarpeLevel> scarpe; public Track<InsegnaLevel> insegna;
  [Serializable] public class Track<T> { public T[] levels; }   // Unity 2020.1+ serializza i generici
}

[Serializable] public class NonnoLevel { public int level; public float restockPpm, selfServicePpm; public long costLn; }
[CreateAssetMenu(menuName="UltimoMiglio/RobotConfig")]
public class RobotConfig : ScriptableObject {
  public Nonno nonno; public bool ninaCanAlwaysRestockByHand;
  [Serializable] public class Nonno { public float unlockAtOnboardingSec; public int hireCostLn; public string padLabel;
    public float restockPpmBase, restockPpmPerLevel, selfServicePpmBase, selfServicePpmPerLevel;
    public int maxLevel; public float upgradeCostBaseLn, upgradeCostGrowth; public NonnoLevel[] levels; }
}

[CreateAssetMenu(menuName="UltimoMiglio/ZoneConfig")]
public class ZoneConfig : ScriptableObject {
  public Distances distancesM; public Timings timingsSec; public Pad[] pads; public bool padPaymentPersistent;
  [Serializable] public class Distances { public float arriviToShelves, shelvesToBancone; }
  [Serializable] public class Timings { public float zoneDwellBeforeAction, pickPerParcel, dropPerParcel, servePerCustomer, coinPickupPerTrip, padFillMax; }
  [Serializable] public class Pad { public string id; public int costLn; public float unlockAtOnboardingSec; public string costRef; }
}

[CreateAssetMenu(menuName="UltimoMiglio/OfflineConfig")]
public class OfflineConfig : ScriptableObject {
  public string collectionMethod; public bool modalSummary;
  public float rateShareOfRobotThroughput, cassaCapacityShareOfTranche; public int cassaCapacityLn;
  public float offlineShareOfChapterCoinsMax, offlineShareOfChapterCoinsMaxAssist;
  public int adDoubleMaxPerDay; public bool adDoubleAlsoWithBulloni, pedaggioApplies;
  public TurnoDiNotte turnoDiNotte; public Totems boostTotems;
  [Serializable] public class TurnoDiNotte { public int unlockChapter, costLn; public float cassaCapacityShareOfTranche; }
  [Serializable] public class Totem { public float speedBonus, stackBonus, durationSec; }
  [Serializable] public class Totems { public int unlockChapter, maxPerSession, maxPerDay; public Totem caffeTurbo, zainoGonfio; }
}

[CreateAssetMenu(menuName="UltimoMiglio/DayCycleConfig")]
public class DayCycleConfig : ScriptableObject {
  public float cycleDurationSec, dayDurationSec, sunsetTransitionSec, nightDurationSec;
  public int nightFromChapter; public float nightDurationSecFromCh6, dayDurationSecFromCh6, calendarDaysPerCycle;
  public bool timeAdvancesOnlyWhileActive;
}
```

Note:
- `costLn` del livello N è il prezzo per **arrivare** a N (il livello 0 costa 0).
- Turno di notte (cap 3) e totem (cap 4) sono inclusi già ora per completezza; nel cap 1 restano bloccati.
- Assist: lo sconto sulla tranche parte dal cap 4 (nel cap 1 la tranche resta 8.400 LN); il tetto offline in Assist è il 12%.
- Distanza scaffali↔Bancone al cap 1 = 9 + 0,4×1 = 9,4 m.
