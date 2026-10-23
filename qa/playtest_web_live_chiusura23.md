# QA chiusura 23/10 — NO-VIA

Due superfici, stesso viewport 390×844, touch, Chrome headless con SwiftShader, storage nuovo. I testi citati sono quelli letti da Tesseract sui PNG (confidenza ≥ 55), non una descrizione a occhio.

1. Sito pubblicato: https://b00rr.github.io/ultimo-miglio-web/dev/ — `dev/VERSIONE.txt` = `21055fc` (22/10 11:13 Europe/Rome). Non contiene la #285.
2. Artefatto `webgl` della run CI [39935828921](https://github.com/B00RR/ultimo-miglio/actions/runs/39935828921) (commit del codice della #285, prima di questo report), servito in locale. Non è sul link dev.

Verdetto: **NO-VIA**. Il protocollo intero non è stato eseguito (tre punti per muro, pad uno per uno, Zaino, OLDO, reload, 360×800, 1170×2532, FPS). Il giro lungo non si chiude da headless senza una mappa dei tap.

| ID | Esito | Nota |
|---|---|---|
| C01 | NON VERIFICATA | `chiusura23_muro_n.png` non è un muro: è il pannello Impostazioni aperto (vedi S01). Niente protocollo 5 s × 3 punti. |
| C02 | NON VERIFICATA | Il drag verso est cambia l'inquadratura e il fumetto (`chiusura23_muro_e.png`: «C'è Auntie Lulu al Bancone»). Non è il protocollo. |
| C03 | NON VERIFICATA | A sud compare «MAX 5/5» (`chiusura23_muro_s.png`). Non è una spinta di 5 s sul muro. |
| C04 | NON VERIFICATA | Ovest non protocollo. |
| C05 | NON VERIFICATA | Angoli non provati. |
| C06 | NON VERIFICATA | Scaffali non spinti di proposito. «MAX 5/5» è solo un'etichetta letta di passaggio. |
| C07 | NON VERIFICATA | Bancone non spinto sui 4 lati. |
| C08 | NON VERIFICATA | Props non spinti uno per uno. Il vaso sotto la Cassaforte (#274) è nel codice della #285 e nel test PlayMode `VasoSottoCassa274Test`, non in questo giro manuale. |
| C09 | NON VERIFICATA | Recinzioni non provate. |
| C10 | NON VERIFICATA | Non ho una prova OCR di un pad calpestato con la cifra del prezzo. Vedi nota #272 sotto. |
| C11 | NON VERIFICATA | OLDO non assunto. |
| C12 | NON VERIFICATA | Coda clienti non osservata. |
| C13 | NON VERIFICATA | OLDO non in scena. |
| A01 | NON VERIFICATA | Otto direzioni non girate. |
| A02 | NON VERIFICATA | Clip carry/pay non raggiunte. |
| A03 | NON VERIFICATA | OLDO assente. |
| A04 | NON VERIFICATA | OLDO assente. |
| A05 | NON VERIFICATA | Clienti non osservati di proposito. Il fumetto cita Auntie Lulu. |
| A06 | VIA | Fumetto di Tilde leggibile. Sulla build dev: «Kaia, sgranchisci le gambe. Il deposito si …» (`chiusura23_boot.png`). Sull'artefatto #285: «Kaia, il Sunn Depot ora è tuo. Un anno per / salvarlo. Coraggio.» (`chiusura23_build285_fumetto.png`). |
| P01 | NON VERIFICATA | Arrivi non caricati fino a MAX. |
| P02 | NON VERIFICATA | Scaffali non usati di proposito. |
| P03 | NON VERIFICATA | Bancone non usato. |
| P04 | NON VERIFICATA | Pad OLDO non raggiunto (0 LN). |
| P05 | NON VERIFICATA | Zaino non comprato. |
| P06 | NON VERIFICATA | Scarpe/Insegna non comprate. |
| P07 | NON VERIFICATA | Serve OLDO assunto. |
| Z01 | NON VERIFICATA | Servono ≥40 LN giocati. |
| Z02 | NON VERIFICATA | Nessun acquisto. |
| B01 | VIA sull'artefatto | Sulla #285 il cold open sta su due righe, senza puntini di troncamento. Sulla build dev la riga finisce con «…» (`chiusura23_boot.png`, parola OCR «...» a confidenza 92): lì il fumetto è tagliato. |
| S01 | VIA | Tap sull'ingranaggio (circa 352–354, 28–30) apre Impostazioni. Sulla build dev il pannello è in `chiusura23_muro_n.png`: titolo, Visuale, Zoom, Rotazione, Inclinazione 50°, Musica, Suoni. Sull'artefatto #285 si apre (`chiusura23_build285_impostazioni.png`) e un tap in basso a sinistra lo chiude (lo shot successivo non ha più quelle etichette). |
| S02 | PARZIALE | Le etichette Zoom / Rotazione / Inclinazione si leggono, con valori 100x non letto qui, Rotazione «o» (0) e Inclinazione 50°. Gli slider non sono stati mossi. |
| S03 | NON VERIFICATA | Musica e Suoni sono in pannello. Non ho spostato i cursori né sentito l'audio (headless). |
| S04 | NON VERIFICATA | Telefono non provato. Resta la #64. |
| S05 | NON VERIFICATA | Nessun reload dopo un cambio. |
| V01 | NON VERIFICATA | Nessun reload a metà partita. |
| V02 | NON VERIFICATA | Nessuna chiusura/riapertura. |
| K01 | PARZIALE | Negli scatti senza pannello Kaia resta in quadro (la camera la segue: le inquadrature cambiano col drag). Non i 5 s su Arrivi/Bancone/angoli. |
| K02 | PARZIALE | Negli scatti non esce dalla mappa. Non è il giro C01–C05. |
| M01 | VIA | 390×844: pillola del fumetto, obiettivo e ingranaggio dentro lo schermo. Sull'artefatto, in alto a destra dopo il movimento si legge «agli Arrivi» (`chiusura23_build285_arrivi.png`). |
| M02 | NON VERIFICATA | 360×800 non aperto. |
| M03 | NON VERIFICATA | 1170×2532 non aperto. |
| M04 | VIA | Il drag muove Kaia. Sulla build dev il fumetto passa da «sgranchisci le gambe» a «Ogni pacco al suo scaffale» e poi a «C'è Auntie Lulu al Bancone». Sull'artefatto il log dice cold open 15 s e poi il passo Move. |
| F01 | NON VERIFICATA | FPS non campionati. SwiftShader non vale come device. |
| F02 | NON VERIFICATA | Heap non campionato. |
| F03 | PARZIALE | Due 404 nel log e nessun errore shader né eccezione. Il messaggio del browser non include l'URL (né `response` né `requestfailed` di Playwright lo vedono). Avvisi «sound which is not loaded yet». Sull'artefatto: «[UM VFX] shader prewarm: 9 materiali». |
| R01 | NON VERIFICATA | I casi #41 #42 #43 #44 #49 non sono stati ripetuti uno per uno. |
| R02 | NO-VIA | Il tutorial non è stato giocato fino a OLDO e Zaino. «Salta tutorial» non è in scena al cold open: il codice lo accende solo dopo il passo Combo (`TutorialManager.SaltaDopo`). Il tap in alto che nella prima bozza sembrava un salto ha solo coinciso col cambio di passo dovuto al movimento. Nessuno shot contiene «Il resto lo scopri da te». |

## Nota sulla #272

In `chiusura23_pillola.png` Tesseract non legge una cifra «40». Intorno a (160, 420) c'è un segno giallo di circa 20 px, non un cartellino che copre la testa. Questo giro **non conferma** il bug a schermo. La correzione resta nel codice della #285 (`PricePillLayout`, test EditMode `PricePill272Tests`) e va giudicata dai test CI, non da questo scatto.

## Cosa non è un VIA

Checklist `qa/CHECKLIST_CAP1.md`: il VIA di capitolo richiede tutte le voci. Qui la maggior parte è non verificata. Non chiudere la #66.
