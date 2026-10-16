# Onboarding cap. 1: specifica graduale (Game Designer, rev. 16/10/2026)
Richiesta di Lorenzo (via DevOn): **una meccanica alla volta**; ogni passo si sblocca **solo** quando il giocatore ha dimostrato di aver capito il precedente, con un'azione osservabile (gate). Dati: `onboarding.json`. Testi definitivi: `onboarding_testi.json` (PR #62); qui solo le chiavi.

## Principi
- All'avvio è tutto nascosto: HUD (pillola valute, telefono, barra debito, combo), zone, pad, NONNO, clienti, app. Ogni passo rivela solo ciò che serve alla sua meccanica.
- Gate = evento verificabile nei log (`gate.event`). Nessun passo avanza a tempo, tranne la combo (gate morbido a 40 s, perché dipende dai clienti).
- Escalation degli aiuti se il giocatore è fermo sul passo: **10 s** ripete il testo e fa pulsare il bersaglio; **20 s** freccia a bordo schermo o mano fantasma; **40 s** camera breve sul bersaglio (≤1,5 s, joystick sempre attivo).
- Il tempo di gioco scorre sempre. "Salta tutorial" compare dopo il passo combo.

## Passi
| # | t atteso (toll.) | Max | Meccanica (una sola) | Si rivela | Gate (evento) | Testi | Aiuti 10/20/40 s |
|---|---|---|---|---|---|---|---|
| 0 `coldOpen` | 0:00 ±1 | 0:15 | nessuna (scena) | — | `coldOpenEnd` | `onb.cold.letterAda`, `onb.cold.letterOmni` | — |
| 1 `move` | 0:15 ±5 | 0:30 | muoversi col joystick | input.joystick | `playerMoved ≥1.5` | `onb.move.card`, `step:move` | ripete onb.move.card, joystick fantasma pulsa / mano fantasma che trascina / mano fantasma in loop + vibrazione leggera |
| 2 `pickUp` | 0:30 ±10 | 0:45 | raccogliere pacchi (stare sulla zona) | zona.arrivi | `parcelPicked ≥1` | `step:pickup`, `onb.pick.max` | cerchio Arrivi pulsa più forte / freccia a bordo schermo verso gli Arrivi / camera si sposta 1,5 s sugli Arrivi e torna |
| 3 `shelve` | 0:50 ±10 | 1:00 | depositare sullo scaffale | zona.scaffaleTerracotta | `shelfDeposit ≥1` | `step:shelve`, `onb.deposit.hint` | scaffale lampeggia / freccia verso lo scaffale / camera sullo scaffale + ripete onb.deposit.hint |
| 4 `serve` | 1:10 ±15 | 1:30 | servire al Bancone (pacco dallo scaffale al cliente) | zona.bancone, cliente.mammaRosa | `customerServed ≥1` | `step:serve`, `onb.cust.first` | Mamma Rosa saluta (bark) e la stella pulsa / freccia sul Bancone; se Nina ha 0 pacchi la freccia va prima allo scaffale / camera sul Bancone + ripete onb.cust.first |
| 5 `coins` | 1:15 ±15 | 1:35 | raccogliere monete | hud.pillolaValute, monete | `coinsCollected ≥1` | `step:coins`, `onb.coins.walk` | monete brillano / freccia sulle monete / le monete saltellano verso Nina (raggio +0,5 m) |
| 6 `zainoPad` | 1:23 ±20 | 2:00 | comprare un pad (stare sopra finché si riempie) | pad.zaino | `padPurchased (zaino 1)` | `step:upgrade_zaino`, `onb.zaino.hint` | pad pulsa / freccia sul pad / camera sul pad + barra 'LN/40' |
| 7 `combo` | 1:40 ±15 | 2:05 | combo: servire clienti di fila | clienti.normali, hud.combo | `comboReached ≥1.2` | `onb.combo` | numero combo pulsa / freccia sul Bancone quando c'è coda / ripete onb.combo; dopo 40 s il gate si considera passato (combo non bloccante) |
| 8 `nonnoIntro` | 2:07 ±5 | 2:12 | conoscere NONNO (solo guardare) | robot.nonno.dorme | `cameraDone` | `onb.nonno.intro`, `onb.nonno.what` | — |
| 9 `hireNonno` | 2:10 ±5 | 2:40 | assumere un robot (pad da 50 LN) | pad.assumiNonno | `nonnoHired` | `onb.nonno.cost`, `step:unlock_nonno` | onb.nonno.reminder + pad rimbalza / freccia + camera pulsa sul pad / camera 1,5 s sul pad + ripete onb.nonno.what |
| 10 `nonnoConfirm` | 2:11 ±5 | 2:30 | vedere NONNO lavorare | robot.nonno.attivo, app.flotta | `nonnoFirstRestock` | `onb.nonno.hired`, `onb.nonno.working` | — |
| 11 `phone` | 2:45 ±10 | 3:20 | telefono: leggere un messaggio | hud.telefono, app.messaggi | `messageRead` | `onb.civetta.msg1`, `onb.civetta.reply`, `onb.civetta.msg2` | telefono vibra di nuovo / badge rosso + freccia sul telefono / mano fantasma che tocca il telefono |
| 12 `padNuovi` | 2:50 ±10 | 3:50 | scegliere tra più pad (Scarpe, Insegna) | pad.scarpe, pad.insegna, pad.zaino.livelliSuccessivi | `padPurchasedAny` | `NEW:onb.pads.new` | i due pad pulsano / freccia sul più economico pagabile / camera sui due pad |
| 13 `ordersApp` | 3:30 ±10 | 4:00 | app Ordini | app.ordini | `ordersTapShowRoute` | `onb.orders.special`, `onb.flotta.hint` | badge rosso fisso / freccia sul telefono / mano fantasma sull'app |
| 14 `cassaPass` | 3:55 ±10 | 4:15 | Cassa: incasso passandoci sopra | zona.cassa | `cassaPassed` | `NEW:onb.cassa.tip` | Cassa luccica / freccia sulla Cassa / camera sulla Cassa |
| 15 `debtCard` | 4:00 ±10 | 4:40 | debito: versare alla Cassaforte | zona.cassaforte, hud.barraDebito | `firstDebtDeposit` | `onb.viktor.call1`, `onb.viktor.call2`, `onb.debt.pedaggioHint` | Cassaforte pulsa / freccia sulla Cassaforte / camera sulla Cassaforte + ripete onb.viktor.call2 |
| 16 `wallPad` | 4:30 ±10 | 5:00 | obiettivo successivo | pad.apriMuro | `goalSeen ≥3` | `onb.wall.goal` | — |
| 17 `freePlay` | 5:00 ±0 | — | gioco libero | pad.nonnoUpgrade, tutto il resto del cap 1 | `none` | `fine` | — |

