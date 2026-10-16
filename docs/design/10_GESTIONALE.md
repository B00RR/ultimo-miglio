# 10 - GESTIONALE v1.3 (Game Designer) - CHIUSO
**v1.3 (15/10/2026), decisioni finali del Director:**
1. **OLDO capocantiere (cap 8+):** se le stazioni restano intatte per 3 cicli, OLDO le gestisce a livello *average* con resa ridotta (mercato ×0,98, robot ×0,97).
2. **Nessun premio a chi non gioca il gestionale:** nei cap 1-7 il naive non è mai peggio di chi ignora; dal cap 8 chi ignora resta più lento di average.
3. Classifiche: una Cloud Function minima per Play Integrity, **prima del lancio, non nello sprint corrente**.
4. Solara infinita si sblocca subito dopo il finale (11).

Modello in `qa/gest_model.py`, matrice in `qa/gest_matrix_v13.txt`.

**Effetto sulla sim (4×5):**
| eff | Scarto naive/good | ignore |
|---|---|---|
| 0,7 | +16,8% | 8,4 h |
| 0,85 | +16,7% | 8,1 h |
| 1,0 | +17,4% | 7,9 h |
| 1,2 | +17,1% | 7,7 h |

Lo scarto scende sotto il 21% di §7 perché il naive dei cap 1-7 è stato alzato al livello di ignore. Documento **chiuso**: le domande dei reparti in §8 restano come registro.


**v1.2 (15/10/2026), correzioni da qa/11_REVIEW_GESTIONALE (decisioni di Lorenzo):**
1. Chi ignora il gestionale non resta mai bloccato:
   - auto-riparazione lenta dei robot;
   - riarmo automatico del quadro in 45 s;
   - pavimento alle penalità (mercato ≥0,93, robot ≥0,80, bolletta ≤6%).
2. **I Bulloni non accelerano l'officina**; i Gettoni del pass premium non comprano componenti a usura lenta; niente Bulloni sulla Corrente.
3. Sim riscritta (`qa/gest_model.py`):
   - multipliers estesi ai cap 8-15;
   - niente doppio conteggio della Corrente;
   - l'offline segue lo stato gestionale;
   - profilo di default = **average**.
4. Caso blackout + notte + refrigerati risolto (§3 E).

