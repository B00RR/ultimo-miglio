# Checklist LIVE Cap.1 — Ultimo Miglio (riusabile per ogni LIVE)
Regola: ogni LIVE esegue TUTTE le voci. **VIA solo se tutte le voci sono VIA.** Ogni voce: esito VIA/NO-VIA + 1 screenshot (`qa/playtest_shots/<versione>_<ID>.png`). Un bug = una GitHub issue (prima cercare duplicati).
Setup: svuota localStorage/sessionStorage/IndexedDB, reload, boot ~18 s. Tasti iso: W su-destra, A su-sinistra, S giù-sinistra, D giù-destra. Muri: tasto tenuto 3–5 s.
Riferimenti codice: bordi `SliceSceneBuilder.cs` (Bound_N/S/E/O a ±11.15/±9.15), clamp Nina `NinaController.cs` (x ±8.2, z −10.2..7.0), kit muri/props `DepotKit.cs`.

| ID | Area | Caso | Atteso | Come testare |
|---|---|---|---|---|
| C01 | Collisioni | Muro N (z≈8.35) e archi N (x −5, +5) | Nina si ferma prima del muro e degli archi, niente attraversamento | W+A tenuto 5 s verso N in 3 punti (sx, centro, dx) + dentro ogni arco |
| C02 | Collisioni | Muro E (x≈7.6) e arco E (z −1) | Nina si ferma | D+W tenuto 5 s in 3 punti + arco |
| C03 | Collisioni | Bordo S (plinto z −11.15) | Nina si ferma, resta visibile | S+D tenuto 5 s |
| C04 | Collisioni | Bordo O (plinto x −9.15) | Nina si ferma, resta visibile | A+S tenuto 5 s |
| C05 | Collisioni | 4 angoli mappa (NE corner, NO, SE, SO) | Nessuna uscita, nessun incastro | Diagonale tenuta 5 s in ogni angolo, poi uscita in direzione opposta |
| C06 | Collisioni | Scaffali A/B/C (corpo, retro e lati) | Corpo solido, Nina non lo attraversa né si incastra | Spingi contro fronte/retro/lati 3 s |
| C07 | Collisioni | Bancone | Solido, nessun incastro | Spingi da 4 lati |
| C08 | Collisioni | Props decorativi solidi: vasi limoni, barile, cassaforte, panchina, cassa, carretto, cestino, secchio, lampione, pile pacchi Arrivi | Solidi | Spingi contro ciascuno 3 s |
| C09 | Collisioni | Recinzioni/fioriere | Solide | Come C08 |
| C10 | Collisioni | Pad (trigger) | Calpestabili, nessun blocco | Attraversa ogni pad |
| C11 | Collisioni | NONNO vs muri/scaffali/bancone | Non attraversa oggetti solidi | Strip ≥20 frame dei suoi viaggi |
| C12 | Collisioni | Clienti vs bancone/props/Nina | Non attraversano solidi | Strip della coda |
| A01 | Animazioni | Nina orientamento | Faccia nella direzione di moto in 8 direzioni | 8 tasti/diagonali, shot per ognuna |
| A02 | Animazioni | Nina idle/walk/carry/pay(deliver/cheer) | Clip corrette, niente T-pose/slide | Ferma, cammina, con pacchi, su pad acquisto |
| A03 | Animazioni | NONNO orientamento | Muso/forche verso la direzione di marcia, niente retromarcia | Strip ≥20 frame andata Arrivi→scaffale e ritorno |
| A04 | Animazioni | NONNO idle/walk/carry(lift)/drop | Clip corrette, niente slide | Strip |
| A05 | Animazioni | Clienti orientamento + walk/idle/pay | Faccia verso il moto, idle in coda, pay al bancone | Strip coda |
| A06 | Animazioni | Ada (se presente) | Fumetto/figura coerente | Shot nei momenti B4 |
| P01 | Pad pickup | Arrivi | Nina carica fino a MAX, centro e bordi attivano | Entra al centro e a ogni bordo |
| P02 | Pad consegna | Scaffali A/B/C (deposito + prendi) | Deposito e ritiro corretti per colore | Centro/bordi di ciascuno |
| P03 | Pad vendita | Bancone | Vendita, monete, LN aumentano | Consegna con clienti in coda |
| P04 | Pad acquisto | NONNO 10 LN | Prezzo visibile, LN scalano, NONNO appare | Centro pad giallo |
| P05 | Pad acquisto | Zaino 40 LN | Prezzo visibile | Vedi Z01 |
| P06 | Pad upgrade | Ogni altro pad nei sorgenti (`UpgradeKind`: Scarpe, ecc.) | Prezzo e LN visibili, effetto applicato | Elenco da `UpgradeKind`, test su ciascuno |
| Z01 | Zaino | Acquisto 40 LN (LN guadagnati giocando) | LN −40, capacità aumenta, obiettivo avanza | Gioca/aspetta vendite fino a ≥40, pad Zaino, ricarica Arrivi e conta MAX |
| B01 | Ada B4 | Fumetto Ada | Visibile, testo leggibile nei momenti previsti | Shot ai trigger da design 11_CAP1 |
| S01 | Impostazioni | Ingranaggio apri/chiudi | Pannello si apre/chiude | Tap |
| S02 | Impostazioni | Zoom | Cambia zoom camera | Slider/pulsanti |
| S03 | Impostazioni | Musica / SFX | On/off efficaci | Toggle |
| S04 | Impostazioni | TAB telefono | Apre/chiude | Tap |
| S05 | Impostazioni | Persistenza dopo reload | Valori mantenuti | Cambia, reload, ricontrolla |
| V01 | Salvataggio | Reload mantiene LN, acquisti, step tutorial, NONNO | Uguale a prima del reload, nessuna perdita/duplicazione | Annota stato, reload (senza clear), confronta |
| V02 | Salvataggio | Offline (se applicabile) | Progressione offline coerente | Chiudi/riapri dopo N s |
| K01 | Camera | Nina sempre in quadro a bordi Arrivi/Bancone/angoli | Nina nel 70% centrale da ferma | Ferma 5 s in ogni bordo (regressione v6/v7) |
| K02 | Camera | Nina non esce dalla mappa | Mai fuori | Vedi C01–C05 |
| M01 | Viewport | 390×844 touch (has_touch, is_mobile) | HUD/pillole/obiettivo leggibili, non tagliati | Contesto mobile, shot |
| M02 | Viewport | 360×800 touch | Idem | Idem |
| M03 | Viewport | 1170×2532 touch | Idem | Idem |
| M04 | Input touch | Tap e drag del joystick | Nina si muove nella direzione del drag | touchscreen/mouse drag |
| F01 | Prestazioni | FPS (rAF 2 s) e hitch (max frame) | ≥30 fps su device; headless: annotare | performance/rAF |
| F02 | Prestazioni | Memoria (performance.memory heap JS) | Stabile, no crescita continua | Campioni a inizio/metà/fine |
| F03 | Console | 0 errori Shader/NavMesh/eccezioni | Solo 404 favicon tollerato | Log console |
| R01 | Regressione | #41 #43 #44 #49 #42, B2–B5 | Restano risolti | Riprova casi originali |
| R02 | Regressione | Cap.1 completo (Arrivi→Scaffale→Bancone→NONNO→Zaino) | Tutti gli step tutorial avanzano | Gioco completo |
| C13 | Collisioni | Nina vs NONNO | NONNO non compenetra Nina (si ferma o la aggira) | Stai su Arrivi mentre NONNO carica, shot (aggiunta QA v9; bug #111) |
| P07 | Pad | Zone attive dopo un acquisto (pad che sparisce con Nina sopra) | Arrivi, Scaffali e Bancone funzionano subito dopo l'assunzione di NONNO, senza reload | Assumi NONNO, poi carica su Arrivi e vendi al Bancone (aggiunta QA v9; bug #107) |
| Z02 | Zaino | Pad dopo l'acquisto (livello successivo) | Prezzo del livello successivo visibile; nessun drenaggio LN non voluto | Resta sul pad dopo l'acquisto, osserva LN (aggiunta QA v9; bug #110) |
