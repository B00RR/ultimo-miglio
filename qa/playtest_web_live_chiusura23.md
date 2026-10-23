# QA LIVE chiusura 23/10 — NO-VIA

Build provata: https://b00rr.github.io/ultimo-miglio-web/dev/ — `dev/VERSIONE.txt` = commit `21055fc` (22/10 11:13 Europe/Rome). Non include la PR #285 (pillola, etichette, vaso): quella build è ancora in CI e non è pubblicata. Viewport 390×844, touch, Chrome headless con SwiftShader, storage del contesto nuovo. Screenshot in `qa/playtest_shots/chiusura23_*.png`.

Metodo: trascinamento del mouse sul canvas (lo stick compare al dito). «Salta tutorial» risponde al tap. Il giro completo dei 16 passi, gli acquisti e OLDO non sono stati giocati: dopo il salto il portafoglio è a 0 LN.

Verdetto: **NO-VIA**. Manca il protocollo intero (tre punti per muro, pad, Zaino, OLDO, reload, altri viewport). Sulla build pubblicata la pillola copre la testa di Kaia (#272).

| ID | Esito | Nota |
|---|---|---|
| C01 | PARZIALE | Un avvicinamento a nord: Kaia si ferma davanti ad arco/muro (`chiusura23_muro_n.png`). Non fatti i 3 punti né i due archi. |
| C02 | NON VERIFICATA | Il drag verso est cambia inquadratura (`chiusura23_muro_e.png`) ma non è il protocollo 5 s × 3 punti. |
| C03 | NON VERIFICATA | Idem sud (`chiusura23_muro_s.png`). |
| C04 | NON VERIFICATA | Idem ovest (`chiusura23_muro_o.png`). |
| C05 | NON VERIFICATA | Angoli non provati. |
| C06 | NON VERIFICATA | Scaffali non spinti di proposito. |
| C07 | NON VERIFICATA | Bancone non spinto sui 4 lati. |
| C08 | NON VERIFICATA | Props non spinti uno per uno. Il vaso sotto la Cassaforte è la #274, fix nella #285, non su questa build. |
| C09 | NON VERIFICATA | Recinzioni non provate. |
| C10 | PARZIALE | Il pad giallo si calpesta (`chiusura23_pillola.png`). Non tutti i pad. |
| C11 | NON VERIFICATA | OLDO non assunto. |
| C12 | NON VERIFICATA | Coda clienti non osservata. |
| C13 | NON VERIFICATA | OLDO non in scena. |
| A01 | NON VERIFICATA | Orientamento sulle 8 direzioni non girato. |
| A02 | NON VERIFICATA | Clip carry/pay non raggiunte. |
| A03 | NON VERIFICATA | OLDO assente. |
| A04 | NON VERIFICATA | OLDO assente. |
| A05 | NON VERIFICATA | Clienti non osservati. |
| A06 | VIA | Fumetto di Tilde leggibile all'avvio (`chiusura23_boot.png`). |
| P01 | NON VERIFICATA | Arrivi non caricati fino a MAX. |
| P02 | NON VERIFICATA | Scaffali non usati. |
| P03 | NON VERIFICATA | Bancone non usato. |
| P04 | NON VERIFICATA | Pad OLDO non raggiunto (0 LN). |
| P05 | NON VERIFICATA | Zaino non comprato (0 LN). |
| P06 | NON VERIFICATA | Scarpe/Insegna non comprate. |
| P07 | NON VERIFICATA | Serve OLDO assunto. |
| Z01 | NON VERIFICATA | Servono ≥40 LN giocati. |
| Z02 | NON VERIFICATA | Nessun acquisto. |
| B01 | VIA | Testo del passo «Muoviti» leggibile, senza ellissi, in `chiusura23_boot.png`. |
| S01 | NO-VIA | Tap stimato sull'ingranaggio (circa 352,28) non ha aperto il pannello: lo scatto dopo è uguale al muro nord (differenza di 2491 pixel, quasi solo animazione). Da rifare con il centro esatto del tasto. |
| S02 | NON VERIFICATA | Pannello non aperto. |
| S03 | NON VERIFICATA | Pannello non aperto. |
| S04 | NON VERIFICATA | Telefono non provato. Resta la #64. |
| S05 | NON VERIFICATA | Nessun valore cambiato. |
| V01 | NON VERIFICATA | Nessun reload a metà partita. |
| V02 | NON VERIFICATA | Nessuna chiusura/riapertura. |
| K01 | PARZIALE | Nei fermi visti Kaia resta in quadro (camera che la segue). Non i 5 s su Arrivi/Bancone/angoli. |
| K02 | PARZIALE | Negli scatti non esce dalla mappa. Non è il giro C01–C05. |
| M01 | VIA | 390×844 touch: pillola LN «0», giorno «G1», obiettivo, Salta e ingranaggio dentro lo schermo (`chiusura23_boot.png`). |
| M02 | NON VERIFICATA | 360×800 non aperto. |
| M03 | NON VERIFICATA | 1170×2532 non aperto. |
| M04 | VIA | Il drag muove Kaia (boot → pad → muri: le inquadrature cambiano). |
| F01 | NON VERIFICATA | FPS non campionati. Headless SwiftShader non vale come device. |
| F02 | NON VERIFICATA | Heap non campionato. |
| F03 | PARZIALE | Nel log due 404 e nessun errore shader né eccezione. Uno è il favicon atteso; l'altro URL non è nel messaggio del browser. Avvisi «sound which is not loaded yet» (la #165 non è su questa build: la sua CI è stata annullata). |
| R01 | NON VERIFICATA | I casi #41 #42 #43 #44 #49 non sono stati ripetuti uno per uno. |
| R02 | NO-VIA | Il tutorial non è stato giocato fino in fondo. «Salta tutorial» funziona (`chiusura23_salta.png`, obiettivo «Il resto lo scopri da te»). |

Bug visto di nuovo, già aperto: **#272**. Con Kaia sul pad la cifra «40» le copre la testa (`chiusura23_pillola.png`). La correzione è nella #285, non in questa build.