**Storico v1.1:**
**v1.1 (15/10/2026), decisioni di Lorenzo:** v1 approvata; **lo scarto good/naive del ~21% resta così** (niente taratura); **nessuna rewarded sui sistemi gestionali**: la "Giornata di sole" è tolta. Lorenzo non accoglie la proposta LiveOps (§8, punto 2) di tenerla dentro il tetto dei totem. Il resto della Risposta LiveOps (Fronte settimanale a tema di stagione, contributo all'antenna co-op e al pass, nessuno spoiler prima del cap 10) è recepito.
Riferimenti: 00_BIBBIA, 01_CORE_LOOP v3.3, 03_MONETIZZAZIONE v2, 04_UI v3, 08_RETENTION, 09_SPRINT1. Sim: `qa/econ_sim_gest.py` (copia di `econ_sim_v3.py`, che resta invariato; flag `--mgr off|naive|good`).
Direzione di Lorenzo: profondità gestionale vera, che resti una sfida nel lungo periodo. La **Corrente è confermata**. Gli altri sistemi devono essere logistici e legati al tema: spedizioni, deposito, Solara, debito, rivalità con GrayBox.
**Vincolo di sempre:** controllo diretto. Ogni decisione si prende **camminando** fino a una stazione fisica: quadro elettrico, colonnine, bacheca contratti, mappa dei quartieri, banco officina. Nessun menu idle. Il telefono serve solo per grafici e report.

## 1. Ricerca: cosa fanno i riferimenti
| Gioco | Sistema gestionale | Cosa prendiamo |
|---|---|---|
| **Shipping Store Simulator 2037** (Playstrom/SayGames, PC, 14/10/2026) | Robot con indicatore di stato/energia che si **fermano se non mantenuti** e vanno riparati con kit (cacciavite, nastro...); budget e forniture; furgone per i grandi ordini e drone per gli urgenti; VIP; mercato nero notturno; **guerra al concorrente QuickBox** (pubblicità, clienti rubati, ordini VIP contesi); eventi città che dirottano ordini; scaffali rinforzati o doppi | Usura dei robot con riparazione fisica; consegne per tipo (furgone/drone); rivale che ruba i clienti; eventi città. Il riferimento diretto più vicino al nostro tema |
| Supermarket Simulator | Ordini di merce, prezzi impostati sul prodotto, layout degli scaffali, licenze | Prezzi dinamici e layout (valutati, §2) |
| Factorio / shapez | Throughput, colli di bottiglia, energia con solare, accumulatori e priorità | Corrente con solare, batterie e priorità; lettura dei colli di bottiglia |
| Big Ambitions | Affitti, stipendi, logistica di import, rotte di consegna, analisi per quartiere | Costi fissi; domanda per quartiere |
| Transport Fever | Rotte, flotta, manutenzione dei veicoli che invecchiano | Rotte per quartiere; usura |
| Two Point Hospital/Campus | Personale con stanchezza, reputazione, sfide a obiettivo | Reputazione legata ai contratti |
| My Little Universe / idle tycoon | Zone a terra, nessuna gestione esplicita, manager che automatizzano | Ogni sistema deve stare in una **zona a terra** e restare leggibile in 2 secondi |

## 2. Shortlist (Corrente confermata + 8 candidati)
| # | Sistema | Sfida | Come si gioca (stazione fisica) | Cap | Rif. | Esito |
|---|---|---|---|---|---|---|
| E | **Corrente** | Comprare la rete nel momento giusto, sfruttare sole e batterie, evitare i blackout | Quadro elettrico in Magazzino, colonnine, pannelli sul tetto | 3 (base) → 9 | Factorio, SSS2037 (energia dei bot) | **CONFERMATO** |
| 1 | **Rotte & quartieri** (furgone e droni) | Ogni giorno 4-6 quartieri di Solara chiedono volumi e tipi diversi; il furgone ha slot limitati | Mappa di Solara sul muro del Garage: Kaia carica il furgone alla baia del quartiere scelto | 4 | SSS2037 (furgone/drone), Transport Fever, Big Ambitions | **RACCOMANDATO** |
| 2 | **Contratti & Reputazione** | Scegliere tra contratti sicuri e rischiosi (scadenza, fragili, refrigerati, penale), con reputazione per quartiere | Bacheca dei contratti accanto al Bancone: si prende il cartellino calpestando la zona | 2 (1 contratto) | SSS2037 (VIP), Two Point (reputazione) | **RACCOMANDATO** |
| 3 | **Officina & usura dei robot** | I robot si consumano; meglio fermarli prima che si guastino in turno | Banco officina in Garage: Kaia porta il robot (lo "guida" camminando) o porta i ricambi a terra | 5 | SSS2037 (riparazioni), Transport Fever | **RACCOMANDATO** (ricambi in LN, **mai Bulloni**) |
| 4 | **Fronte GrayBox** (quote per quartiere) | Maxim attacca un quartiere a settimana (prezzi bassi, manifesti); bisogna difenderlo o lasciarlo andare | Su un quartiere attaccato: consegne lampo + attacchinaggio con Kenzo alla mappa | 7 | SSS2037 (QuickBox, poaching) | **RACCOMANDATO**: è il cuore narrativo |
| 5 | Dogana & contrabbando al Molo Basso | Rischio di ispezione contro valore | Esiste già: notte, Calore, Gettoni (01 §8) | 6 | SSS2037 (mercato nero) | Scartato come sistema nuovo: c'è già, lo si collega ai contratti |
| 6 | Materiale d'imballo (scatole, nastro, ghiaccio) | Scorte da riordinare | Pallet fornitore agli Arrivi | - | Supermarket Sim | Scartato: è un'altra corsa ai rifornimenti senza scelte vere. Il ghiaccio vive dentro i contratti refrigerati |
| 7 | Layout & smistamento del magazzino | Posizionare gli scaffali | Pad di spostamento | - | shapez, Supermarket Sim | Rimandato: a mobile, con TIKTAK dal cap 4, è costoso da produrre; resta come eventuale post-game |
| 8 | Prezzi dinamici | Tariffa per quartiere e ora | Cartello prezzi al Bancone | - | Supermarket Sim, Big Ambitions | Scartato: entra in conflitto con la domanda > capacità (01 §1). La tariffa la gioca GrayBox (#4) |
| 9 | Assicurazione / reclami e resi / meteo e traffico | Varianza | - | - | Big Ambitions | Non come sistemi a sé: il meteo guida solare (E) e rotte (1); reclami e resi = penale del contratto (2); l'assicurazione è una clausola del contratto |
| 10 | Costi fissi (affitto Garage, ricambi) | Pressione sul margine | Bolletta di fine giornata | 5 | Big Ambitions | Incluso come **voce della bolletta** (§4), non come sistema |

**Perché questi 4 + Corrente:** coprono i tre livelli di una ditta di consegne: dentro il deposito (Corrente, Officina), fuori (Rotte), mercato e trama (Contratti, GrayBox). Ognuno ha una **scelta con un costo-opportunità**, non solo una manutenzione. Tutti usano spazi già previsti (Magazzino, Garage, tetto, Bancone).

## 3. Design dei sistemi raccomandati
### E. Corrente (cap 3 → 9)
- **Produzione:**
  - *Rete* con prezzo variabile su 3 fasce: notte 0,5×, giorno 1×, picco serale 2×. Il ciclo di 7 min contiene tutte e 3 le fasce;
  - *Pannelli solari* sui tetti in terracotta: rendono col sole, a metà con le nuvole, zero di notte. Il meteo del giorno è annunciato da Auntie Lulu (cielo nel diorama);
  - *Batterie* nel locale tecnico (cap 6): si caricano a prezzo basso o dal surplus solare e si usano nel picco.
- **Consumi:** robot in ricarica (la voce principale), nastri di TIKTAK, luci (notte), frigo dei contratti refrigerati (cap 8), lanterne del Retrobottega.
- **Colonnine fisiche:** i robot hanno una batteria (icona sopra la testa). Sotto il 20% vanno da soli alla colonnina: è tempo morto, e se la colonnina è occupata fanno la coda. Più colonnine (pad) e colonnine rapide (costano di più in picco) sono la leva.
- **Fasce di prezzo (v1.2):** il **picco 2× cade a metà del giorno-sera** (min 3:30-4:30 del ciclo di 7 min), **non all'inizio della notte**. Notte 0,5×. Il blackout più probabile arriva quindi mentre Kaia è in Magazzino, non durante lo stealth.
- **Blackout e priorità:** se il consumo supera la potenza contrattuale, salta il quadro. Kaia corre al quadro elettrico e riarma **camminandoci sopra** (2 s); se non ci va, **il quadro si riarma da solo dopo 45 s** (v1.2). Al quadro ci sono **3 interruttori a terra** (Robot / Nastri / Frigo-Luci): passandoci sopra se ne cambia la priorità, quindi cosa si spegne per primo. **Gli interruttori arrivano al cap 8 insieme al frigo** (v1.2). Dal cap 9 c'è l'interruttore "Notte su batteria" e i blackout diventano più frequenti (meteo, sabotaggi).
- **Blackout e notte (v1.2):** un blackout **non aiuta lo stealth**. Le ronde hanno torce e i coni di visione non dipendono dalla luce. Se è Kaia a causare il blackout (sovraccarico mentre è nel Retrobottega), **Calore +1**.
- **Regole del frigo (v1.2):**
  - un contratto refrigerato tiene la catena del freddo per **90 s senza corrente**; oltre, il pacco è "caldo";
  - **penale = 25% del valore del contratto + Reputazione −1** nel quartiere;
  - offline e "Chiudi bottega": frigo, batterie e usura sono **congelati**, e la scadenza dei refrigerati conta solo in cicli attivi, quindi nessun contratto fallisce mentre il giocatore è assente;
  - i refrigerati si possono accettare solo con il frigo costruito.
- **Scelta buona:** caricare i robot fuori picco, pannelli prima delle colonnine rapide, frigo in priorità alta solo con contratti refrigerati attivi.

### 1. Rotte & quartieri (cap 4)
- Ogni giorno la **mappa di Solara** in Garage mostra 4-6 quartieri (Borgo Gradoni, Molo Basso, Terrazze Alte, Faro...) con domanda, tipo di pacco e meteo/traffico (scalinate, vento sul Faro).
- Il furgone ha N slot. Kaia carica i pacchi alla **baia del quartiere scelto**; i droni di FIZZ coprono i quartieri ripidi. Un giro pieno nel quartiere giusto vale +15-25%, uno mezzo vuoto o nel traffico ci perde.
- Nessuna guida: è una scelta, quindi le corse di Kenzo restano separate.

### 2. Contratti & Reputazione (cap 2 → 12)
- **Bacheca** con 3 cartellini al giorno: *sicuro* (valore basso), *urgente* (scadenza in cicli), *fragile* (niente corsa, pila max 4), *refrigerato* (cap 8, frigo acceso = corrente), *notturno* (collegato a Madame Moth).
- Consegna in tempo = bonus e Reputazione del quartiere (+domanda e VIP). In ritardo = penale (reclamo) e Reputazione giù. **Mai più di 2 attivi** al cap 2-5, 3 dopo.
- Il cap 2 ne introduce **uno solo, sicuro, guidato da Auntie Lulu** (sostituisce l'ordine speciale).

### 3. Officina & usura (cap 5)
- Ogni robot ha un'**usura** 0-100% (barra sopra la testa, colore). Sopra il 70% rende -10%; al 100% **guasto in turno** (fumo, si ferma).
- Banco officina in Garage. **Manutenzione programmata:** Kaia passa sopra il robot e "Mandalo in officina", poi resta fermo 1 ciclo / 2 se è guasto. Ricambi in LN dal pallet ricambi, **mai in Bulloni né acquistabili**.
- **I timer dell'officina NON si accelerano con i Bulloni** (v1.2): il fermo è la leva economica del sistema.
- **Auto-riparazione (v1.2):** un robot guasto non resta fermo per sempre. Dopo ~1 ciclo (7 min attivi) si rimette in piedi da solo **al 50% di usura**, rende il 70% finché non passa in officina e costa una piccola fattura in LN (la metà della manutenzione), che compare nella bolletta. Ignorare l'officina costa, ma non blocca mai.
- I progetti notturni (Gettoni guadagnati **di notte**, 01 §8) danno componenti a usura lenta. **I Gettoni del pass premium non possono comprarli** (v1.2): sono marcati "Gettoni del Libretto", spendibili solo in cosmetici e pezzi decorativi.

### 4. Fronte GrayBox (cap 7 → 14)
- Ogni settimana Maxim "attacca" un quartiere: manifesti teal, prezzi bassi, domanda di quel quartiere -40%.
- Difesa (scelta, non obbligo): contratti in quel quartiere + attacchinaggio (Kaia porta i manifesti del Sunn Depot ai 3 muri del quartiere al rientro dal furgone). Quartiere difeso = quota recuperata + Reputazione; ignorato = perso per la settimana.
- È il motore narrativo dell'Atto II-III: i quartieri difesi sbloccano gli alleati del cap 11 (evento cooperativo).

## 4. Leggibilità
- **Bolletta di fine giornata** (toast di 3 righe sulla Cassaforte, report completo nell'app *Conti* del telefono): Incassi · Corrente (rete/solare/batteria) · Ricambi · Affitto Garage · Penali/bonus contratti = Netto. Con il confronto con ieri e 1 consiglio ("Ricarica di notte: -18%").
- **Indicatori sopra i robot:** anello batteria (verde/giallo/rosso), chiave inglese per l'usura >70%, fulmine per "in coda alla colonnina". Massimo 1 icona per volta, priorità guasto > batteria > coda.
- **Grafico dei consumi** (app *Rete* del telefono, sola lettura): curva produzione/consumo sulle 3 fasce di prezzo, blackout segnati. In gioco: lancetta sul quadro elettrico (verde/rosso).
- Mappa dei quartieri: colore = Reputazione, bandierina teal = attacco GrayBox.
- **Quartieri 5 e 6 (dal cap 10):**
  - **Rione Lanterne:** botteghe, lanterne e mercato serale; domanda alta di sera, pacchi piccoli e numerosi. Mappa: rosa `#C9719E`, quadretti, icona lanterna.
  - **Colle delle Antenne:** quartiere alto di antenne e uffici, **roccaforte GrayBox** (attaccato di default, quota iniziale bassa); contratti d'ufficio a scadenza stretta. Mappa: azzurro `#7FB8E0`, righe orizzontali, icona antenna.
  - Colore, motivo e icona insieme, così la mappa resta leggibile anche per i daltonici.

## 5. Curva di introduzione (cap 1-15)
| Cap | Novità gestionali | Nota |
|---|---|---|
| 1 | **Nessuna** | Slice Sprint 1 invariata |
| 2 | 1 contratto guidato (Auntie Lulu) | Solo bacheca |
| 3 | Corrente base: 1 colonnina, anello batteria su OLDO/FIZZ, bolletta | Solo rete, prezzo fisso |
| 4 | Rotte & quartieri (furgone, 4 quartieri); fasce di prezzo della rete | Con TIKTAK |
| 5 | Officina & usura; pannelli solari; affitto Garage | |
| 6 | Batterie; luci notturne; contratti notturni | Con la notte |
| 7 | Fronte GrayBox | Guerra dei prezzi (Atto II) |
| 8 | Contratti refrigerati + frigo + **3 interruttori di priorità**; carichi pesanti | Con HUGO |
| 9 | Blackout più frequenti, "Notte su batteria"; meteo variabile | Atto III, sabotaggi |
| 10-12 | 6 quartieri (5° Rione Lanterne, 6° Colle delle Antenne, vedi sotto); attacchi doppi; sabotaggi al quadro (trama) | |
| 13-15 | Tutto attivo; sfide "settimana perfetta" | Rampa del Faro: picco di consumo da gestire |
Regola: **al massimo 1 sistema nuovo per capitolo**, introdotto da un personaggio con una missione guidata di 60 s.

## 6. Economia e monetizzazione
- I sistemi agiscono come **moltiplicatori sul reddito netto**. Chi gestisce bene guadagna; chi gestisce male perde, ma non va in blocco.
- **No pay-to-win (03 §2):** Bulloni, rewarded e IAP non comprano corrente, ricambi, riparazioni, contratti o quote di quartiere. **I Bulloni non accelerano nessun timer gestionale** (officina, auto-riparazione, ricarica, frigo) e non toccano la Corrente (v1.2). 03 §2 elenca già i timer accelerabili (droni, spedizioni, ispezione) senza l'officina: allineato. I Gettoni del pass premium sono solo cosmetici per il gestionale. **Nessuna rewarded sui sistemi gestionali** (decisione di Lorenzo, v1.1): corrente, officina, rotte, contratti e Fronte non hanno totem né video.
- **Cosmetici nuovi:** livree furgone, skin delle colonnine, manifesti personalizzati. Pass: missioni "consegna 3 contratti urgenti".
- **Retention (08):** la bolletta e il "batti ieri" danno un obiettivo giornaliero; il Fronte GrayBox è settimanale (lun-dom) con tema di stagione S1-S4; ogni quartiere difeso vale +1 all'antenna co-op e conta per le missioni del pass; fino al cap 10 nessun riferimento a NULO (Risposta LiveOps §8). Il post-game ottiene profondità (punteggio di gestione per settimana).

## 7. Check di economia v1.2 (`qa/econ_sim_gest.py` + `qa/gest_model.py`, tranche v3.3)
**Modello:** tre blocchi separati, senza doppi conteggi.
- **Mercato** (Kaia e robot): contratti cap 2, rotte cap 4, Fronte cap 7, refrigerati cap 8, 6 quartieri cap 10, attacchi doppi cap 11, settimana perfetta cap 13.
- **Robot** (solo la quota automatica e l'offline): ricarica cap 3, usura cap 5, blackout cap 8, sabotaggi cap 11.
- **Bolletta** in LN: corrente cap 3, frigo cap 8, costi fissi 2% dal cap 5.

L'offline passa dal moltiplicatore robot. I pavimenti valgono per tutti i profili.

| Profilo | Cosa fa |
|---|---|
| good | Ottimizza (solare/batteria, contratti puntuali, officina programmata, quartieri difesi) |
| **average** (default) | Gestisce a metà |
| naive | Ci prova ma sbaglia: penali, guasti, picco |
| ignore | Non tocca mai una stazione: auto-riparazione, riarmo 45 s, nessun contratto |

Il moltiplicatore **Kaia/robot** per capitolo esce da `python3 qa/gest_model.py`. Valori al cap 15:

| Profilo | Kaia | Robot |
|---|---|---|
| good | 1,15 | 1,10 |
| average | 0,99 | 0,87 |
| naive | 0,87 | 0,70 |
| ignore | 0,89 | 0,72 |

**Ore totali sui 15 capitoli** (range min/cap). Matrice completa in `qa/gest_matrix_v12.txt` (`qa/gest_matrix.sh`).

| eff | sess | good | average | naive | ignore | naive vs good |
|---|---|---|---|---|---|---|
| 0,7 | 4×5 | 7,4 h (27,5-34,2) | 8,1 h (31,3-34,2) | 8,9 h (32,5-37,0) | 8,7 h (32,2-36,2) | +19,5% |
| 0,85 | 4×5 | 7,2 h (27,1-31,4) | 7,8 h (29,3-32,0) | 8,6 h (30,5-35,7) | 8,4 h (30,2-35,1) | +19,3% |
| 1,0 | 4×5 | 7,0 h (26,6-30,0) | 7,6 h (28,6-31,5) | 8,4 h (29,8-35,5) | 8,2 h (29,5-35,0) | +20,5% |
| 1,2 | 4×5 | 6,8 h (26,0-28,6) | 7,4 h (27,1-31,1) | 8,2 h (27,8-35,1) | 8,0 h (27,5-34,3) | +20,0% |
| 0,7 | senza offline | 8,6 h | 9,5 h | 11,2 h, **7 more** | 10,6 h, **4 more** | +30,7% |
| 0,85 | senza offline | 8,2 h | 9,1 h | 10,2 h, 0 more, 4 proroghe | **9,9 h, 0 more (max 39,9 nei cap 1-7, 42,0 dopo)** | +23,7% |
| 1,0 | senza offline | 8,0 h | 8,8 h | 9,9 h, 0 more | 9,6 h, 0 more | +23,6% |
| 1,2 | senza offline | 7,8 h | 8,6 h | 9,6 h, 0 more | 9,3 h, 0 more | +23,7% |

**Con l'Assist (senza offline)**, tutti con 0 more:

| Profilo | Range min/cap |
|---|---|
| ignore eff 0,85 | 33,9-37,9 |
| ignore eff 0,7 | 35,4-39,6 |
| naive eff 0,85 | 35,1-38,8 |
| naive eff 0,7 | 36,7-40,0 |

**Esito delle decisioni:**
- ✔ Scarto good/naive con 4×5: **19-21%**, nel target di Lorenzo (~21%). Senza offline sale al 24% (31% a eff 0,7).
- ✔ ignore a eff 0,85 senza offline: **0 more**, ≤40 min nei cap 1-7 (max 39,9 al cap 7); nei cap 8-15 arriva a 40-42 min, salvato dalla finestra minima.
- ✔ L'Assist sistema sempre i casi lenti.
- ✔ Il giocatore lento (eff 0,7) senza offline e senza Assist va ancora in mora: è la spinta morbida voluta verso l'Assist (01 §7).

## 8. Domande per i reparti
- **QA:** sim indipendente dei moltiplicatori (in particolare il cumulo nei cap 10-15); test "giocatore che ignora tutto" (non deve mai bloccarsi né andare in mora a eff 0,85); casi limite blackout + notte + contratto refrigerato; verifica che nessun acquisto in Bulloni tocchi Corrente/officina oltre il timer.
- **LiveOps:** il Fronte GrayBox settimanale va agganciato alle stagioni "Segnali di NULO"? Rewarded "Giornata di sole" sì o no (rischio percezione P2W)? KPI da tracciare: % di bollette in attivo, guasti/giorno, contratti persi, quartieri difesi; A/B sulla durata della manutenzione.
### Risposta LiveOps (LiveOps & Monetizzazione, 15/10/2026)
Skill seguite: product-analytics, analytics-tracking, ab-test-setup, churn-prevention, paywall-upgrade-cro, pricing-strategy. Tutti i numeri sono **stime da ricalibrare** in soft launch.

**1) Fronte GrayBox e stagioni "Segnali di NULO": sì, agganciarlo, ma in modo leggero.**
- L'attacco resta **settimanale**, dal lunedì alla domenica, e non sostituisce il Weekend del Segnale.
- La stagione dà il **tema** dell'attacco:
  - S1: manifesti generici;
  - S2: GrayBox occupa il Promontorio del Faro;
  - S3: ronde al Molo Basso;
  - S4: GrayBox assalta il cantiere.
- Ogni quartiere difeso vale **+1 contributo al co-op mensile dell'antenna** e una missione del pass ("difendi 2 quartieri").
- Fino al cap 10 non si nomina NULO: è solo "GrayBox alza il tiro".
- **Nessuna ricompensa a pagamento** e nessun attacco più duro durante gli eventi di monetizzazione.
- Per chi ha finito la storia (post-game), l'attacco vale sui distretti della rete aperta.

> **Esito v1.1: NON accolta da Lorenzo**: nessuna rewarded sui sistemi gestionali, anche dentro il tetto dei totem. Il placement `giornata_sole` va tolto dagli eventi analytics. **v1.2:** anche la guardrail "spesa di Bulloni sull'officina" non serve più, perché i Bulloni non toccano l'officina; `bulloni_spesi` va tolto da `robot_maintenance_*`.

**2) Rewarded "Giornata di sole": rischio pay-to-win basso-medio. PROPOSTA, la decisione spetta a Lorenzo.**
- Il rischio sta nella percezione: è un boost economico su un sistema gestionale, quindi "paghi o guardi per gestire meglio".
- **Raccomandazione: sì, ma dentro il tetto dei totem**, non come rewarded aggiuntiva. Le guardrail:
  - dal cap 5 (con i pannelli) e conta nel tetto 1/sessione e 4/giorno dei totem;
  - effetto solo sulla **produzione solare ×2 per 2 min**: non ripara guasti, non evita blackout, non tocca contratti, quote o debito;
  - **solo di giorno con il sole**: con le nuvole e di notte il totem non compare;
  - mai con i Bulloni e mai come IAP;
  - se la sim mostra che vale più di 0,5 min di gioco per uso, si dimezza.
- Metriche di controllo: la quota di "bollette in attivo" non deve dipendere dalla rewarded (scarto inferiore a 5 punti tra chi la usa e chi no) e le recensioni Play con "pay to win" vanno monitorate.

**3) KPI gestionali ed eventi analytics. PROPOSTA, i target spettano a Lorenzo.**
| KPI | Definizione | Target (stima) | Allarme |
|---|---|---|---|
| Bollette in attivo | % di giornate con Netto > 0 (cap ≥5) | 75-85% | <65% (troppo duro) o >95% (sistema inutile) |
| Guasti/giorno | guasti in turno per DAU (cap ≥5) | 0,3-0,8 | >1,2 |
| Contratti persi | % dei contratti accettati scaduti | 10-20% | >30% |
| Quartieri difesi | % degli attacchi settimanali difesi (cap ≥7) | 50-70% | <35% o >90% |
| Blackout/giorno | per DAU (cap ≥9) | 0,2-0,6 | >1 |
| Uso manutenzione programmata | % riparazioni prima del guasto | ≥60% | <40% |

Eventi Firebase/GA4 in snake_case, con i parametri comuni `chapter`, `cycle_day`, `session_id`, `assist_on`, `ab_variant`:
- `bill_closed` (netto, incassi, corrente, ricambi, affitto, penali, segno);
- `robot_breakdown` (robot_id, usura, in_turno);
- `robot_maintenance_start` / `robot_maintenance_end` (robot_id, usura, durata, bulloni_spesi);
- `contract_accepted` / `contract_completed` / `contract_failed` (tipo, quartiere, scadenza, ritardo);
- `district_attack_start` / `district_defended` / `district_lost` (quartiere, stagione);
- `blackout` (consumo, potenza, priorità);
- `rewarded_shown` / `rewarded_completed` (placement: cassa_x2, caffe_turbo, zaino_gonfio, giornata_sole).

Dashboard: andamento per capitolo e per coorte, separando i giocatori in Assist.

**4) A/B test sulla durata della manutenzione.**
- **Ipotesi:** con la manutenzione programmata più corta, i giocatori la usano di più prima del guasto, quindi ci sono meno guasti in turno e meno frustrazione. Senza però rendere banale il sistema, quindi senza far salire troppo le bollette in attivo.
- **Varianti** (fermo per manutenzione programmata; il fermo per guasto resta 2 cicli):
  - A: 1 ciclo (controllo, come 10 §3);
  - B: 0,5 cicli;
  - C: 1,5 cicli.
  - Assegnazione per utente, al primo ingresso nel cap 5.
- **Metrica primaria:** % di riparazioni fatte prima del guasto (programmate su totali). Baseline stimata 55%, effetto minimo rilevabile +8 punti.
- **Metriche secondarie:** guasti per DAU, retention D7 dall'ingresso nel cap 5, minuti per capitolo 5-8.
- **Guardrail:**
  - retention D7 non sotto il -2% relativo;
  - minuti per capitolo dentro 25-40;
  - bollette in attivo tra 65% e 95%;
  - spesa di Bulloni sull'officina non sopra il +15% (spia di pay-to-skip);
  - crash/ANR invariati.
- **Campione:** circa 600 utenti per variante che arrivano al cap 5 (alfa 0,05, potenza 0,8, su 55%→63%), quindi circa 1.800 in totale. Con un tasso di arrivo al cap 5 stimato al 35% degli installi, servono circa 5.200 installi.
- **Durata:** almeno 14 giorni, per due cicli settimanali del Fronte, senza fermarsi prima. Si decide sulla primaria solo se le guardrail tengono.

- **UI/UX:** priorità delle icone sopra i robot (max 1), layout della bolletta in 3 righe senza modale, app *Rete* e *Conti*, colori dei quartieri sulla mappa compatibili col daltonismo, feedback del blackout (luci che calano, non schermo nero).
- **Tech Lead:** costo dei sistemi in Unity (simulazione energia a tick 1 s, pathfinding dei robot verso colonnine/officina, code alle colonnine); salvataggio dello stato offline (batterie e usura offline: proposta congelati); meteo/luce solare dal ciclo URP già previsto; budget di performance su Android medio con 6 robot + furgone.
