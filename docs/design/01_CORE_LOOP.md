# 01 - CORE LOOP & ECONOMIA v3.3 (Game Designer)
Vincolo: GAMEPLAY in 00_BIBBIA.md. Controllo diretto di Nina con joystick virtuale a un dito, interazioni per prossimità. Nessun punta e clicca, nessun idle a menu. Il telefono serve solo per i menu secondari. Ambientazione: città fittizia **Solara**.
Sim di design: `qa/econ_sim_v3.py` v3.3 (`--eff`, `--sess NxL|none`, `--ad`, `--assist`, `--boost`, `--tdn`). Sim QA indipendente: `qa/econ_sim.py`. Output: `qa/sim_v33_matrix.txt`, `qa/sim_v33_eff{e}_4x5.txt`. Dati per Unity del cap 1: `data/ch1/`. Versioni precedenti in `_obsoleti/`. Correzioni da 07_REVIEW §7.

## 1. Core loop fisico
**30 s:** il furgone scarica sulla Zona Arrivi → Nina raccoglie → deposita allo scaffale del colore giusto → prende dallo scaffale e serve al Bancone → raccoglie le LN a terra.
**Tempi per zona:** 0,5 s di permanenza prima che l'azione parta; 0,1 s per pacco preso o posato; 1,0 s per cliente servito; 1,5 s di raccolta monete per giro; i pad incassano a curva accelerata (pad pieno in max 3 s di sosta).
**Clienti e abilità:** arrivano 30 + 5×(cap-1) clienti/min, **sempre più di quanti Nina riesca a servire** (27-45/min nella sim). La coda tiene fino a 20 s di **pazienza**: chi aspetta di più se ne va e la vendita è persa. Chi gioca meglio serve più clienti, quindi il ritmo dipende dall'abilità e dagli upgrade di Nina, non dalla domanda.
**Combo:** +0,1 per cliente servito, max x2. Si azzera se tra un cliente e il successivo passano **più di 5 s** (7 s in Assist). Dentro un giro i clienti sono a 1 s l'uno dall'altro; tra due giri conta il tempo di ritorno, quindi la combo sopravvive al giro solo se Nina è veloce (Scarpe, TICO, pila grande). Media nella sim: x1,5 al cap 1, x1,8 dal cap 10.
**Sessione (3-5 min):** Cassa (offline) → ordini speciali di Mamma Rosa → 1-2 pad → Cassaforte → missione drone (cap 3+).
**Ciclo giorno/notte = 7 min attivi ≈ 4,26 giorni di calendario** (4 in Assist, §7). Cap 1-5: giorno 6:55 + tramonto di 5 s. Dal cap 6: **giorno 5:00 + notte 2:00**. Il tempo scorre solo giocando.

## 2. Mappa del deposito (diorama continuo)
Bancone + Magazzino (cap 1: Arrivi, scaffali, Bancone, Cassa, Cassaforte) · Tetto/Pista droni (cap 3) · Smistamento con TICO (cap 4) · Garage (cap 5; box di Samir dal cap 7) · Retrobottega (cap 6, solo di notte) · Cortile con BRACCIO (cap 8) · Cantina con Carico 0/ECO (cap 10).
**Distanze:** Arrivi↔scaffali **6 m** (fissa); scaffali↔Bancone **9 m + 0,4 m per capitolo**, -25% dal cap 4 (TICO porta lo stock vicino al Bancone).

## 3. Pila, Zaino, Scarpe, Insegna
La pila parte da **5**. Con la pila piena compare MAX e Nina rallenta del 10%. Pad di potenziamento vicino alla Cassa. Zaino costa 40×2^(n-1) LN, Scarpe 60×2,7^(n-1) LN.
| Liv | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Zaino (pila) | 6 | 7 | 8 | 9 | 10 | 12 | 14 | 16 | 18 | 20 | 22 | 24 | 27 | 30 |
| costo LN | 40 | 80 | 160 | 320 | 640 | 1.280 | 2.560 | 5.120 | 10.240 | 20.480 | 40.960 | 81.920 | 163.840 | 327.680 |
| Scarpe (+vel.) | +6% | +12% | +18% | +24% | +30% | +36% | +42% | +48% | +54% | +60% | | | | |
| costo LN | 60 | 162 | 437 | 1.181 | 3.189 | 8.609 | 23.245 | 62.762 | 169.458 | 457.536 | | | | |
- **Zaino:** meno viaggi e combo più lunghe. **Scarpe:** circa 1 s in meno a giro. Visto che Nina è sempre il collo di bottiglia (§1), tutti e due alzano davvero le LN/min.
- **Insegna** (10 liv, 90×2,7^(n-1) LN): **+6% di valore medio per cliente** (più VIP), non più clienti. La domanda è già oltre la capacità di Nina.
- Acquisti scelti per **miglior guadagno di LN/min per LN spesa** (sim). Giocatore medio: Zaino 6 a fine cap 1, 10 al cap 4, 14 al cap 10; Insegna max al cap 11, Scarpe al cap 13 (§6).
**Pacchi pesanti (cap 8):** occupano **5 slot** della stessa pila, quindi Nina ne porta floor(pila/5) e li può mescolare con i normali. Valgono 6× un pacco normale. BRACCIO li sposta in automatico.