Note ai passi:
- `shelve` compare quando la pila è piena (5) o quando Nina lascia gli Arrivi con almeno 1 pacco.
- `zainoPad` compare a 40 LN in tasca. È un gate vero (insegna a comprare un pad): senza, NONNO non si capisce.
- `nonnoIntro`/`hireNonno`/`nonnoConfirm`: camera su NONNO e pad (2,5 s), barra "LN/50" sul pad, con LN insufficienti `onb.nonno.notEnough` e freccia al Bancone, poi `onb.nonno.ready`; dopo l'assunzione camera su NONNO al lavoro (3 s), `onb.nonno.hired` + `onb.nonno.working`; da lì `onb.nonno.bark` solo come bark ricorrente.
- `padNuovi`: Scarpe e Insegna compaiono solo qui (prima era tutto visibile dopo NONNO); gate = un acquisto qualsiasi dei due.
- Clienti: Mamma Rosa sola fino al passo combo; poi normali, anziano da 1:40, frettoloso da 2:30.

## Effetto sui tempi del golden path
Rieseguito `qa/playthrough_ch1.py` con Scarpe/Insegna nascosti fino a 170 s: **nessuna differenza**. Zaino 1 1:23, NONNO 2:10, primo versamento 4:00, rata saldata a 34:00 (eff 1,2: 32:00; eff 0,85: 36:00). I gate coincidono con azioni che il golden path fa già; un giocatore lento ci mette di più, ma entro i `maxSec`.

## Chiavi di testo
Esistenti (PR #62): tutte le `onb.*` e i biglietti `step:<id>` citati sopra.

**Chiavi NUOVE richieste al Narrative** (max 70 caratteri):
- `onb.zaino.notEnough`: servono 40 LN per lo Zaino (continua a servire clienti);
- `onb.pads.new`: ci sono nuovi pad, scegline uno (Scarpe = più veloce, Insegna = clienti che pagano di più);
- `onb.cassa.tip`: la mancia di 30 LN di Nonna Ada al primo passaggio sulla Cassa.

## Cosa serve dagli altri ruoli
**UI/UX:** stati nascosto/rivelato con animazione di comparsa per ogni elemento di `lockedAtStart`; barra di riempimento LN sui pad ("32/50"); freccia a bordo schermo; mano fantasma; pulsazione dei bersagli; camera breve non bloccante (2,5-3 s); badge "6 pacchi/min"; tasto "Salta tutorial" dopo la combo; evento di log per ogni gate e ogni livello di aiuto.
**Narrative:** le 3 chiavi nuove sopra; conferma che i biglietti `step:*` restano 7 e nello stesso ordine.

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

