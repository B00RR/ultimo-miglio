# 11 - LONGEVITÀ v1.2 (Game Designer) - CHIUSO
**v1.2 (15/10/2026), decisioni finali del Director:**
1. Per chi ignora il gestionale: **NONNO capocantiere dal cap 8** (10 v1.3).
2. Classifiche: Cloud Function minima per Play Integrity **prima del lancio**, fuori dallo sprint corrente.
3. **Solara infinita si sblocca subito dopo il finale**, con un seed di partenza facile.

**ignore con (b)+(c)** (`qa/long_matrix_v12.txt`):
| eff | Ore | Max min/cap | More |
|---|---|---|---|
| 0,7 | 27,9 | 141 | 0 |
| 0,85 | 27,2 | 138 | 0 |
| 1,0 | 26,6 | 136 | 0 |
| 1,2 | 26,1 | 134 | 0 |

Chi ignora è sempre più lento di average (25,3-27,1 h) e del naive che impara dalla bolletta (25,5-27,3 h). Supera i 135 min solo dove già l'average sta a 136-138 (eff ≤ 0,85), quindi il limite viene dalla curva, non da chi ignora. Decisioni aperte: nessuna. §3.1 resta la tabella v1.1 come storico.
**v1.1 (15/10/2026), correzioni da qa/12_REVIEW_LONGEVITA e decisioni di Lorenzo:**
1. Le decisioni costano tempo: il 50% del tempo-decisione non produce LN. Tranche ricalibrate, good eff 1,0 ≈ 23,3 h, curva dei capitoli appiattita (max 116 min).
2. Raccomandazione adottata: **(c) avviso in bolletta + (b) Assist proposta alla 1ª mora, alla 2ª proroga o con un capitolo oltre 135 min**.
3. Calendario: giorno 365 = fine cap 15, cap 1 invariato.
4. Cap 1 escluso dalla regola del 125%.
5. Tabella more rifatta col modello gestionale v1.2.
6. Specifiche tecniche di Solara infinita e anti-cheat.
7. Telemetria a stati.

**Tutte le tranche e le quote sono da RICALIBRARE dopo il playtest con la telemetria (§9).**
Riferimenti: 00_BIBBIA, 01_CORE_LOOP v3.3, 03_MONETIZZAZIONE v2, 08_RETENTION v1, 10_GESTIONALE v1.1. Sim: `qa/econ_sim_long.py` (copia di `econ_sim_gest.py` con le scadenze per capitolo, `--due`) e `qa/longevita_plan.py` (piano dei minuti, calibrazione delle tranche, quote di tempo). `econ_sim_v3.py` ed `econ_sim_gest.py` restano invariati.
**Obiettivo di Lorenzo:** MESI di gioco, non 8 ore. **Vincoli:** niente pay-to-win; i Bulloni non servono mai per avanzare; il cap 1 resta identico allo Sprint 1; nessuna rewarded sui sistemi gestionali (10 v1.1).

## 1. Principio: più cose da fare, non tranche più alte
La storia passa da **~7,5 h a ~24 h effettive** (eff 1,0). Le tranche crescono, ma solo perché ogni capitolo ha **più contenuto giocato**: un sistema nuovo, contratti, quartieri, eventi, missioni di trama. Non è "lo stesso loop più a lungo".
Regole anti-grind:
1. In ogni capitolo dal 2 in poi, almeno il **35% del tempo va in decisioni** (stazioni gestionali, bacheca, mappa, officina, quadro, missioni di trama).
2. L'**attesa pura** (timer senza un'alternativa utile) resta **sotto il 5%**: ogni timer, dall'officina ai droni, ha sempre un'attività parallela a portata di joystick.
3. **Durata capitolo (v1.1):** good ≤ 116 min, average/naive con (b)+(c) ≤ ~135 min (p75 ≤ 130 in telemetria).
4. **Ogni 15-20 min arriva una novità** (contratto speciale, attacco, evento di quartiere, scena).