## 4. Pad a terra
Pad di Costruzione, Assunzione e Potenziamento, pagati a rate stando sopra. **"ASSUMI NONNO · 50 LN" a 2:10, confermato**: Nina ha già ~100 LN e lo paga in una sola sosta. Ogni capitolo ha un **pad obbligatorio** (stanza o robot, §6), pagato con le LN del portafoglio, non con quelle versate sul debito.

## 5. Robot: automazione tratto per tratto
**Nina può sempre fare tutto a mano**: rifornire gli scaffali dagli Arrivi, servire, smistare. I robot coprono una quota del lavoro; il resto tocca a lei.
| Robot | Cap | Ruolo | Liv 0 → +per livello | Costo liv n (max 10) |
|---|---|---|---|---|
| NONNO | 1 (2:10) | Restock Arrivi→scaffali (cap 1-3); banco self-service | restock 6 pacchi/min +1,5; banco 0,5 pacchi/min +0,2 | 100×2,4^(n-1) |
| PIPPA | 3 | Consegne drone (missioni 5 min-4 h) | 1 pacco/min +0,5 | 600×2,4^(n-1) |
| TICO | 4 | Smistamento completo; NONNO passa al Bancone | +2 pacchi/min fissi | sblocco col pad del cap 4 |
| BRACCIO | 8 | Pesanti | 1 pacco/min +0,5 | 15.000×2,4^(n-1) |
| ECO | 10 | Ordini nascosti (quest) | - | - |
**Restock di NONNO:** finché il restock di NONNO è inferiore alle vendite di Nina, lei rifornisce a mano la differenza. Esempio: al cap 1 lv 0 NONNO copre 6 pacchi/min su ~25, gli altri li porta Nina. Dal cap 4 TICO copre tutto il restock.
**Le vendite automatiche sono limitate ai clienti che Nina non serve**: i robot non creano domanda. VIP, ordini speciali, notte e raccolta monete restano a Nina.

## 6. Economia per capitolo (verificata con la sim)
Valore pacco = 10×1,5^(cap-1) LN. Il giocatore tiene il 40% delle LN per pad e upgrade; il 60% va sul debito. Le tranche dei cap 1-3 sono -5% rispetto alla v3.2; quelle dei cap 4, 5, 7-10 e 12 sono state limate dell'1-3% per garantire a eff 1,0 senza offline un margine ≥10% sulla finestra di 40 min.
| Cap | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| LN/pacco | 10 | 15 | 22 | 34 | 51 | 76 | 114 | 171 | 256 | 384 | 577 | 865 | 1.297 | 1.946 | 2.919 |
| Clienti/min | 30 | 35 | 40 | 45 | 50 | 55 | 60 | 65 | 70 | 75 | 80 | 85 | 90 | 95 | 100 |
| Tranche (k LN) | 8,4 | 18 | 33 | 70 | 117 | 130 | 205 | 325 | 505 | 790 | 1.200 | 1.950 | 3.000 | 4.500 | 6.800 |
| Pad obbl. (LN) | 0 | 400 | 800 | 1.500 | 2.500 | 4.000 | 6.000 | 9.000 | 13.000 | 19.000 | 27.000 | 38.000 | 55.000 | 80.000 | 115.000 |
Il cap 6 ha una tranche più bassa per compensare la notte (2/7 del tempo, pagata in Gettoni).

