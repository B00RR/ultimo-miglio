# Dati Capitolo 1 (01_CORE_LOOP v3.3) - import in Unity 6

Sono JSON piatti con chiavi camelCase; l'unità di misura è nel nome della chiave (`Sec`, `Min`, `M`, `Mps`, `Ln`, `Ppm` = pacchi/min). Le frazioni sono decimali (0.15 = 15%).
Fonte: `01_CORE_LOOP.md` v3.3; verifica in `qa/econ_sim_v3.py`. Validazione: `python3 -m json.tool <file>`.

| File | ScriptableObject | Contenuto |
|---|---|---|
| economy.json | `EconomyConfig` | valore pacco, clienti/min, pazienza, combo, Kaia base |
| debt.json | `DebtConfig` | tranche, finestra, Proroga, mora/Pedaggio, Assist |
| upgrades.json | `UpgradeTrackConfig` ×3 | Zaino, Scarpe, Insegna (livelli, costi, effetti) |
| robots.json | `RobotConfig` | OLDO: pad 50 LN a 130 s, restock e banco, livelli |
| customers.json | `CustomerTypesConfig` | 4 tipi di cliente |
| onboarding.json | `OnboardingConfig` | 15 passi dell'onboarding |
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

## Golden path cap 1 (`playthrough_expected.json`)
Generato da `qa/playthrough_ch1.py`, che usa la stessa logica di `qa/econ_sim_v3.py`. Parametri: eff 1,0, nessun offline (run continua in editor), onboarding di 04_UI §4. Rigenera con `python3 qa/playthrough_ch1.py`, poi ricopia il file in `unity/Assets/_Game/Data/ch1/`.
- `milestones[]`: `id`, `tSec`, `toleranceSec`, `cumulativeEarnedLnApprox`.
- `purchases[]`: acquisti della politica "miglior valore per LN" (`pad`, `label`, `costLn`, `level`, `t`, `toleranceSec` = max(15 s, 15%)).
- `debtDeposits[]`: versamenti alla Cassaforte. Il primo è a 240 s (60% del portafoglio), poi uno ogni 60 s.
- `checkpoints[]`: stato ogni 60 s (LN cumulate, portafoglio, versato, livelli).
- `chapterEnd`: fine capitolo a eff 1,0 / 1,2 / 0,85, con finestra di 2.400 s.
- `botSteps[]` e `loops`: azioni del bot (`moveTo` + `zoneId` da `zones.json` → `zoneIds`, `holdOnZoneSec`).
- Tolleranze: onboarding ±5-15 s; acquisti ±15%; fine capitolo ±120 s (copre eff 0,85-1,2).

## Clienti e onboarding (aggiunti il 15/10)
- `customers.json` → `CustomerTypesConfig`: 4 tipi (normale, frettoloso, anziano, vipMammaRosa), ciascuno con `valueMultiplier`, `patienceSec`, `spawnWeight`, `unlockAtSec`, `visualCue`, `barkKeys`. Il valore medio pesato vale 1,00, quindi l'economia e il golden path restano invariati.
- `onboarding.json` → `OnboardingConfig`: `steps[]` con `id`, `tSec`, `toleranceSec`, `trigger`, `completeWhen`, `target` (zoneId), `textKeys` (segnaposto `onb.*`), `unlocks`. Specifica leggibile: `onboarding.md`.

## Revisione 16/10 (Game Designer)
- `upgrades.json`: Zaino 5/6/7 costano 560/960/1.700 LN (×1,7); dal livello 8 resta la serie ×2.
- `economy.json` → `rewards`: traguardi pacchi (100/250/450 = +60/+120/+200 LN), tappe del debito (25/50/75%), mancia di 30 LN al primo passaggio sulla Cassa, freccia "prossimo pad" da 480 s.
- Golden path rigenerato: rata 1 saldata a 34:00 (eff 1,2: 32:00; eff 0,85: 36:00). Valutazione delle varianti: `qa/eval_liveops_ch1.py`.
