# Onboarding cap. 1: specifica (Game Designer, 01 v3.3)
Fonti: 04_UI §4, `playthrough_expected.json` (golden path eff 1,0), `onboarding.json` (passi, trigger e chiavi di testo segnaposto `onb.*`, che il Narrative sostituirà con i testi definitivi).
Principi: si impara camminando; una riga per passo; nessun pulsante, a parte il tap sul telefono. Il tempo di gioco scorre durante tutto l'onboarding (calendario: a 4:00 è il Giorno 3/365).

| # | Tempo (toll.) | Passo | Trigger | Completamento | Note |
|---|---|---|---|---|---|
| 1 | 0:00 (15 s) | Cold open: serranda, lettere di Nonna Ada e di OmniFreight | avvio scena | fine camera | HUD nascosto, non saltabile |
| 2 | 0:15 ±5 | Muoviti (mano fantasma + card) | fine cold open | 1,5 m percorsi | |
| 3 | 0:30 ±10 | Raccogli: il furgone scarica 5 pacchi agli Arrivi | passo 2 | pila = 5 → "MAX" | |
| 4 | 0:50 ±10 | Deposita sullo scaffale Terracotta | pila piena | scaffale ≥5 | |
| 5 | 1:10 ±15 | Primo cliente: Mamma Rosa (VIP), freccia a bordo schermo | deposito fatto | 1 cliente servito | compare la pillola valute |
| 6 | 1:15 ±15 | "Cammina sulle monete" | primo servizio | monete raccolte | |
| 7 | 1:23 ±20 | Pad **ZAINO LV 1 · 40 LN** nel Magazzino (opzionale) | portafoglio ≥40 LN | Zaino 1 | nessuna pausa del flusso |
| 8 | 1:40 ±15 | Combo: 2 clienti normali ravvicinati | t ≥ 100 s | combo ≥ x1,2 | passo +0,1 → x1,1 → x1,2 |
| 9 | **2:10 ±5** | Pad **ASSUMI NONNO · 50 LN** vicino agli Arrivi | t ≥ 130 s | NONNO attivo (restock 6/min) | si sblocca Flotta (sola lettura) |
| 10 | 2:45 ±10 | Telefono: messaggio della Civetta (teaser) | NONNO + 35 s | messaggio letto | si sblocca Messaggi |
| 11 | 3:30 ±10 | App Ordini: ordine speciale di Mamma Rosa, "Mostra la strada" | t ≥ 210 s | tap | suggerimento Flotta |
| 12 | 3:55 ±10 | Passaggio sulla Cassa | verso la Cassaforte | passaggio | +30 LN una tantum, "mancia di Nonna Ada" (16/10) |
| 13 | **4:00 ±10** | Chiamata di Viktor + scheda debito **Giorno 3/365 · Rata 1 · 8.400 LN · scade Giorno 24** + anteprima del Pedaggio | t ≥ 240 s | primo versamento (~176 LN, 60% del portafoglio) | finestra di 40 min attivi |
| 14 | 4:30 ±10 | Pad APRI MURO come prossimo obiettivo; permesso notifiche (Android 13+) | versamento + 30 s | mostrato | |
| 15 | 5:00 | Gioco libero | | | Bottega da 5:00; totem solo dal cap 4 |

**Tipi di clienti durante l'onboarding** (`customers.json`): fino a 1:40 solo Mamma Rosa (scriptata) e clienti normali; l'anziano compare da 1:40; il frettoloso da 2:30, quando NONNO lavora già.
**Fallback:** dopo 5 s fermi in un passo compaiono la mano fantasma e la freccia. "Salta tutorial" è disponibile dopo il passo 8.
**Economia:** fino a 4:00 tutte le LN restano nel portafoglio (nessun debito visibile); dal primo versamento la regola è 40% upgrade / 60% debito. Valori al golden path: 41 LN a 1:23, 211 LN cumulate a 2:10, 771 a 4:00. La rata viene saldata a ~34:00 (32:00-36:00 per eff 1,2-0,85; rev. 16/10, vedi sotto).
**Da allineare in 04_UI §4 (non modificato qui):** la combo del passo 6 è "x1,1 → x1,2" (non "x1,2 → x1,4"); il pad Zaino a 1:23 e la comparsa dei tipi di cliente vanno aggiunti all'elenco.