## 2. Cosa riempie ogni capitolo
> **v1.1:** i target per capitolo validi sono quelli di §3 (curva appiattita, max 116 min per good eff 1,0). La colonna "Target min" qui sotto è quella della v1, superata.

| Cap | Target min | Novità di sistema (10) | Contratti / quartieri | Eventi e trama (min fissi) |
|---|---|---|---|---|
| 1 | 30 | - (Sprint 1 invariato) | - | - |
| 2 | 45 | Bacheca: 1 contratto guidato | Mamma Rosa, Borgo Gradoni | Riapertura ufficiale (5) |
| 3 | 55 | Corrente base, colonnina, bolletta | 2 contratti, primi droni | Prova del tetto con PIPPA (8) |
| 4 | 70 | Rotte & quartieri (furgone, 4 quartieri), fasce di prezzo | Terrazze Alte, Limoneti | Arriva TICO, prima consegna in furgone (10) |
| 5 | 80 | Officina & usura, pannelli, affitto Garage | contratti fragili | Samir e il Garage (10) |
| 6 | 90 | Batterie, luci notturne | contratti notturni (Civetta) | Prima notte al Molo Basso (12) |
| 7 | 100 | Fronte OmniFreight (settimanale) | difesa del primo quartiere | Guerra dei prezzi, Viktor (12) |
| 8 | 105 | Refrigerati + frigo, pesanti | 5 quartieri | BRACCIO, Cortile (12) |
| 9 | 110 | Blackout con priorità, meteo variabile | contratti multi-tappa | Il furto (14) |
| 10 | 115 | 6 quartieri; ECO, ordini nascosti | contratti ECO | Carico 0 (14) |
| 11 | 120 | Attacchi doppi | alleanze di quartiere | Sabotaggio al quadro (14) |
| 12 | 120 | Evento co-op (antenna) | contratti di rete | Evento cooperativo (14) |
| 13 | 125 | Cantiere del razzo: picco di consumo | contratti "cantiere" | Atto IV (15) |
| 14 | 125 | Tutti i sistemi, settimana perfetta | quartieri in difesa | Viktor all'angolo (15) |
| 15 | 130 | Lancio: la rampa del Faro va alimentata | ultima consegna | Finale (18) |

## 3. Verifica in sim v1.1 (`qa/longevita_plan.py`, `qa/econ_sim_long.py`, `qa/gest_model.py` v1.2; 4×5, senza ad)
**Metodo:**
- Le **decisioni costano tempo**: per ogni capitolo il reddito di Nina è × (1 − 0,5 × quota decisioni). Le quote sono 5% al cap 1, poi 17-49%.
- Le tranche sono calibrate su **good eff 1,0**. Target per capitolo (curva appiattita rispetto alla v1, per lasciare spazio allo scarto del 21%): **30 / 50 / 65 / 80 / 90 / 95 / 100 / 105 / 108 / 110 / 110 / 110 / 112 / 114 / 116 min** (trama inclusa).
- Le scadenze sono il 125% dei minuti di loop, **escluso il cap 1** (40 min).
- (c) è modellata come ipotesi QA: il naive diventa average dal cap 6 dopo l'avviso in bolletta. (b): l'Assist parte dal capitolo successivo alla 1ª mora, alla 2ª proroga o al primo capitolo oltre 135 min.
- Output in `qa/long_matrix_v11.txt` (`qa/long_matrix_v11.sh`); configurazione in `qa/longevita_v11.json`.

### 3.1 Minuti, ore e more per profilo
Totali in ore, max in minuti per capitolo; P = proroghe.

