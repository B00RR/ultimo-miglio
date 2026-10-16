# 12 - CAPITOLO 1: criteri di accettazione QA (Game Designer, 16/10/2026)
Fonti: `data/ch1/playthrough_expected.json` (rigenerato il 16/10), `onboarding.json`, `customers.json`, `economy.json` (`rewards`), `upgrades.json`, `debt.json`, 01_CORE_LOOP v3.3, 04_UI §4.
Formato Given/When/Then; ogni criterio è misurabile dal bot di playtest (`botSteps`) o dai log. Tempi in m:ss di gioco attivo, eff = efficienza del giocatore (1,0 = golden path). Tolleranze già incluse.

## A. Onboarding
| ID | Given | When | Then (tolleranza) |
|---|---|---|---|
| ONB-01 | nuova partita | parte la scena | cold open di 15 s ±1, HUD nascosto, nessun tap salta la scena |
| ONB-02 | cold open finito | il giocatore non tocca nulla per 5 s | compaiono mano fantasma e card `onb.move.card` entro 5,5 s |
| ONB-03 | bot sul golden path | raggiunge gli Arrivi | primo pacco raccolto a 0:30 ±10 s; la pila mostra "MAX" a 5 pacchi |
| ONB-04 | pila piena | deposita sullo Scaffale Terracotta | scaffale ≥ 5 pacchi a 0:50 ±10 s |
| ONB-05 | deposito fatto | Mamma Rosa arriva al Bancone | primo cliente servito a 1:10 ±15 s; è sempre Mamma Rosa (VIP, valore ×1,3) |
| ONB-06 | primo servizio | Nina cammina sulle monete | LN accreditate nella pillola entro 0,5 s dal contatto |
| ONB-07 | portafoglio ≥ 40 LN | Nina sta 3 s sul pad ZAINO LV 1 | Zaino 1 comprato a 1:23 ±20 s; il flusso non si mette mai in pausa |
| ONB-08 | t ≥ 1:40 | due clienti serviti a distanza ≤ 5 s | sopra Nina compare "+1" poi "+2" (combo x1,1 → x1,2), ognuno sparisce in 1,5 s ±0,2; nessun contatore combo fisso nell'HUD |
| ONB-09 | t ≥ 2:10 e portafoglio ≥ 50 LN | Nina sul pad ASSUMI NONNO | NONNO assunto a 2:10 ±5 s; restock 6 pacchi/min ±0,5 misurato su 60 s |
| ONB-09a | t ≥ 2:07 | parte il passo `nonnoIntro` | la camera inquadra pad e NONNO per 2,5 s ±0,3, il joystick resta attivo, a schermo `onb.nonno.what` (dice che NONNO porta i pacchi al posto tuo) |
| ONB-09b | Nina sul pad di NONNO con portafoglio < 50 LN | — | il pad mostra "X/50 LN" coerente col portafoglio (±1 LN), compare `onb.nonno.notEnough` e la freccia punta al Bancone; arrivati a 50 LN la freccia torna al pad entro 1 s |
| ONB-09c | pad di NONNO attivo | il giocatore non ci va per 20 s ±1 | promemoria `onb.nonno.reminder` + freccia + camera che pulsa; si ripete ogni 20 s fino all'assunzione |
| ONB-09d | NONNO assunto | — | camera su NONNO per 3 s ±0,3; NONNO consegna il primo pacco allo scaffale entro 12 s; badge "6 pacchi/min" visibile; a schermo `onb.nonno.hired` e `onb.nonno.working`, mai `onb.nonno.bark` nello stesso momento (il bark parte solo a conferma finita) |
| ONB-10 | cap 1, tutta la sessione | — | **nessun telefono**: nessuna icona, notifica o app (Messaggi, Ordini, Flotta); 0 occorrenze di `onb.civetta.*`, `onb.orders.special`, `onb.flotta.hint` (rinviati al cap 2) |
| ONB-11 | cap 1, tutta la sessione | — | **nessuna barra del debito** né contatore combo persistente nell'HUD; l'HUD mostra solo la pillola valute |
| ONB-12 | primo passaggio sulla Cassa (3:55 ±10 s) | Nina ci passa sopra | +30 LN una sola volta; al secondo passaggio 0 LN extra |
| ONB-13 | t ≥ 4:00 | Nina sulla Cassaforte | fumetto di Nonna Ada `onb.debt.adaExplain` (Rata 1 · 8.400 LN · scade Giorno 24) e lettera OmniFreight (`onb.viktor.call1` → `onb.viktor.call2`); primo versamento = 60% del portafoglio ±1 LN; nessuna barra appare dopo il versamento |
| ONB-14 | primo versamento + 30 s | — | pad APRI MURO mostrato come obiettivo; richiesta permesso notifiche solo su Android 13+ |
| ONB-13b | fine del primo ciclo di gioco dopo il primo versamento | compare la schermata di fine giornata | mostra il progresso del debito: versato / 8.400 LN (±1 LN rispetto ai log) e Giorno attuale / Giorno 24 |
| ONB-15 | qualsiasi passo dopo l'8 | il giocatore tocca "Salta tutorial" | si passa al gioco libero con tutti gli sblocchi dei passi saltati; prima del passo 8 il tasto non c'è |
| ONB-16 | ogni passo | testo mostrato | una sola riga, ≤ 70 caratteri, chiave presente in `onboarding_testi.json` (PR #62) (nessuna chiave grezza `onb.*` a schermo) |

## B. Clienti
| ID | Given | When | Then |
|---|---|---|---|
| CLI-01 | t < 1:40 | arrivano clienti | solo Mamma Rosa (scriptata) e "normale" |
| CLI-02 | t ≥ 1:40 / ≥ 2:30 | arrivano clienti | compaiono "anziano" / "frettoloso"; prima di quel tempo 0 occorrenze nei log |
| CLI-03 | tutti i tipi sbloccati, 200 clienti | si contano i tipi | normale 60% ±7, frettoloso 15% ±5, anziano 15% ±5, VIP 10% ±4 |
| CLI-04 | cliente in coda non servito | scade la pazienza (normale 20 s, frettoloso 12, anziano 35, VIP 25; ±0,5 s) | se ne va: 0 LN, combo invariata |
| CLI-05 | cliente servito | paga | valore = 10 LN × moltiplicatore tipo (0,95 / 1,2 / 0,8 / 1,3) × combo × bonus Insegna, ±1 LN |
| CLI-06 | ogni tipo | entra in scena | indizio visivo leggibile a 1 m di distanza sullo schermo da 6" (borsa, casco+fulmine, bastone+tazzina, stella gialla) |
| CLI-07 | coda | arrivano più di 6 clienti | al massimo 6 visibili |

## C. Economia e progressione (bot golden path, nessun offline)
| ID | Given | When | Then (tolleranza) |
|---|---|---|---|
| ECO-01 | eff 1,0 | il bot segue `botSteps` | ognuno dei 15 acquisti di `purchases[]` avviene al `tSec` ± `toleranceSec` (max(15 s, 15%)) |
| ECO-02 | eff 1,0 | — | LN cumulate a 1:23 = 41 ±10%; a 2:10 = 211 ±10%; a 4:00 = 771 ±10% |
| ECO-03 | eff 1,0 | rata 1 saldata | a 34:00 ±120 s, entro il Giorno 24 del calendario |
| ECO-04 | eff 1,2 / 0,85 | rata 1 saldata | 32:00 ±120 s / 36:00 ±120 s |
| ECO-05 | eff 0,7 | rata 1 saldata | entro 40:00 (finestra) senza usare la Proroga |
| ECO-06 | qualsiasi eff | prima della spiegazione del debito (4:00) | il 100% delle LN resta nel portafoglio; dopo, 40% portafoglio / 60% debito ±1% |
| ECO-07 | 100 / 250 / 450 pacchi consegnati | — | +60 / +120 / +200 LN una sola volta, coriandoli e riga di Mamma Rosa; eff 1,0: 6:03 / 12:25 / 20:11 ±15% |
| ECO-08 | versato 25% / 50% / 75% della rata | il versamento supera la soglia | timbro / +1 Reputazione / arredo `decor_foto_nonna_ada`; nessuna valuta assegnata; entro 1 s: timbro sulla Cassaforte, riga di Ada `onb.debt.milestone1/2/3` (una sola volta per tappa) e `sfx_debt_stamp` |
| ECO-08b | tappa N raggiunta nel ciclo | compare la schermata di fine giornata | mostra "tappa N/3" (1/3, 2/3, 3/3) coerente coi log; mai una barra o un contatore delle tappe nell'HUD durante il gioco |
| SFX-01 | passo `combo` | 2° cliente di fila ("+2", moltiplicatore interno x1,2) | suona `sfx_combo_plus` insieme al "+2" (±0,1 s) e scatta il gate; `sfx_combo_x1_5` non suona prima di "+5" |
| SFX-02 | tutto il cap 1 | — | nessun sfx di telefono (squillo, notifica, vibrazione) in nessun passo |
| ECO-09 | t ≥ 8:00, Scarpe 2 o NONNO 2 pagabili | — | freccia sul più economico dei due; sparisce all'acquisto o dopo 20 s |
| ECO-10 | costi | si apre ogni pad | prezzi = `upgrades.json` (Zaino 5/6/7 = 560/960/1.700 LN) |
| ECO-11 | cap 1 | — | nessun sistema gestionale di 10 attivo; tranche 8.400 LN invariata |

## D. Regressione dati
- DAT-01: `python3 -m json.tool` passa su tutti i `data/ch1/*.json`; le due copie (`data/ch1/` e `unity/Assets/_Game/Data/ch1/`) sono identiche byte per byte (`cmp`).
- DAT-02: `python3 qa/playthrough_ch1.py` rigenera `playthrough_expected.json` senza differenze rispetto al file versionato.
- DAT-03: `python3 qa/econ_sim_v3.py --eff X --sess none` per X = 0,7 / 0,85 / 1,0 / 1,2: cap 1 in stato "ok" (valori del 16/10: 38,9 / 35,7 / 33,5 / 31,3 min).

## Definition of Done del cap. 1 (design)
Tutti i criteri A-D verdi su 3 run consecutivi del bot; nessun blocco del flusso > 5 s senza suggerimento; nessun testo segnaposto a schermo.

## E. Onboarding graduale: criteri dei gate (rev. 16/10)
| ID | Given | When | Then |
|---|---|---|---|
| GATE-01 | nuova partita | parte il passo `move` | sono visibili solo Nina e il joystick; tutti gli elementi di `lockedAtStart` (`onboarding.json`) sono nascosti |
| GATE-02 | qualsiasi passo N | il gate di N non è avvenuto | nessun elemento di `reveals` del passo N+1 è visibile o interattivo (0 occorrenze nei log) |
| GATE-03 | gate di N avvenuto | — | il passo N+1 parte entro 1 s e rivela solo i suoi elementi |
| GATE-04 | giocatore fermo su un passo | 10 / 20 / 40 s ±1 | parte il livello di aiuto corrispondente (`hintEscalation`), con evento di log |
| GATE-05 | passo `combo` | 40 s senza combo x1,2 | il passo avanza comunque (gate morbido); tutti gli altri gate non avanzano mai a tempo |
| GATE-06 | bot golden path | tutti i gate | tempi invariati: Zaino 1 1:23 ±20 s, NONNO 2:10 ±5 s, primo versamento 4:00 ±10 s, rata 34:00 ±120 s |
| GATE-07 | `padNuovi` non ancora avviato | — | pad Scarpe e Insegna invisibili |

## F. Protocollo playtest dal vivo (giocatore nuovo)
**Chi:** 5 tester per giro (bastano a trovare circa l'80% dei problemi di usabilità), mai visto il gioco né i documenti, almeno 2 non giocatori abituali di gestionali. Dispositivo: telefono Android medio, verticale, audio acceso.
**Regole per il facilitatore:**
1. Unica frase iniziale: "È un gioco nuovo. Gioca come faresti a casa e dì ad alta voce quello che pensi e quello che provi a fare." Nessun'altra spiegazione.
2. Non rispondere alle domande sul gioco ("Cosa faresti tu?"). Intervieni solo se il tester è bloccato da più di 60 s, annotandolo come **blocco**.
3. Registra schermo + voce (think-aloud); se il tester tace per più di 15 s, chiedi "A cosa stai pensando?".
4. Ferma la sessione a fine onboarding (passo `freePlay`) o dopo 10 minuti.

**Cosa si misura per ogni passo** (dai log dei gate + video): durata del passo (dal reveal al gate); livello massimo di aiuto scattato (nessuno / 10 / 20 / 40 s); **punti di confusione** = momenti di più di 10 s senza un'azione utile o con frasi tipo "non so cosa fare" / "e adesso?"; verbalizzazione della meccanica (sa dire cosa ha appena imparato?).
**Domande finali (2 minuti):** "Cosa fa NONNO?", "Come si compra un miglioramento?", "Cosa succede se non paghi la rata?", "C'è stato un momento in cui non sapevi cosa fare?". Risposta corretta = idea giusta con parole proprie.

**Criteri di superamento della run (PT-01…PT-07, tutti obbligatori):**
| ID | Criterio | Come si misura |
|---|---|---|
| PT-01 | Mai più di 10 s senza sapere cosa fare | 0 blocchi >10 s nella scheda |
| PT-02 | Nessun intervento esterno (nessun blocco grave >60 s) | colonna blocco |
| PT-03 | Aiuto di livello 3 (40 s) al massimo 1 volta in tutto l'onboarding | colonna "aiuto max" |
| PT-04 | Ogni passo entro il suo `maxSec` (`freePlay` = 0, terminale: escluso) | t fine ≤ maxSec |
| PT-05 | Tutti i 16 gate nell'ordine di `onboarding.json`, ognuno loggato una volta | log dei gate |
| PT-06 | Nessuna chiave grezza `onb.*` / `step:*` a schermo e nessun testo >70 caratteri (ONB-16) | screenshot |
| PT-07 | Comprensione: almeno 3 domande finali su 4 corrette, sempre "Cosa fa NONNO?" (per la run QA-Playwright: NONNO assunto entro 2:10 +30 s senza aiuto di livello 3) | domande / log |

I criteri dei gate restano GATE-01…GATE-07 in §E: §E = comportamento del gioco, §F = superamento della run.

**Criterio di superamento del giro:** almeno 4 tester su 5 passano; per ogni passo dove 2 o più tester superano i 10 s di confusione si apre un ticket con video e minuto. Template di report: tabella tester × passo (durata, aiuto massimo, confusione sì/no, citazione).

## G. Mappatura ufficiale passo → criteri (rev. 16/10, HUD minimo: 16 passi)
Valgono per **tutti** i passi: GATE-02, GATE-03, ONB-10, ONB-11, ONB-16, PT-01…PT-06. GATE-04 vale per ogni passo con escalation (tutti tranne `coldOpen`, `nonnoIntro`, `nonnoConfirm`, `wallPad`, `freePlay`).
| # | Passo | Criteri specifici |
|---|---|---|
| 1 | `coldOpen` | ONB-01 |
| 2 | `move` | ONB-02, GATE-01 |
| 3 | `pickUp` | ONB-03 |
| 4 | `shelve` | ONB-04 |
| 5 | `serve` | ONB-05, CLI-01, CLI-05 |
| 6 | `coins` | ONB-06 |
| 7 | `zainoPad` | ONB-07, GATE-06, ECO-10 |
| 8 | `combo` | SFX-01, ONB-08, ONB-15, CLI-02 (anziano da 1:40), GATE-05 |
| 9 | `nonnoIntro` | ONB-09a |
| 10 | `hireNonno` | ONB-09, ONB-09b, ONB-09c, GATE-06 |
| 11 | `nonnoConfirm` | ONB-09d |
| 12 | `padNuovi` | GATE-07, ECO-10, CLI-02 (frettoloso da 2:30) |
| 13 | `cassaPass` | ONB-12 |
| 14 | `debtCard` | ONB-13, ONB-13b, ECO-06, GATE-06 |
| 15 | `wallPad` | ONB-14 |
| 16 | `freePlay` | PT-07, fine run (maxSec 0 = terminale) |
Criteri a livello di sessione (non legati a un passo): ECO-08b, SFX-02, CLI-03, CLI-04, CLI-06, CLI-07, ECO-01…ECO-05, ECO-07…ECO-09, ECO-11, DAT-01…03.
