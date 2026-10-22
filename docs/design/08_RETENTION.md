> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# 08 - RETENTION & POST-GAME v1 (Game Designer, co-autore LiveOps)
Riferimenti: 00_BIBBIA (vincoli Solara, joystick, solo Android), 01_CORE_LOOP v3 (§6 economia, §7 scadenza, §9 offline), 03_MONETIZZAZIONE v2 (stagioni, pass, regole anti pay-to-win). La storia dura ~8 h attive (~480 min, 15 capitoli). Con 3-5 sessioni da 5-8 min al giorno sono ~2-3 settimane di calendario reale. Questo documento copre cosa succede durante e dopo.
**Regole fisse:** i Bulloni servono solo a timer e cosmetici, mai a prestigio, distretti o avanzamento. Ogni collezionabile di Solara si ottiene giocando; a pagamento ci sono solo varianti cosmetiche. Niente spoiler: prima del cap 10 il segnale di NULO è solo un'"interferenza misteriosa".

## 1. Perché si torna
| Orizzonte | Motivo principale | Supporto |
|---|---|---|
| **Giornaliero** | La Cassa piena (offline fisico, 01 §9) e le missioni drone concluse | 3 ordini di Auntie Lulu, streak di login, 1 corsa di Kenzo (3 tentativi base), totem rewarded |
| **Settimanale** | Il Weekend del Segnale (72 h, mini-diorama a joystick con Crediti Segnale propri) | Quest settimanali di Auntie Lulu, leghe delle corse Bronzo/Argento/Oro, streak di 7 giorni |
| **3 mesi** | Rete aperta e distretti di Solara (post-game), stagioni di NULO, collezione completa | Pass "Libretto del Corriere" (28 gg), evento co-op mensile per l'antenna di NULO, rinascite |
Il ritorno deve avere una **ragione fisica nel diorama**: monete nella Cassa, droni che atterrano, un cliente nuovo sulla porta. Niente popup di richiamo.