**Minuti attivi per capitolo, min-max sui 15 capitoli.** Profili:
- none = offline mai ritirato;
- NxL = N sessioni al giorno da L min;
- +ad = x2 alla Cassa (max 2 al giorno);
- full = +ad, totem Caffè Turbo/Zaino Gonfio (1 a sessione, max 4 al giorno, dal cap 4) e Turno di notte (dal cap 3).
| eff | none | 3×3 | 3×3+ad | 4×5 | 4×5+ad | 5×8+ad | 3×3 full | 4×5 full | 5×8 full |
|---|---|---|---|---|---|---|---|---|---|
| 0,7 | 36,1-39,6 | 29,2-32,4 | 29,2-32,3 | 30,9-34,2 | 30,0-33,1 | 31,7-34,7 | 27,8-32,3 | 28,7-33,1 | 29,4-34,7 |
| 0,85 | 35,0-36,5 | 28,4-30,0 | 28,4-29,9 | 30,1-31,6 | 28,9-30,2 | 30,8-33,2 | 27,0-29,9 | 27,5-30,1 | 28,1-32,1 |
| 1,0 | 33,4-35,7 | 27,0-28,8 | 26,9-28,8 | 29,5-30,8 | 27,9-29,6 | 29,1-32,5 | 26,7-27,6 | 26,9-28,7 | 28,2-30,8 |
| 1,2 | 31,8-35,0 | 25,7-28,4 | 25,7-28,4 | 27,5-30,1 | 26,3-29,4 | 27,9-32,1 | 25,7-27,1 | 26,3-28,1 | 27,1-30,4 |

- **Tutto in 25,7-39,6 min. Nessuna mora e nessuna proroga in nessun profilo.**
- A eff 1,0 senza offline il capitolo più lungo dura 35,7 min contro una finestra di 40: **margine 10,8%**.

**Assist** (tranche -10% dal cap 4, 1 ciclo = 4 giorni, finestra combo 7 s, offline ≤12% dal cap 4, 20% nei cap 1-3):
| eff | none | 3×3 | 3×3+ad | 4×5 | 4×5+ad | 5×8+ad | 3×3 full | 4×5 full | 5×8 full |
|---|---|---|---|---|---|---|---|---|---|
| 0,7 | 32,8-39,6 | 29,0-35,3 | 29,0-35,3 | 28,9-35,2 | 28,9-35,2 | 30,0-36,1 | 27,5-35,3 | 28,7-35,2 | 29,2-36,1 |
| 0,85 | 31,7-36,4 | 27,9-32,5 | 27,9-32,5 | 28,0-32,5 | 28,0-32,5 | 28,4-33,1 | 27,0-32,5 | 27,5-32,5 | 28,4-33,1 |
| 1,0 | 31,0-34,1 | 27,4-30,2 | 27,4-30,2 | 27,3-30,3 | 27,3-30,3 | 27,8-31,5 | 26,4-30,2 | 27,2-30,3 | 27,5-31,5 |
| 1,2 | 30,4-32,4 | 26,7-28,6 | 26,7-28,6 | 26,9-28,7 | 27,1-28,7 | 27,4-29,4 | 25,8-28,6 | 26,4-28,7 | 26,7-29,4 |

- **Tutto ≥25,8 min**, anche per i giocatori veloci con sessioni brevi e tutti i boost.

Dettaglio, eff 1,0, 4×5 senza ad:
```
cap|tranche|min attivi|Zaino|Scarpe|Insegna|NONNO|PIPPA|combo media|%offline|min cumul.|scadenza|stato
1|8400|30.0|6|2|3|2|0|1.48|12|30|40|ok
2|18000|29.5|8|3|4|4|0|1.63|12|60|80|ok
3|33000|29.7|9|4|5|5|2|1.68|12|89|120|ok
4|70000|30.5|10|4|6|5|2|1.71|13|120|160|ok
5|117000|30.8|11|5|7|5|4|1.73|15|150|200|ok
6|130000|30.1|11|6|7|5|4|1.76|14|181|240|ok
7|205000|30.1|12|6|8|6|5|1.76|14|211|280|ok
8|325000|30.1|13|7|8|6|5|1.78|14|241|320|ok
9|505000|30.0|13|7|9|7|5|1.8|14|271|360|ok
10|790000|30.1|14|8|9|7|6|1.81|14|301|400|ok
11|1200000|30.1|14|8|10|7|6|1.82|12|331|440|ok
12|1950000|30.3|14|9|10|9|8|1.82|14|361|480|ok
13|3000000|30.6|14|10|10|10|9|1.82|14|392|520|ok
14|4500000|30.3|14|10|10|10|10|1.82|14|422|560|ok
15|6800000|30.4|14|10|10|10|10|1.82|14|453|600|ok
Totale 453 min (7.5 h); range 29.5-30.8 min; more 0; proroghe usate 0
```

