# Scheda di osservazione playtest – Cap. 1 (onboarding → primo versamento rata 1)
_Rev. PR #106: HUD minimo, 16 passi (tolti `phone` e `ordersApp`); combo solo "+N" sopra Kaia; debito nel fumetto di Tilde e nella schermata di fine giornata._
Rif.: `12_CAP1_CRITERI_QA.md` §A, §E, §F (PT-01…07), §G mappatura passo→criteri (PR #96, rev. PR #106) · testi da `data/ch1/onboarding_testi.json` (incl. PR #92) · passi da `data/ch1/onboarding.json`.
Versione compilabile: `qa/scheda_osservazione_playtest.csv` (una riga per passo, per run).

> **Adattamento (decisione DevOn, 16/10):** niente reclutamento, niente moderatore, niente registrazioni né pensiero ad alta voce. La run "giocatore nuovo" la esegue il QA via Playwright sulla build con onboarding, **seguendo solo ciò che il gioco mostra** (testi, frecce, mano fantasma, camera). Il primo tester umano sarà Lorenzo, dopo il VIA. Il superamento si valuta sui criteri di `12_CAP1_CRITERI_QA.md` applicati alla singola run.

## 1. Intestazione della run
| Campo | Valore |
|---|---|
| Run n. / tipo | ☐ QA-Playwright ☐ Tester umano 1 (Lorenzo) |
| Esecutore | |
| Profilo | QA: agente che non usa conoscenze del design · Lorenzo: DA DEFINIRE |
| Device / viewport | (Playwright: browser, risoluzione verticale; umano: modello telefono) |
| Build / commit | |
| Data e ora (Europe/Rome) | |
| Cartella screenshot | `qa/playtest_shots/<run>/` |

## 2. Regole di esecuzione (QA-Playwright)
1. Agire solo in base a ciò che è a schermo: niente coordinate note, niente documenti di design, niente salti.
2. Un passo inizia al reveal (testo/elemento nuovo) e finisce al gate (evento di log di `onboarding.json`). Annotare i timestamp di gioco (m:ss) dai log dei gate.
3. Se per >10 s non è chiaro cosa fare dallo schermo: segnare **blocco >10 s**, screenshot, e attendere l'escalation di gioco (10/20/40 s). Nessun aiuto esterno; se oltre 60 s nulla cambia, annotare **blocco grave** e chiudere la run.
4. Screenshot obbligatori: al reveal di ogni passo, a ogni livello d'aiuto scattato, a ogni blocco, al gate.
5. Fine run: al reveal di `freePlay` (maxSec 0, passo terminale senza escalation) o a 10 minuti di gioco.

## 3. Scala del livello di aiuto (scattato nel gioco)
| Livello | Significato | Escalation `onboarding.json` |
|---|---|---|
| 0 | nessun aiuto: bastano testo e reveal | – |
| 1 | aiuto da 10 s | ripete suggerimento / bersaglio pulsa |
| 2 | aiuto da 20 s | freccia a bordo schermo / mano fantasma |
| 3 | aiuto da 40 s | camera sul bersaglio (≤1,5 s, input attivo) |

## 4. Tabella per passo (16 passi)
Legenda testi: `[chiave] «testo esatto»`; `step:<id>` = biglietto di Oma Tilde. Tempi di riferimento = golden path (eff 1,0). Criteri per passo da §G: GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 e PT-01…06 su tutti i passi; GATE-04 su ogni passo con escalation.

| # | Passo | Meccanica | Suggerimento atteso (ID + testo) | t rif. | maxSec | Escalation prevista | t inizio | t fine | Aiuto max (0-3) | Blocco >10 s (sì/no, s) | Screenshot | Esito | Criteri §A/§E (§G) | Criteri PT |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `coldOpen` | nessuna (scena) | [onb.cold.letterAda] «Kaia, tesoro, / se leggi, la serranda l'hai alzata tu. Bene. / Sunn Depot ora è tuo: pacchi, debiti e quel brontolone di OLDO. / Sul bancone c'è un pacco. Non è per i clienti. Lascialo lì, per ora. / Lui saprà aspettare. Tu no, lo so: comincia dagli Arrivi. / Ti voglio bene. / Oma Tilde» + [onb.cold.letterOmni] «GrayBox Inc. · Gentile sig.ra Rio, avete 1 anno per rendere redditizio il deposito. Poi lo acquisiremo. Cordialmente, M. Vane» | 0:00 | 15 | nessuna escalation | | | | | | | ONB-01; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 | PT-01…PT-06 |
| 2 | `move` | muoversi col joystick | [onb.move.card] «Trascina per muovere Kaia» + [step:move] «Kaia, sgranchisci le gambe. Il deposito si gira a piedi.» | 0:15 | 30 | 10s: ripete onb.move.card, joystick fantasma pulsa / 20s: mano fantasma che trascina / 40s: mano fantasma in loop + vibrazione leggera | | | | | | | ONB-02, GATE-01; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 3 | `pickUp` | raccogliere pacchi (stare sulla zona) | [step:pickup] «È arrivato il furgone! Passa agli Arrivi e prendi i pacchi.» + [onb.pick.max] «Pila piena! Portala allo scaffale» | 0:30 | 45 | 10s: cerchio Arrivi pulsa più forte / 20s: freccia a bordo schermo verso gli Arrivi / 40s: camera si sposta 1,5 s sugli Arrivi e torna | | | | | | | ONB-03; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 4 | `shelve` | depositare sullo scaffale | [step:shelve] «Ogni pacco al suo scaffale. L'ordine è mezzo mestiere.» + [onb.deposit.hint] «Stai sullo scaffale: i pacchi si sistemano da soli» | 0:50 | 60 | 10s: scaffale lampeggia / 20s: freccia verso lo scaffale / 40s: camera sullo scaffale + ripete onb.deposit.hint | | | | | | | ONB-04; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 5 | `serve` | servire al Bancone (pacco dallo scaffale al cliente) | [step:serve] «C'è Auntie Lulu al Bancone. Non farla aspettare, eh.» + [onb.cust.first] «Porta un pacco al Bancone» | 1:10 | 90 | 10s: Auntie Lulu saluta (bark) e la stella pulsa / 20s: freccia sul Bancone; se Kaia ha 0 pacchi la freccia va prima allo scaffale / 40s: camera sul Bancone + ripete onb.cust.first | | | | | | | ONB-05, CLI-01, CLI-05; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 6 | `coins` | raccogliere monete | [step:coins] «Raccogli le monete. Una alla volta si fa un anno.» + [onb.coins.walk] «Cammina sulle monete» | 1:15 | 95 | 10s: monete brillano / 20s: freccia sulle monete / 40s: le monete saltellano verso Kaia (raggio +0,5 m) | | | | | | | ONB-06; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 7 | `zainoPad` | comprare un pad (stare sopra finché si riempie) | [step:upgrade_zaino] «Sali sul pad ZAINO: più pacchi, meno giri. Fidati.» + [onb.zaino.hint] «Zaino più grande, più pacchi per giro» + [onb.zaino.notEnough] «Per lo Zaino servono 40 LN. Ancora qualche cliente!» | 1:23 | 120 | 10s: pad pulsa / 20s: freccia sul pad / 40s: camera sul pad + barra 'LN/40' | | | | | | | ONB-07, GATE-06, ECO-10; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 8 | `combo` | combo: servire clienti di fila (feedback '+N' sopra Kaia) | [onb.combo] «Servi di fila, senza pause: ogni cliente rende di più!» | 1:40 | 125 | 10s: il '+N' sopra Kaia diventa più grande per un istante / 20s: freccia sul Bancone quando c'è coda / 40s: ripete onb.combo; dopo 40 s il gate si considera passato (combo non bloccante) | | | | | | | ONB-08, ONB-15, CLI-02 (anziano da 1:40), GATE-05; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 9 | `nonnoIntro` | conoscere OLDO (solo guardare) | [onb.nonno.intro] «Quel brontolone che dorme è OLDO. L'ha fatto Oma.» + [onb.nonno.what] «OLDO porta i pacchi dagli Arrivi agli scaffali.» | 2:07 | 132 | nessuna escalation | | | | | | | ONB-09a; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 | PT-01…PT-06 |
| 10 | `hireNonno` | assumere un robot (pad da 50 LN) | [onb.nonno.cost] «Stai sul pad: con 50 LN OLDO si sveglia.» + [step:unlock_nonno] «OLDO brontola, ma lavora. Sveglialo dal pad, digli ciao.» + [onb.nonno.notEnough] «Mancano LN: servi qualche cliente al Bancone.» + [onb.nonno.ready] «50 LN in tasca! Torna dal pad di OLDO.» + [onb.nonno.reminder] «OLDO ti aspetta sul pad vicino agli Arrivi.» | 2:10 | 160 | 10s: onb.nonno.reminder + pad rimbalza / 20s: freccia + camera pulsa sul pad / 40s: camera 1,5 s sul pad + ripete onb.nonno.what | | | | | | | ONB-09, ONB-09b, ONB-09c, GATE-06; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 11 | `nonnoConfirm` | vedere OLDO lavorare | [onb.nonno.hired] «OLDO: «Sveglio. Non per scelta, ma sveglio.»» + [onb.nonno.working] «Gli scaffali li riempie lui. Tu pensa ai clienti.» | 2:11 | 150 | nessuna escalation | | | | | | | ONB-09d; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 | PT-01…PT-06 |
| 12 | `padNuovi` | scegliere tra più pad (Scarpe, Insegna) | [onb.pads.new] «Pad nuovi! Scarpe per correre, Insegna per clienti più ricchi.» | 2:50 | 230 | 10s: i due pad pulsano / 20s: freccia sul più economico pagabile / 40s: camera sui due pad | | | | | | | GATE-07, ECO-10, CLI-02 (frettoloso da 2:30); GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 13 | `cassaPass` | Cassa: incasso passandoci sopra | [onb.cassa.tip] «Sulla Cassa c'è una mancia di Oma Tilde: 30 LN. Prendila.» | 3:55 | 255 | 10s: Cassa luccica / 20s: freccia sulla Cassa / 40s: camera sulla Cassa | | | | | | | ONB-12; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 14 | `debtCard` | debito: versare alla Cassaforte (spiegato da Oma Tilde) | [onb.debt.adaExplain] «La rata si versa qui, alla Cassaforte. Entro il Giorno 24, Kaia.» + [onb.viktor.call1] «Sulla Cassaforte, una busta GrayBox: prima rata entro il Giorno 24.» + [onb.viktor.call2] ««Non ci deluda, sig.ra Rio. Cordialmente, M. Vane»» + [onb.debt.pedaggioHint] «Salti la rata? Quelli si prendono il 15% di ogni moneta. Occhio.» | 4:00 | 280 | 10s: Cassaforte pulsa / 20s: freccia sulla Cassaforte / 40s: camera sulla Cassaforte + Oma Tilde ripete onb.debt.adaExplain | | | | | | | ONB-13, ONB-13b, ECO-06, GATE-06; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, GATE-04 | PT-01…PT-06 |
| 15 | `wallPad` | obiettivo successivo | [onb.wall.goal] «Prossimo obiettivo: abbatti il muro» | 4:30 | 300 | nessuna escalation | | | | | | | ONB-14; GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 | PT-01…PT-06 |
| 16 | `freePlay` | gioco libero | [fine] «Brava, Kaia. Il resto lo scopri da te. Io l'ho sempre saputo.» | 5:00 | 0 (terminale) | nessuna escalation | | | | | | | fine run (maxSec 0 = terminale); GATE-02, GATE-03, ONB-10, ONB-11, ONB-16 | PT-01…PT-06, PT-07 |

## 5. Riepilogo della run
| Voce | QA-Playwright | Tester umano 1 (Lorenzo) |
|---|---|---|
| Passi completati / 16 | | |
| Durata totale onboarding (m:ss) | | |
| Blocchi >10 s (n., passi) | | |
| Aiuti livello 3 scattati (n.) | | |
| Passi oltre `maxSec` | | |
| Testi mancanti / chiavi grezze / >70 car. (ONB-16) | | |
| Criteri §A/§E falliti (ID) | | |
| ONB-10/11: telefono, barra debito o contatore combo visti nell'HUD (sì/no) | | |
| ONB-13b: schermata fine giornata con versato/8.400 e Giorno/24 (sì/no, screenshot) | | |
| PT-01…PT-07 (ok/ko ciascuno) | | |
| Esito run | ☐ PASSA ☐ NON PASSA | ☐ PASSA ☐ NON PASSA |

Spazio note Lorenzo (tester umano 1, dopo il VIA): momenti in cui non sapeva cosa fare, cosa fa OLDO, come si compra un miglioramento, cosa succede se non si paga la rata.

## 6. Regola di superamento della run (§F, PT-01…PT-07, tutti obbligatori)
| ID | Criterio | Come si misura | QA | Lorenzo |
|---|---|---|---|---|
| PT-01 | Mai più di 10 s senza sapere cosa fare | 0 blocchi >10 s nella scheda | | |
| PT-02 | Nessun intervento esterno (nessun blocco grave >60 s) | colonna blocco | | |
| PT-03 | Aiuto di livello 3 (40 s) al massimo 1 volta in tutto l'onboarding | colonna "aiuto max" | | |
| PT-04 | Ogni passo entro il suo `maxSec` (`freePlay` = 0, terminale: escluso) | t fine ≤ maxSec | | |
| PT-05 | Tutti i 16 gate nell'ordine di `onboarding.json`, ognuno loggato una volta | log dei gate | | |
| PT-06 | Nessuna chiave grezza `onb.*` / `step:*` a schermo e nessun testo >70 caratteri (ONB-16) | screenshot | | |
| PT-07 | Comprensione: almeno 3 domande finali su 4 corrette, sempre "Cosa fa OLDO?" (run QA-Playwright: OLDO assunto entro 2:10 +30 s senza aiuto di livello 3) | domande / log | | |

La run PASSA solo se PT-01…PT-07 sono tutti verdi. Criteri a livello di sessione (fuori scheda, §G): CLI-03, CLI-04, CLI-06, CLI-07, ECO-01…05, ECO-07…09, ECO-11, DAT-01…03. Ogni passo con blocco >10 s → ticket con screenshot e timestamp.