## 2. Post-game (dopo il cap 15)
**2.1 Nuovi depositi/distretti (aperti da Kaia).** Con il debito saldato Kaia apre filiali della rete libera in altri quartieri di Solara, tutti di fantasia:
- **Molo Basso** (di giorno): banchine semiallagate, carico su chiatte.
- **Terrazze Alte**: scalinate ripide, consegne in ascensore a fune.
- **Promontorio del Faro**: logistica del razzo e consegne orbitali simboliche.
- **Quartiere dei Limoneti**: merci fragili e a tempo.
Ogni distretto è un diorama nuovo con 5 "mini-capitoli" (~25-30 min l'uno), una meccanica fisica sua e un'economia scalata dal cap 15 (riusa la sim v3 con una nuova base). Si sblocca con LN e Reputazione, mai con Bulloni.

**2.2 Prestigio / "Rifondazione".** Quando un distretto è completo, Kaia può "passarlo alla cooperativa":
- il deposito riparte dal livello 1 di Zaino, Scarpe e Insegna;
- Kaia guadagna **Stelle della Rete**, un bonus permanente (+10% sul valore del pacco per Stella, tetto 10) che vale per tutti i depositi;
- la rifondazione è facoltativa e la trama resta intatta. Si rigioca solo il loop, che diventa più rapido: il secondo giro dura circa il 60% del primo.
- Stelle e rifondazioni non si comprano.

**2.3 La rete aperta (meta-game).** Sul telefono c'è una mappa della rete, sola consultazione come la Flotta (01 §11):
- I depositi sono **collegati** fra loro. Ognuno manda un flusso passivo ai vicini: la rotta X→Y aggiunge il 5% delle vendite automatiche di X alla Cassa di Y.
- La Cassa ha ancora un tetto (01 §9). Il flusso serve a far tornare il giocatore, non a farlo giocare da solo.
- **Rotte speciali**: richieste che coinvolgono 2-3 depositi ("porta 30 pesanti dal Cortile al Molo"). Si risolvono a joystick spostandosi fra i depositi e hanno ricompense cosmetiche.
- Obiettivo di lungo periodo: **accendere tutti i nodi** della rete di Solara.

**2.4 Stagioni "Segnali di NULO"** (28 gg, allineate al pass, 03 §3):
- Ogni stagione decodifica un frammento delle coordinate e apre una zona temporanea.
- Dopo il finale le stagioni portano alle destinazioni di NULO, come epiloghi narrativi brevi.
- Prima del cap 10 lo stesso contenuto si presenta come "interferenza": stesse ricompense, nessuna rivelazione.

## 3. Collezionabili (tutti ottenibili giocando)
- **Robot**: varianti dei 5 robot (OLDO "vintage", FIZZ "ape", ecc.) da quest, corse, Weekend e pass gratuito. Più 3 **robot minori** da collezione (es. un gabbiano-drone), che si guadagnano nel post-game con un piccolo bonus cosmetico o di qualità della vita.
- **Skin**: outfit di Kaia, livree del furgone e dei droni, zaini.
- **Arredi del deposito**: piante di limone, lanterne del Molo, foto di Oma Tilde, insegne. Si posano su slot a terra fissi e non cambiano l'economia.
- **Album di Solara**: sul telefono, set per tema (Gradoni, Molo, Faro, Madame Moth). Un set completato dà un titolo e un achievement.
- **A pagamento**: solo varianti di colore o animazione di oggetti che esistono già. Nessun collezionabile è esclusivo a pagamento.

## 4. Obiettivi di lungo periodo
- Epilogo "Puntuale" (01 §7).
- 10 Stelle della Rete.
- Tutti i nodi della rete accesi.
- Album completo.
- Lega Oro nelle corse.
- Le 4 stagioni di NULO decodificate.
- Achievement di Google Play (~40), divisi in storia, abilità (combo x2 per 60 s, notte senza essere visti) e collezione.

## 5. Social leggero (lancio Android, niente backend pesante)
- **Google Play Games Services**:
  - classifiche: corse di Kenzo (solo i 3 tentativi base), LN/min massimo, Stelle della Rete;
  - achievement;
  - salvataggio in cloud (Saved Games).
- **Visite asincrone ai depositi**: uno snapshot JSON del layout e degli arredi, salvato su storage statico o come blob Saved Games condiviso tramite codice o link. Chi visita cammina in un diorama in sola lettura e lascia un "timbro". Chi riceve il timbro guadagna 1 cosmetico a set. Nessuna economia condivisa e nessun furto.
- **Share card**: immagini generate sul dispositivo (il deposito di notte, il Faro che si riaccende, il record di una corsa) da condividere con lo share intent di Android. Hanno un watermark e un codice di visita.
- **Co-op mensile per l'antenna**: un unico contatore globale aggregato (endpoint minimo). Le soglie collettive danno premi a tutti.
- **Gruppi di 50 del Weekend**: matchmaking lato server leggero o leaderboard Play con una finestra temporale.

## 6. Leve D1 / D7 / D30
| Momento | Leva |
|---|---|
| **D1** | Chiudere la prima sessione con un "gancio fisico": il drone di FIZZ in volo, la Cassa che si riempie dal cap 1 grazie al banco self-service di OLDO (50% della resa dei robot, capienza 4% della tranche, offline ≤20% delle LN del capitolo, 01 §9). Streak del giorno 2 visibile alla Cassaforte. Nessun interstitial nei cap 1-2. |
| **D7** | Atto I chiuso (cap 3-4 entro 4-6 giorni), il primo Weekend del Segnale, la streak di 7 giorni (cosmetico robot), il pass a metà percorso, la prima lega delle corse. |
| **D30** | Il finale è vicino o raggiunto. Primo distretto post-game, prima rifondazione, cambio di stagione e di pass, evento co-op dell'antenna. Il giocatore lento ha l'assist (§8). |

## 7. Notifiche (non invasive, con tetto)
- **Massimo 2 al giorno**, mai fra le 22:00 e le 09:00 ora locale.
- Solo quando c'è un valore reale:
  - la Cassa è piena;
  - la streak sta per scadere;
  - è iniziato un evento (Weekend, co-op, stagione).
- **Nelle prime 24 h** solo "Cassa piena".
- **Dopo 3 notifiche ignorate** si scende a 1 al giorno. Si torna a 2 dopo un'apertura spontanea.
- Tono di Kaia o Auntie Lulu, senza colpevolizzare ("La Cassa trabocca di monete!"). Mai notifiche sulla mora o su Maxim.
- Interruttori per categoria nelle opzioni. Su Android 13+ il permesso si chiede dopo il primo ritiro alla Cassa, non all'avvio.

## 8. Ammorbidire la scadenza di 1 anno (coerente con 01 §7 v3.3)
Regole base (01 §7):
- 1 ciclo = 7 min attivi ≈ **4,26 giorni**, quindi ogni rata (24,3 giorni) ha **40 min attivi**;
- **storia lunga (11 v1.1):** dal cap 2 i giorni per ciclo calano per capitolo (3,03 → 1,39), così il giorno 365 cade alla fine del cap 15; il cap 1 resta a 4,2583 (40 min). "Puntuale" = nessuna rata in mora (01 §7). L'Assist si propone alla 1ª mora, alla 2ª proroga o con un capitolo oltre i 135 min;
- ogni rata ha almeno 40 min attivi dal pagamento della precedente;
- mora = +10% sulla tranche e **Pedaggio GrayBox -15% su ogni LN** finché la rata non è saldata;
- nessun game over.
Per i giocatori lenti:
- **Gettoni Proroga** (sistema unico):
  - **1 a inizio di ogni Atto** (cap 1, 4, 9, 13), **tetto 3**;
  - si consumano in automatico alla prima scadenza mancata di una rata: **+1 ciclo (7 min) solo per la rata corrente**, senza mora;
  - non si comprano, né con Bulloni né con rewarded.
- **Modalità Assist** (opzionale, attivabile dalla Cassaforte dopo 2 more o in qualsiasi momento dalle opzioni):
  - **tranche -10% dal cap 4** (i cap 1-3 sono già scontati del 5% per tutti);
  - calendario più lento (1 ciclo = 4 giorni, quindi 42,6 min per rata);
  - combo più tollerante (finestra di 7 s);
  - offline ≤12% delle LN del capitolo (20% senza Assist), così chi gioca veloce con Assist resta ≥25 min a capitolo.
  - L'epilogo "Puntuale" resta ottenibile. L'achievement "Puntuale" ha una variante "Puntuale (Assist)" che non penalizza.
- **Nessuna pressione nelle notifiche** e niente countdown rossi fuori dalla Cassaforte.
- **Lettura in sim** (`qa/econ_sim_v3.py` v3.3, `qa/sim_v33_matrix.txt`; verifica incrociata su `qa/econ_sim.py` per i cap 1-3):

| Giocatore | Senza Assist | Con Assist |
|---|---|---|
| eff 0,7, mai offline | GD 36,1-39,6 min/cap, 0 more; QA cap 2 in mora (45,0) | GD 32,8-39,6; QA 39,7-40,9, 0 more |
| eff 0,85, mai offline | GD 35,0-36,5, QA 37,4-37,8, 0 more | 31,7-36,4, 0 more |
| eff 1,0, 4×5 (riferimento) | 29,5-30,8, 0 more | 27,3-30,3, 0 more |
| eff 1,2, 3×3 con ad + totem + Turno di notte | 25,7-27,1 | 25,8-28,6 |


## 9. Note LiveOps
*(Sezione riservata a LiveOps & Monetizzazione: ritmo delle stagioni, A/B test, KPI.)*
Vincoli già concordati:
- notifiche come in §7;
- pass "Libretto del Corriere" di 28 giorni, premium a 4,99 €, percorso gratuito giocabile per intero e completabile con 20-25 min al giorno;
- Weekend del Segnale settimanale di 72 h;
- co-op mensile per l'antenna;
- quest settimanali di Auntie Lulu;
- interstitial mai con il joystick attivo e nessuno nei cap 1-2;
- classifica delle corse solo con i 3 tentativi base.

**Calendario delle stagioni (28 gg ciascuna, allineate al pass).** Fino al cap 10 ogni stagione si presenta come "Interferenza #n", con le stesse ricompense e senza nomenclatura di NULO.
| Stagione | Tema (dopo il cap 10 / prima del cap 10) | Co-op mensile per l'antenna | Ricompense del pass (gratuito / premium) | Distretto in evidenza |
|---|---|---|---|---|
| **S1 "Interferenza"** | NULO capta il primo frammento / ronzio misterioso nella Cantina | "Antenna sul tetto": 1 M di consegne globali | Gratuito: zaino Limoneto e 60 Bulloni. Premium: outfit "Corriera di Borgo Gradoni", livrea FIZZ "ape", 150 Bulloni e 40 Gettoni | Borgo Gradoni (base) |
| **S2 "Rotta del Faro"** | Il segnale punta al Faro Spento / il Faro sfarfalla di notte | "Riaccendi il Faro": 2 M di pacchi, con 3 soglie | Gratuito: lanterna del Faro (arredo). Premium: furgone "Guardiano", OLDO "vintage", arredi del Faro | Promontorio del Faro (post-game) |
| **S3 "Madame Moth Parla"** | La notte al Molo Basso, i segreti di Oma Tilde / ordini notturni più ricchi | "Rete del Molo": consegne notturne senza essere visti | Gratuito: lanterna del Molo. Premium: outfit stealth, TIKTAK "rame", 60 Gettoni | Molo Basso (post-game) |
| **S4 "Oltre la Luna"** | Le coordinate complete, epilogo breve / "segnale dal cielo" sul cantiere della Dr. Nova Kade | "Antenna orbitale": l'obiettivo globale più alto | Gratuito: insegna Sunn Depot "stellare". Premium: tuta orbitale di Kaia, NULO "costellazione", arredo razzo | Terrazze Alte / Limoneti (post-game) |

Regole del pass:
- 30 livelli. Il premium dà solo cosmetici, Bulloni (timer) e Gettoni, mai pad o livelli di robot.
- I cosmetici delle stagioni passate tornano in rotazione dopo 6 mesi e si ottengono giocando (nessuna esclusiva a pagamento, §3).
- Dopo S4 il ciclo riparte con nuove destinazioni di NULO.
- Ogni stagione apre un distretto post-game, così chi finisce la storia trova contenuto nuovo ogni 28 gg.

**Weekend del Segnale per chi ha finito la storia.**
- Stesso mini-diorama a joystick e stessi Crediti Segnale, con il **livello "Rete"**: difficoltà scalata sulle Stelle della Rete (+1 cliente/min ogni 2 Stelle), rotte speciali fra 2 depositi-evento e pacchi pesanti.
- Gruppi da 50 separati fra **storia** e **post-game**, così i veterani non schiacciano i nuovi.
- Premi: Album di Solara, robot minori (gabbiano-drone), Gettoni. Mai Stelle né bonus permanenti all'economia.

**KPI di retention post-game (Android, stime da ricalibrare in soft launch).**
| KPI | Soglia | Obiettivo | Leva |
|---|---|---|---|
| D30 | 5% | 6-8% | finale, primo distretto |
| D60 | 2,5% | 3,5-5% | S2/S3, Rifondazione, rete |
| D90 | 1,5% | 2-3,5% | S4, collezione, leghe |
| Payer post-game (% dei DAU D60+) | 4% | 5-8% | pass, cosmetici |
| Partecipazione al Weekend (DAU D30+) | 35% | 50% | gruppi separati |

Il rapporto D90/D30 resta intorno al 35-45%: è una stima per un hybrid-casual con un meta-game che regge.

**Valori di Proroga e Assist (da validare con A/B test e con `qa/econ_sim_v3.py`).**
- **Gettoni Proroga** (sistema unico, come §8):
  - 1 a inizio di ogni Atto (cap 1, 4, 9, 13), tetto 3;
  - si consumano in automatico alla prima scadenza mancata: +1 ciclo (7 min attivi, ~4,26 giorni) **solo sulla rata corrente**, senza mora;
  - mai acquistabili, né con Bulloni né con rewarded;
  - test A/B: tetto 3 contro 2.
- **Mora:** +10% sulla tranche e **Pedaggio GrayBox -15% sulle LN** finché la rata non è saldata. Nessuna rewarded o IAP lo rimuove.
- **Assist** (valori del §8):
  - tranche -10%, solo dal cap 4 (i cap 1-3 hanno già tranche scontate);
  - 1 ciclo = 4 giorni, invece di ~4,26 (finestra della rata da 40 min);
  - finestra combo di 7 s;
  - offline ≤12% delle LN del capitolo.
  - Test A/B solo sulla soglia di proposta (dopo 1 mora contro 2 more).
  - Gratis, reversibile, senza malus al pass o alle classifiche.
  - Metrica: un giocatore con Assist deve avere una retention D30 di almeno 0,9 volte quella dei giocatori senza Assist.
- **Rewarded collegate:**
  - totem a tempo Caffè Turbo e Zaino Gonfio (2 min): dal cap 4, al massimo 1 per sessione e 4 al giorno;
  - x2 alla Cassa: al massimo 2 al giorno, con l'offline che resta ≤20% delle LN del capitolo (≤12% in Assist).
  - Tetto complessivo delle rewarded: 8-10 al giorno. Restano volontarie e non toccano mai debito, mora o Proroga.
- **Bulloni:** solo accelerazione dei timer (1 Bullone ogni 3 min) e cosmetici.

**Tetti alle notifiche (vincolanti, come §7).**
- Al massimo 2 al giorno.
- Nessuna fra le 22:00 e le 09:00 ora locale.
- Nelle prime 24 h nessuna notifica, tranne "Cassa piena" (Cassa alla capienza: 4% della tranche, 6% con il pad "Turno di notte").
- Dopo 3 notifiche ignorate si scende a 1 al giorno; si torna a 2 dopo un'apertura spontanea.
- Nei test A/B si possono cambiare solo testi e orari, mai questi tetti.

**A/B test generali:**
- cadenza del Weekend (72 h contro 48 h);
- soglie del co-op;
- prezzo del pass (4,99 € con un tier regionale più basso).

## 10. Le 5 leve principali
1. **Cassa fisica piena**: l'offline con tetto che fa tornare ogni giorno, con una notifica sola se c'è valore.
2. **Weekend del Segnale + stagioni di NULO**: un motivo settimanale e mensile, con una narrazione che continua oltre il finale.
3. **Rete aperta post-game**: distretti nuovi, depositi collegati e Rifondazione con Stelle della Rete per l'orizzonte dei 3 mesi.
4. **Collezione di Solara giocabile**: robot, skin, arredi e album, tutti guadagnabili, più le visite asincrone e le share card.
5. **Scadenza che perdona**: Gettoni Proroga, finestra minima per rata e Assist, così il giocatore lento non abbandona.