## 7. Scadenza di 1 anno (conta, ma non è game over)
- **Giorno N/365** è mostrato solo alla Cassaforte. 1 ciclo (7 min attivi) ≈ **4,26 giorni** (4 in Assist). La rata k scade al giorno round(365·k/15): ogni ~24,3 giorni = **40 min attivi** (42,6 in Assist). Il profilo di riferimento usa ~30 min, cioè ~25% di margine; a eff 1,0 senza offline il margine minimo è il 10,8%.
- **Finestra minima:** ogni rata ha **almeno 24,3 giorni (40 min) dal pagamento della precedente**. Chi resta indietro non va in mora a catena: la scadenza effettiva è max(giorno assoluto, pagamento precedente + 24,3 giorni).
- **Pagamento anticipato** (≥1 ciclo prima): +1 Reputazione e 5 Bulloni.
- **Gettoni Proroga** (sistema unico, coincide con 08 §8):
  - si guadagna **1 Gettone Proroga all'inizio di ogni Atto** (cap 1, 4, 9, 13), **tetto 3**;
  - alla prima scadenza mancata di una rata se ne consuma uno in automatico, se disponibile: la **sola rata corrente** ottiene +1 ciclo (+7 min, ~4,26 giorni) senza penalità; le scadenze successive non si spostano;
  - mai acquistabili.