| Profilo | eff | senza soluzioni | (b)+(c) |
|---|---|---|---|
| good | 0,7 | 24.4 h; max 121; more 0; proroghe 0 | 24.4 h; max 121; more 0; proroghe 0 |
| good | 0,85 | 23.7 h; max 118; more 0; proroghe 0 | 23.7 h; max 118; more 0; proroghe 0 |
| good | 1,0 | 23.3 h; max 116; more 0; proroghe 0 | 23.3 h; max 116; more 0; proroghe 0 |
| good | 1,2 | 22.8 h; max 114; more 0; proroghe 0 | 22.8 h; max 114; more 0; proroghe 0 |
| average | 0,7 | 27.1 h; max 140; more 0; proroghe 0 | 27.1 h; max 138; more 0; proroghe 0; assist dal cap 14 |
| average | 0,85 | 26.3 h; max 136; more 0; proroghe 0 | 26.3 h; max 136; more 0; proroghe 0 (assist mai, oltre 135 solo al cap 15) |
| average | 1,0 | 25.8 h; max 133; more 0; proroghe 0 | 25.8 h; max 133; more 0; proroghe 0 |
| average | 1,2 | 25.3 h; max 131; more 0; proroghe 0 | 25.3 h; max 131; more 0; proroghe 0 |
| naive | 0,7 | 32.6 h; max 178; more 8; proroghe 0 | 27.5 h; max 138; more 0; proroghe 0; assist dal cap 14 |
| naive | 0,85 | 30.8 h; max 172; more 5; proroghe 0 | 26.7 h; max 136; more 0; proroghe 0 (assist mai, oltre 135 solo al cap 15) |
| naive | 1,0 | 29.3 h; max 167; more 2; proroghe 1 | 26.1 h; max 133; more 0; proroghe 0 |
| naive | 1,2 | 28.2 h; max 148; more 0; proroghe 0 | 25.7 h; max 131; more 0; proroghe 0 |
| ignore | 0,7 | 30.6 h; max 173; more 4; proroghe 1 | 29.3 h; max 154; more 0; proroghe 4; assist dal cap 11 |
| ignore | 0,85 | 28.8 h; max 167; more 1; proroghe 1 | 28.4 h; max 150; more 0; proroghe 0; assist dal cap 11 |
| ignore | 1,0 | 28.0 h; max 148; more 0; proroghe 0 | 27.9 h; max 147; more 0; proroghe 0; assist dal cap 11 |
| ignore | 1,2 | 27.4 h; max 145; more 0; proroghe 0 | 27.3 h; max 144; more 0; proroghe 0; assist dal cap 12 |

**Lettura:**
- ✔ **good eff 1,0 = 23,3 h**, tutti i capitoli ≤ 116 min, 0 more a ogni eff.
- ✔ **naive con (b)+(c): 0 more, max 131-138 min** (138 a eff 0,7, 136 a eff 0,85). Senza interventi: fino a 8 more e 178 min.
- ✔ average: 25-27 h, max 131-140 (eff 0,7).
- ❌ **ignore: max 144-154 min anche con (b)+(c)**, 0 more ma 4 proroghe a eff 0,7. Chi non tocca mai il gestionale non impara dalla bolletta, e l'Assist (−10%) arriva tardi (cap 11-12) e non basta. **Da decidere** (§8).
- Lo scarto good/naive del 21% su capitoli lunghi si traduce in 15-20 min in più per capitolo: è la conseguenza attesa della scelta di Lorenzo.