## Dettaglio per passo: cosa vede il giocatore, obiettivo, suggerimento (rev. 16/10)
| # | Cosa vede | Obiettivo | Suggerimento (chiave testo) | Fallback dopo 5 s fermo |
|---|---|---|---|---|
| 1 | Serranda che si alza, due lettere | nessuno (scena) | `onb.cold.letterAda`, `onb.cold.letterOmni` | — |
| 2 | Nina al centro, joystick fantasma in basso a sinistra | muoversi di 1,5 m | `onb.move.card` + biglietto Ada `move` | mano fantasma che trascina |
| 3 | Furgone che scarica 5 pacchi, cerchio pulsante sugli Arrivi | pila di 5 (badge "MAX") | biglietto `pickup`, poi `onb.pick.max` | freccia a bordo schermo |
| 4 | Scaffale Terracotta evidenziato | 5 pacchi sullo scaffale | biglietto `shelve`, `onb.deposit.hint` | freccia sullo scaffale |
| 5 | Mamma Rosa (stella gialla) al Bancone | servire 1 cliente | biglietto `serve`, `onb.cust.first` | freccia sul Bancone |
| 6 | Monete a terra, pillola LN in alto | raccogliere le monete | biglietto `coins`, `onb.coins.walk` | alone sulle monete |
| 7 | Pad ZAINO LV 1 · 40 LN illuminato in Magazzino | (facoltativo) Zaino 1 | biglietto `upgrade_zaino`, `onb.zaino.hint` | nessuno: non blocca |
| 8 | Due clienti in coda, numero combo sopra il Bancone | combo ≥ x1,2 | `onb.combo` | nessuno |
| 9 | Pad ASSUMI NONNO · 50 LN accanto agli Arrivi, NONNO che dorme | assumere NONNO | biglietto `unlock_nonno`, `onb.nonno.bark` | freccia sul pad |
| 10 | Telefono che vibra | leggere il messaggio | `onb.civetta.msg1/reply/msg2` | nuova vibrazione |
| 11 | Notifica app Ordini | tap su "Mostra la strada" | `onb.orders.special`, `onb.flotta.hint` | badge rosso fisso |
| 12 | Cassa sul percorso verso la Cassaforte | passarci sopra | moneta +30 LN | — |
| 13 | Chiamata di Viktor, scheda debito | primo versamento | `onb.viktor.call1/2`, `onb.debt.pedaggioHint` | freccia sulla Cassaforte |
| 14 | Pad APRI MURO tratteggiato | nessuno (obiettivo mostrato) | `onb.wall.goal` | — |
| 15 | HUD completo | gioco libero | biglietto `fine` | — |

I biglietti di Nonna Ada sono in `onboarding_testi.json` (7, in ordine: move, pickup, shelve, serve, coins, upgrade_zaino, unlock_nonno); le chiavi `onb.*` stanno nello stesso file (`lettere`, `suggerimenti`).

## Ricompense dopo l'onboarding (rev. 16/10, `economy.json` → `rewards`)
- Traguardi consegne: 100 / 250 / 450 pacchi = +60 / +120 / +200 LN (golden path: 6:03, 12:25, 20:11).
- Tappe del debito: 25% timbro visivo; 50% +1 Reputazione; 75% arredo "foto di Nonna Ada".
- Freccia "prossimo pad" da 8:00 sul più economico tra Scarpe 2 e NONNO 2, solo se pagabile.

## Verifica economica (16/10, `qa/playthrough_ch1.py`, `qa/econ_sim_v3.py`, `qa/eval_liveops_ch1.py`)
| eff | Zaino 1 | NONNO | Rata 1 saldata (run continua) | econ_sim_v3 cap 1 | Esito |
|---|---|---|---|---|---|
| 0,70 | 1:27 | 2:10 | 39:00 | 38,9 min | ok (prima della revisione: 41:00, fuori dalla finestra di 40 min) |
| 0,85 | ~1:25 | 2:10 | 36:00 | 35,7 min | ok |
| 1,00 | 1:23 | 2:10 | 34:00 | 33,5 min | ok (golden path) |
| 1,20 | <1:23 | 2:10 | 32:00 | 31,3 min | ok |
Al golden path il portafoglio vale 41 LN a 1:23 e 211 LN cumulati a 2:10: tutti i 15 acquisti del cap 1 (fino a Zaino 7 a 28:47) sono pagabili in tempo con la quota del 40%.