- **Mora** (scadenza mancata senza Gettone): +10% sulla tranche e **Pedaggio OmniFreight -15% su ogni LN incassata**, attiva o dalla Cassa, finché la rata non è saldata. Scena con Viktor; i caselli OmniFreight compaiono sulla porta. Nelle sim v3.2 una rata in mora costava +5-6 min. Nella v3.3 non scatta in nessun profilo, perché la finestra è di 40 min.
**Calendario v-long (11 v1.1; si applica quando le tranche v-long entrano in produzione):**
- La scadenza di 1 anno copre **tutta la storia**. La rata k scade sempre al giorno round(365·k/15), ogni ~24,3 giorni.
- I **giorni per ciclo calano capitolo per capitolo**: gg/ciclo = 24,33 × 7 / finestra del capitolo. Cap 1 = **4,2583** (invariato, 40 min, giorno 24); cap 2 = 3,03 … cap 15 = 1,39.
- Finestra = 125% dei minuti di loop target, **escluso il cap 1** che resta a 40 min.
- Le missioni di trama e le scene **fermano il calendario**.
- **"Puntuale"** = tutte le 15 rate pagate entro la loro scadenza, quindi debito saldato entro il giorno 365, che cade alla fine del cap 15. Le proroghe non lo fanno perdere, la mora sì.
- Tabella in 11 §3.2.
- **Fine anno:** debito saldato entro il giorno 365 → epilogo "Puntuale" (Insegna d'oro + achievement; variante "Puntuale (Assist)"); altrimenti epilogo standard. Nessun game over.
- **Assist** (08 §8): tranche -10% dal cap 4 (i cap 1-3 sono già scontati), ciclo di 4 giorni, finestra combo 7 s, offline ≤12% delle LN **solo dal cap 4**; nei cap 1-3 resta al 20% (v3.4, da 07_REVIEW §8.2: così l'Assist non rallenta i primi capitoli).
- **Sim (GD e QA):** eff 0,85 senza offline non va mai in mora (GD: 35,0-36,5 min; QA cap 1-3: 37,4-37,8). eff 1,0 senza offline: GD 33,4-35,7, QA 35,2-35,9, quindi margine ≥10% in entrambe. eff 0,7 senza offline: GD puntuale (max 39,6); la QA va in mora al cap 2 (45,0 min) senza Assist e torna puntuale con l'Assist (39,7-40,9). La spinta morbida resta quindi solo sul giocatore più lento.

## 8. Notte (dal cap 6)
**2:00 min attivi su 7:00** (giorno 5:00, transizioni di 5 s incluse). Nel Retrobottega i pacchi della Civetta vanno al furgone evitando i coni di visione (2-3 ronde, giro di 20 s). Niente clienti, niente LN.
- **Gettoni:** 1 per pacco scottante consegnato, 2 se il Calore è 0; 6-10 per notte. Non si convertono in LN.
- **Cosa comprano:** progetti dei robot (sbloccano i livelli 6-10 di PIPPA e BRACCIO, 30-60 Gettoni), pezzi rari e cosmetici notturni (10-25). **Valore di riferimento:** 1 Gettone ≈ 1 min di gioco attivo.
- **Calore** pieno = ispezione, un robot fermo per 1 ciclo. "Chiudi bottega" salta la notte.

## 9. Offline: solo la Cassa
- **Unico metodo:** le LN offline si accumulano fisicamente nella Cassa e Nina le ritira camminandoci sopra. Al rientro c'è solo un toast di 1 riga; nessuna schermata modale.
- **Tasso = 50% del throughput reale dei robot** (vendite automatiche di NONNO, PIPPA, TICO e BRACCIO × valore pacco × Insegna). Alzare i livelli dei robot riempie la Cassa più in fretta, ed è il "tasso offline" che mostra la Flotta. È attivo dal cap 1, dopo NONNO (banco self-service).
- **Capienza = 4% della tranche del capitolo** (cap 1 = 336 LN, cap 3 = 1.320, cap 8 = 13.000). Col tasso dei robot ai livelli tipici si riempie in ~1 h al cap 1 e in 10-30 min dal cap 5: il tasso conta soprattutto nei cap 1-3 e con rientri ravvicinati (limite noto, §12).
- **Tetto rigido: l'offline non supera il 20% delle LN del capitolo** (12% in Assist dal cap 4; 20% anche in Assist nei cap 1-3), cioè 1/4 di quelle guadagnate giocando. Oltre il tetto la Cassa mostra "Piena: torna a lavorare!" e smette di riempirsi finché Nina non incassa altre LN in attivo.
- **x2 con rewarded o Bulloni**, chiesto sopra la Cassa, **al massimo 2 volte al giorno**. Il tetto del 20% vale anche con il x2.
- **Upgrade "Turno di notte":** pad unico al cap 3, **2.500 LN**: +50% di capienza della Cassa (6% della tranche). Il tetto del 20% resta. Simulato (profilo full): il minimo è 25,7 min.
- **Totem rewarded** (03 §2): Caffè Turbo (Scarpe +30%, 2 min) e Zaino Gonfio (pila +50%, 2 min). **Regola 01 v3.3: dal cap 4, al massimo 1 totem per sessione e 4 al giorno.** Simulati: minimo 25,7 min (25,8 in Assist).
- Durante la mora anche l'offline paga il Pedaggio (-15%).
- I droni completano le missioni anche offline; notte e corse no.

## 10. Valute
LN (soft) · Bulloni (hard: timer e cosmetici, mai avanzamento) · Gettoni (notte, §8) · Reputazione (+5% valore dei clienti VIP per livello; si somma all'Insegna).

## 11. Telefono di Nina e app Flotta
**Cap 1 (decisione DevOn 16/10, HUD minimo):** nel cap 1 il telefono non c'è; arriva dal cap 2 con Messaggi, Ordini e Flotta. Nel cap 1 la combo è solo un "+N" sopra Nina (1,5 s) e il debito si vede nella schermata di fine giornata e nel fumetto di Nonna Ada.
Il telefono contiene **solo menu secondari**: Messaggi, Ordini, **Flotta**, Quartiere, Bottega, Razzo.
**Flotta** (si sblocca con NONNO a 2:10), **in sola lettura**:
- per ogni robot: stato (lavora/fermo/ispezione), livello, pacchi/min, quota di restock coperta e contributo al tasso offline;
- riempimento della Cassa (es. "Cassa 210/336 LN · +1,2 LN/min · piena tra 1 h 45");
- missioni drone (cap 3) e prossimo pad consigliato.
Non si compra nulla: "Mostra la strada" accende la freccia verso il pad a terra.

## 12. Da allineare negli altri documenti (non modificati qui)
- 02_TECH r.22/36: rimuovere la schermata "Mentre eri via", formula offline = §9.
- 03: **allineato** da LiveOps (niente mancia offline, nessun "Turno di Notte Plus" a pagamento). Da riportare: tetto offline 20% (era 25%) e totem 1 per sessione / 4 al giorno dal cap 4.
- 08: §8 **allineato** a v3.3. In §9 LiveOps deve aggiornare l'Assist a -10% (era -15%), il tetto offline (20% / Assist 12%), i Gettoni Proroga (1 per Atto, tetto 3, +1 ciclo ≈ 4,26 gg) e il tetto rewarded per i totem.
- 04_UI §3.3: Flotta in sola lettura (tasso offline per robot); calendario alla Cassaforte; il chiosco è sostituito dal Pedaggio OmniFreight (caselli sulla porta, -15% sulle LN).
- Limite noto: con capienza al 4% della tranche la Cassa si riempie in fretta, quindi gli upgrade dei robot accelerano l'offline soprattutto nei cap 1-3.