### 3.2 Tranche v-long e calendario (giorno 365 = fine cap 15)
| Cap | Target min | Finestra (min loop) | Scadenza (giorno) | gg/ciclo | Tranche (LN) |
|---|---|---|---|---|---|
| 1 | 30 | 40,0 | 24 | 4,2583 | 8.400 |
| 2 | 50 | 56,2 | 49 | 3,0308 | 30.208 |
| 3 | 65 | 71,2 | 73 | 2,3923 | 68.641 |
| 4 | 80 | 87,5 | 97 | 1,9467 | 167.622 |
| 5 | 90 | 100,0 | 122 | 1,7033 | 311.628 |
| 6 | 95 | 103,8 | 146 | 1,641 | 358.400 |
| 7 | 100 | 110,0 | 170 | 1,5485 | 606.175 |
| 8 | 105 | 116,2 | 195 | 1,4659 | 1.035.440 |
| 9 | 108 | 117,5 | 219 | 1,4496 | 1.636.331 |
| 10 | 110 | 120,0 | 243 | 1,4194 | 2.623.742 |
| 11 | 110 | 120,0 | 268 | 1,4194 | 4.059.446 |
| 12 | 110 | 120,0 | 292 | 1,4194 | 6.132.386 |
| 13 | 112 | 121,2 | 316 | 1,4054 | 9.391.335 |
| 14 | 114 | 123,8 | 341 | 1,3759 | 14.484.339 |
| 15 | 116 | 122,5 | 365 | 1,3905 | 21.402.231 |

Il cap 1 è identico allo Sprint 1: 8.400 LN, 40 min, giorno 24, 4,2583 gg/ciclo. "Puntuale" = nessuna rata in mora (01 §7).

### 3.3 Test EditMode (Tech Lead, `tests-run` con l'Editor attivo)
- `Ch1_Nonno_UnlockAt130s`: `NonnoUnlockSec == 130`.
- `Ch1_Window40min`: `windowActiveMin == 40`; la regola del 125% non si applica al cap 1.
- `Ch1_DaysPerCycle_4_2583`.
- `Ch1_Tranche8400_Unchanged_AfterLongevity`.
- `Ch1_NoManagementSystems`: nessun sistema di 10 attivo nel cap 1.
- In aggiunta, per il §4.3:
  - `SolaraRng_GoldenHash_Seeds0to999`;
  - `Ln_Int64_NoOverflow_Level100`;
  - `Ln_Add1_On1e15_Exact`.

## 4. Post-game profondo (estende 08 §2)
### 4.1 Distretti con regole uniche (diorami nuovi, 7 mini-capitoli × 25-30 min)
| Distretto | Regola unica | Sistema in primo piano |
|---|---|---|
| Molo Basso (giorno) | Marea: le banchine si allagano a orari fissi e le chiatte caricano solo con l'acqua alta | Rotte |
| Terrazze Alte | Ascensore a fune con potenza limitata: ogni salita costa corrente | Corrente |
| Quartiere dei Limoneti | Solo fragili e refrigerati, scadenze strette | Contratti |
| Promontorio del Faro | Vento: droni a terra con la raffica, consegne orbitali simboliche | Meteo + Officina |
| Borgo Nuovo (OmniFreight ex hub) | Ogni settimana una zona torna a Viktor se non è difesa | Fronte |
| Rione Lanterne | Botteghe e lanterne: dal tramonto apre il mercato serale, la domanda raddoppia ma solo per 1 ciclo serale; le lanterne vanno accese (pad) per attirare clienti | Contratti + Corrente |
| Colle delle Antenne | Quartiere alto di antenne e uffici, roccaforte OmniFreight: consegne in orario d'ufficio, quote contese ogni giorno, salita ripida per il furgone | Fronte + Rotte |

**Identità visiva dei distretti 5 e 6** (mappa, bandierine, UI; colore + motivo + icona per i daltonici):
- **Rione Lanterne:** rosa `#C9719E`, motivo a quadretti, icona lanterna.
- **Colle delle Antenne:** azzurro `#7FB8E0`, motivo a righe orizzontali, icona antenna.

### 4.2 Rifondazione a livelli (prestigio)
- **Livello 1 "Cooperativa":**
  - il distretto riparte da Zaino, Scarpe e Insegna al livello 1;
  - +1 Stella della Rete (+10% valore pacco, tetto 10, come 08 §2.2).
