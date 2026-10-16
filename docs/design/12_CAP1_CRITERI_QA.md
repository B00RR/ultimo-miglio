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
| ONB-08 | t ≥ 1:40 | due clienti serviti a distanza ≤ 5 s | combo mostrata x1,1 poi x1,2 |
| ONB-09 | t ≥ 2:10 e portafoglio ≥ 50 LN | Nina sul pad ASSUMI NONNO | NONNO assunto a 2:10 ±5 s; restock 6 pacchi/min ±0,5 misurato su 60 s |
| ONB-09a | t ≥ 2:07 | parte il passo `nonnoIntro` | la camera inquadra pad e NONNO per 2,5 s ±0,3, il joystick resta attivo, a schermo `onb.nonno.what` (dice che NONNO porta i pacchi al posto tuo) |
| ONB-09b | Nina sul pad di NONNO con portafoglio < 50 LN | — | il pad mostra "X/50 LN" coerente col portafoglio (±1 LN), compare `onb.nonno.notEnough` e la freccia punta al Bancone; arrivati a 50 LN la freccia torna al pad entro 1 s |
| ONB-09c | pad di NONNO attivo | il giocatore non ci va per 20 s ±1 | promemoria `onb.nonno.reminder` + freccia + camera che pulsa; si ripete ogni 20 s fino all'assunzione |
| ONB-09d | NONNO assunto | — | camera su NONNO per 3 s ±0,3; NONNO consegna il primo pacco allo scaffale entro 12 s; badge "6 pacchi/min" visibile |
| ONB-10 | NONNO assunto | passano 35 s | il telefono vibra (Civetta) a 2:45 ±10 s |
| ONB-11 | t ≥ 3:30 | tap su notifica Ordini | si apre l'ordine speciale; "Mostra la strada" disegna la freccia |
| ONB-12 | primo passaggio sulla Cassa (3:55 ±10 s) | Nina ci passa sopra | +30 LN una sola volta; al secondo passaggio 0 LN extra |
| ONB-13 | t ≥ 4:00 | Nina sulla Cassaforte | chiamata di Viktor e scheda "Giorno 3/365 · Rata 1 · 8.400 LN · scade Giorno 24"; primo versamento = 60% del portafoglio ±1 LN |
| ONB-14 | primo versamento + 30 s | — | pad APRI MURO mostrato come obiettivo; richiesta permesso notifiche solo su Android 13+ |
| ONB-15 | qualsiasi passo dopo l'8 | il giocatore tocca "Salta tutorial" | si passa al gioco libero con tutti gli sblocchi dei passi saltati; prima del passo 8 il tasto non c'è |
| ONB-16 | ogni passo | testo mostrato | una sola riga, ≤ 70 caratteri, chiave presente in `onboarding_testi.json` (nessuna chiave grezza `onb.*` a schermo) |

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
| ECO-06 | qualsiasi eff | prima della scheda debito (4:00) | il 100% delle LN resta nel portafoglio; dopo, 40% portafoglio / 60% debito ±1% |
| ECO-07 | 100 / 250 / 450 pacchi consegnati | — | +60 / +120 / +200 LN una sola volta, coriandoli e riga di Mamma Rosa; eff 1,0: 6:03 / 12:25 / 20:11 ±15% |
| ECO-08 | versato 25% / 50% / 75% della rata | — | timbro / +1 Reputazione / arredo `decor_foto_nonna_ada`; nessuna valuta assegnata |
| ECO-09 | t ≥ 8:00, Scarpe 2 o NONNO 2 pagabili | — | freccia sul più economico dei due; sparisce all'acquisto o dopo 20 s |
| ECO-10 | costi | si apre ogni pad | prezzi = `upgrades.json` (Zaino 5/6/7 = 560/960/1.700 LN) |
| ECO-11 | cap 1 | — | nessun sistema gestionale di 10 attivo; tranche 8.400 LN invariata |

## D. Regressione dati
- DAT-01: `python3 -m json.tool` passa su tutti i `data/ch1/*.json`; le due copie (`data/ch1/` e `unity/Assets/_Game/Data/ch1/`) sono identiche byte per byte (`cmp`).
- DAT-02: `python3 qa/playthrough_ch1.py` rigenera `playthrough_expected.json` senza differenze rispetto al file versionato.
- DAT-03: `python3 qa/econ_sim_v3.py --eff X --sess none` per X = 0,7 / 0,85 / 1,0 / 1,2: cap 1 in stato "ok" (valori del 16/10: 38,9 / 35,7 / 33,5 / 31,3 min).

## Definition of Done del cap. 1 (design)
Tutti i criteri A-D verdi su 3 run consecutivi del bot; nessun blocco del flusso > 5 s senza suggerimento; nessun testo segnaposto a schermo.
