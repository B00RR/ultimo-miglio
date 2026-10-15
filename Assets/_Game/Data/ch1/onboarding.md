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
| 12 | 3:55 ±10 | Passaggio sulla Cassa | verso la Cassaforte | passaggio | 0 LN in una partita continua |
| 13 | **4:00 ±10** | Chiamata di Viktor + scheda debito **Giorno 3/365 · Rata 1 · 8.400 LN · scade Giorno 24** + anteprima del Pedaggio | t ≥ 240 s | primo versamento (~176 LN, 60% del portafoglio) | finestra di 40 min attivi |
| 14 | 4:30 ±10 | Pad APRI MURO come prossimo obiettivo; permesso notifiche (Android 13+) | versamento + 30 s | mostrato | |
| 15 | 5:00 | Gioco libero | | | Bottega da 5:00; totem solo dal cap 4 |

**Tipi di clienti durante l'onboarding** (`customers.json`): fino a 1:40 solo Mamma Rosa (scriptata) e clienti normali; l'anziano compare da 1:40; il frettoloso da 2:30, quando NONNO lavora già.
**Fallback:** dopo 5 s fermi in un passo compaiono la mano fantasma e la freccia. "Salta tutorial" è disponibile dopo il passo 8.
**Economia:** fino a 4:00 tutte le LN restano nel portafoglio (nessun debito visibile); dal primo versamento la regola è 40% upgrade / 60% debito. Valori al golden path: 41 LN a 1:23, 211 LN cumulate a 2:10, 771 a 4:00. La rata viene saldata a ~35:00 (33:00-37:00 per eff 1,2-0,85).
**Da allineare in 04_UI §4 (non modificato qui):** la combo del passo 6 è "x1,1 → x1,2" (non "x1,2 → x1,4"); il pad Zaino a 1:23 e la comparsa dei tipi di cliente vanno aggiunti all'elenco.