- **Livello 2 "Consorzio"** (dopo 5 Stelle): un **modificatore di regola** scelto da Nina alla Cassaforte, per esempio "Solo solare" o "Niente furgone".
  - Ogni Rifondazione con modificatore dà **Medaglie di Rete**.
  - Le Medaglie comprano progetti di qualità della vita (colonnina rapida, bacheca a 4 cartellini), arredi e titoli.
- **Livello 3 "Rete libera"** (10 Stelle): sblocca Solara infinita (§4.3) a difficoltà crescente.
- Stelle e Medaglie **non si comprano mai**. Il secondo giro dura circa il 60% del primo (08).

### 4.3 Modalità "Solara infinita"
- Distretti procedurali con un seed settimanale (lo stesso per tutti): pianta del deposito, quartieri, meteo, contratti e attacchi OmniFreight generati.
- **Sblocco (v1.2):** subito dopo il finale, partendo dal Livello di Rete 1 con un seed facile.
- Scaling: **Livello di Rete N**. Ogni distretto completato alza la domanda del 5% e la difficoltà dei contratti, e aggiunge un "modificatore" ogni 5 livelli: blackout più frequenti, robot più fragili, marea, quartieri in sciopero.
- **Specifiche tecniche (v1.1, da 12_REVIEW §4):**
  - **RNG proprio e versionato** (PCG32 o xoshiro256** in C# puro), mai `UnityEngine.Random` né `System.Random`. Solo aritmetica intera nel generatore (probabilità in /10.000, griglia intera, niente float/Mathf), niente dipendenze dall'ordine di Dictionary/HashSet, dal tempo o dal locale.
  - **Stream separati per sistema:** pianta, quartieri, meteo, contratti, attacchi.
  - **Seed settimanale** = hash64(anno-settimana ISO **UTC**, `GENERATOR_VERSION`, modalità). Cambia il **lunedì alle 00:00 UTC** (alle 02:00 a Roma in estate): va indicato in UI. `GENERATOR_VERSION` si cambia solo al cambio di settimana.
  - **Golden test:** SHA-256 del distretto serializzato in modo canonico per i seed 0-999, riferimento in `qa/golden/solara_v1.json`. Gira in EditMode e su device **ARM64 e ARMv7** (Firebase Test Lab), e un hash diverso fa fallire la build.
  - **LN come int64** (`long`) intere, con arrotondamento esplicito a ogni accredito e saturazione al posto del wrap. Formattazione fino a 1e18; nel save i `long` sono stringhe; mai PlayerPrefs.
  - **Crescita del valore pacco ≤ +5% per Livello di Rete.** Con le stime QA (`qa/overflow_check.py`) si arriva a ~8e13 LN di carriera al livello 100, ben dentro int64: **nessun overflow fino al livello 100** (con ×1,5 per livello int64 andrebbe in overflow prima di N=50).
- L'obiettivo è **il punteggio di gestione**: netto della settimana, contratti puntuali, quartieri difesi, zero blackout. Non conta il grind delle LN.

### 4.4 Eventi e stagioni (allineate a 03 §3, 08 §2.4 e alla Risposta LiveOps in 10 §8)
- **Stagioni "Segnali di ECO" S1-S4** (28 giorni): ogni stagione dà il tema al Fronte settimanale:
  - S1: manifesti;
  - S2: Promontorio del Faro;
  - S3: ronde al Molo Basso;
  - S4: cantiere.
  Nel post-game le stagioni aprono un **distretto temporaneo** con regola unica. Prima del cap 10 non si nomina ECO.
- **Weekend del Segnale** (72 h): mini-diorama con economia propria.
- **Settimanale:** Fronte OmniFreight (+1 contributo all'antenna co-op per quartiere difeso, missioni del pass) e quest di Mamma Rosa.
- **Mensile:** antenna co-op (dal cap 12).
- **Eventi di città** (riferimento: il Crane Lock Day di SSS2037): "Festa dei Limoni", "Sciopero dei traghetti". Durano 1-2 giorni, la domanda di un quartiere va ×3 e si preparano in anticipo con scorte e corrente.

### 4.5 Collezione
- Album di Solara (08 §3), a cui si aggiungono:
  - **le cartoline dei quartieri** (una per contratto speciale);
  - **il quaderno di Nonna Ada** (pagine di lore trovate nei distretti);
  - **i robot minori**;
  - le livree di furgone e colonnine.
- Tutto si ottiene giocando. A pagamento ci sono solo varianti di colore.

### 4.6 Classifiche asincrone (Google Play Games)
- **Seed settimanale di Solara infinita:** punteggio di gestione, con leghe da 50 come i Weekend.
- **Settimana perfetta:** classifica a soglie (badge), non competitiva.
- **Corse di Samir:** come già previsto.
- Nessun backend proprio: si usano le classifiche di Play Games, con la difesa anti-cheat lato client e i limiti di 08 §5.
- **Anti-cheat (v1.1, da 12_REVIEW §5), tutto sul client prima dell'invio, più moderazione:**
  1. **tetto del punteggio** calcolato dalle tabelle dati (massimo teorico good per distretto e livello); sopra il tetto non si invia e parte `lb_reject`;
  2. **tempo minimo per distretto** (circa 0,5 × durata sim a eff 1,5 good, quindi ~12-15 min);
  3. **minuti attivi ≤ tempo reale trascorso** (orologio monotono) e ≤ 18 h al giorno;
  4. **hash del replay** (decisioni + contatori) ricalcolato dal client, salvato nello `scoreTag` insieme alla versione del generatore;
  5. **flag di manomissione** di orologio e save: la settimana è marcata `suspect` e non si invia;
  6. **revisione manuale settimanale del top 1%** dalla Play Console, più l'evento `lb_submit` {score, bound_ratio, t_active}.
  - **Nota:** la Play Integrity API piena richiede un server per verificare il verdetto. Senza backend serve solo come segnalazione: decisione di Lorenzo (§8).

### 4.7 Obiettivi di mesi
- 10 Stelle.
- Consorzio con tutti i modificatori.
- Livello di Rete 50 in Solara infinita.
- Album e quaderno di Ada completi.
- Le 4 stagioni.
- Lega Oro.
- Circa 60 achievement di Play.

## 5. Percorso del giocatore (4 sessioni da 5-8 min al giorno, ~25 min/giorno)
**3 mesi**
| Periodo | Cosa fa | Obiettivo che lo tiene |
|---|---|---|
| Settimana 1 | Cap 1-4: tutorial, primi contratti, corrente, furgone | Atto I in 4-6 giorni (03), D7 |
| Settimane 2-3 | Cap 5-8: officina, notte, Fronte, refrigerati | Il primo quartiere difeso, la bolletta in attivo |
| Settimane 4-6 | Cap 9-12: blackout, ECO, antenna co-op | Trama del Carico 0, la stagione in corso |
| Settimane 7-8 | Cap 13-15 e finale (~24 h totali in ~60 giorni) | Epilogo "Puntuale" |
| Mese 3 | 1-2 distretti post-game, prima Rifondazione | Stelle, distretto di stagione |

**6 mesi**
| Mese | Contenuto | Leva |
|---|---|---|
| 1-2 | Storia | Trama, sistemi nuovi |
| 3 | Distretti 1-2, Stelle 1-3 | Regole uniche |
| 4 | Distretti 3-5, Stelle 4-6, Consorzio | Modificatori, Medaglie |
| 5 | Solara infinita, seed settimanale | Classifica, Livello di Rete |
| 6 | Stagione S2 della post-storia, Stelle 7-10, album | Collezione, co-op |

## 6. Monetizzazione (invariata nei principi)
- Più ore significano più sessioni, quindi più rewarded di tipo totem e Cassa entro i tetti già previsti, e una vita più lunga del pass. **Nessuna rewarded sui sistemi gestionali.**
- I Bulloni restano su timer e cosmetici: Stelle, Medaglie, Livello di Rete, distretti, contratti e seed **non si comprano**.
- In Solara infinita la classifica conta solo le ore di gioco, non il denaro.

## 7. Domande per LiveOps e QA
**LiveOps**
1. I temi del Fronte e i distretti temporanei per S1-S4 nel post-game: chi scrive il calendario dei seed settimanali?
2. Gli eventi di città (×3 domanda) si sovrappongono al Weekend del Segnale o li alterniamo?
3. Le missioni del pass per chi è nel post-game (Solara infinita) e per chi è al cap 2-5 devono essere separate?
4. KPI di longevità: D60/D90, % di giocatori che completano la storia, Rifondazioni per payer/non-payer, sessioni/giorno nei mesi 3-6. Quali soglie per il soft launch?
5. Con una storia da ~24 h (circa 60 giorni), l'interstitial di "nuovo capitolo" diventa più raro: va rivisto il cap per DAU?

**QA**
1. Sim indipendente di `longevita_plan.py`: verificate le tranche v-long e il **rischio mora per il naive** (§3) e testate le soluzioni (a), (b) e (c).
2. Misurare in playtest la quota decisioni/attesa (oggi è stimata): servono gli eventi di telemetria che separano routine, decisione e attesa.
3. Il cap 1 è identico allo Sprint 1? Regression con tranche 8.400 e NONNO a 2:10.
4. Solara infinita: seed deterministico (stesso seed = stesso distretto su tutti i dispositivi), scaling al Livello di Rete 50-100 senza overflow delle LN (servono long/double).
5. Le classifiche di Play Games: controlli di plausibilità sul punteggio e sul tempo minimo per distretto.

## 8. Decisioni (chiuse)
- Storia di ~24 h (good, eff 1,0) e mora del naive gestita con (c)+(b): decise in v1.1.
- Capocantiere, Cloud Function prima del lancio e Solara infinita dopo il finale: decise in v1.2.

## 9. Telemetria: routine, decisioni, attesa (da 12_REVIEW §2)
Ogni secondo attivo appartiene a **un solo stato** (macchina a stati sul client). Gli eventi partono solo ai cambi di stato.

**I 5 stati:**
| Stato | Cosa include |
|---|---|
| **routine** | `carry_restock`, `serve`, `collect`, `pad_pay` |
| **decisione** | `station_decision`, `contract_route`, `night_stealth`, `phone_read` |
| **trama** | `story_mission`, `cutscene` |
| **attesa bloccata** | `wait_blocked`: `has_useful_action()==false` |
| **inattivo** | `idle_input`: nessun input da più di 10 s |

**Eventi:**
- `session_activity_summary` {sec_routine, sec_decision, sec_story, sec_wait_blocked, sec_idle, n_novelties};
- `state_change` (aggregato);
- `novelty_shown`;
- `decision_made` {station_id, choice, expected_value_delta};
- `wait_blocked_start`/`_end` {reason};
- `chapter_complete` {active_min, story_min, loop_min, ln_active, ln_offline, mgr_profile_est, more, proroghe, assist_on};
- `bill_closed`.

**KPI e soglie:**
| KPI | Soglia |
|---|---|
| Quota decisioni sul tempo attivo | ≥ 35% dal cap 3 |
| `wait_blocked` | < 5% (allarme > 8%) |
| Novità | mediana ≤ 20 min |
| Durata capitolo | p75 ≤ 130 min |
| Profilo gestionale stimato contro durata | verifica diretta del 21% good/naive |

Validazione: overlay di debug con lo stato corrente; la somma dei secondi per stato deve fare la durata della sessione ±1 s. **Con questi dati si ricalibrano le tranche (§3) e la quota decisioni (`DEC` in `longevita_plan.py`).**
