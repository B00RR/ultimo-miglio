# 13 - CAPITOLO 1: eventi di log dell'onboarding (specifica di design, issue #91)
Game Designer, 16/10/2026. Base: onboarding a **16 passi** di PR #106 (HUD minimo). Se #106 non fosse unita, vedi §5.
Scopo: il QA legge dalla console del browser (build WebGL) ogni sblocco di passo e ogni livello di aiuto, per compilare la scheda di osservazione e i criteri §E/§F di `12_CAP1_CRITERI_QA.md` (GATE-02…05, PT-01…PT-05) senza strumenti extra.

## 1. Formato della riga
```
[ONB] <tipo> id=<idPasso> n=<indice> t=<tempo> [campi extra]
```
- Prefisso fisso `[ONB]`, poi un solo spazio; campi `chiave=valore` separati da uno spazio, senza spazi nei valori (usare `_`).
- `id` = `steps[].id` di `onboarding.json` (es. `hireNonno`), **non** etichette tipo "9b". `n` = indice 0-based del passo (0-15).
- `t` = tempo di gioco attivo dall'avvio della scena, in secondi, **1 decimale, punto decimale** (es. `t=131.2`). Non scorre in pausa/menu.
- Una riga per evento, emessa nello stesso frame in cui l'evento avviene. Livello di log: `Debug.Log` (info), sempre attivo anche in build release finché c'è l'onboarding.
- Regex di validazione (per il test EditMode): `^\[ONB\] (start|gate|hint|softgate|nolnp|skip|end) id=[A-Za-z]+ n=\d{1,2} t=\d+\.\d( [a-zA-Z]+=[^ ]+)*$`

## 2. Tipi di evento
| Tipo | Quando si emette | Campi extra | Esempio |
|---|---|---|---|
| `start` | il passo diventa attivo (reveal dei suoi elementi) | `reveals=<n elementi>` | `[ONB] start id=hireNonno n=9 t=130.0 reveals=1` |
| `gate` | l'evento di completamento del passo avviene (§3) | `event=<nome gate>` `dur=<s dal start, 1 dec.>` | `[ONB] gate id=hireNonno n=9 t=131.2 event=nonnoHired dur=1.2` |
| `hint` | scatta un livello di aiuto per inattività sul passo | `lvl=1\|2\|3` (10/20/40 s) `idle=<s>` `kind=<tipo aiuto>` | `[ONB] hint id=move n=1 t=36.0 lvl=2 idle=20.0 kind=manoFantasma` |
| `softgate` | solo `combo`: avanza dopo 40 s senza x1,2 | `idle=40.0` | `[ONB] softgate id=combo n=7 t=140.0 idle=40.0` |
| `nolnp` | il giocatore è sul pad del passo ma non ha LN sufficienti | `pad=<id>` `wallet=<LN>` `cost=<LN>` | `[ONB] nolnp id=hireNonno n=9 t=129.5 pad=assumiNonno wallet=32 cost=50` |
| `skip` | tocco su "Salta tutorial" (disponibile dopo `combo`) | `from=<idPasso>` | `[ONB] skip id=nonnoIntro n=8 t=120.4 from=nonnoIntro` |
| `end` | inizio di `freePlay` (o dopo skip) | `total=<s>` `hints=<n>` `lvl3=<n>` | `[ONB] end id=freePlay n=15 t=301.0 total=301.0 hints=2 lvl3=0` |

Regole:
- **Inattività** = nessun progresso verso il gate del passo (nessun movimento utile verso il bersaglio e nessun evento di gioco del passo). Il conteggio riparte da 0 a ogni progresso; ogni livello si emette **una volta** per periodo di inattività. Se l'escalation si ripete (es. promemoria NONNO ogni 20 s), si riemette `hint` con lo stesso `lvl`.
- `kind` usa i nomi di `hintEscalation`: `lvl=1` → `ripeti` (testo ripetuto/bersaglio pulsa), `lvl=2` → `freccia` o `manoFantasma`, `lvl=3` → `camera`.
- I passi senza escalation (`coldOpen`, `nonnoIntro`, `nonnoConfirm`, `wallPad`, `freePlay`) emettono solo `start` e `gate`.
- Ordine garantito per passo: `start` → (`hint`/`nolnp`)* → `gate`, poi `start` del passo successivo **entro 1 s** (GATE-03).

## 3. Mappa sui 16 passi
| n | id | evento `gate` (`event=`) | `hint` 10/20/40 s |
|---|---|---|---|
| 0 | `coldOpen` | `coldOpenEnd` | — |
| 1 | `move` | `playerMoved m≥1.5` | sì |
| 2 | `pickUp` | `parcelPicked ≥1` | sì |
| 3 | `shelve` | `shelfDeposit ≥1` | sì |
| 4 | `serve` | `customerServed ≥1` | sì |
| 5 | `coins` | `coinsCollected ≥1` | sì |
| 6 | `zainoPad` | `padPurchased zaino 1` | sì |
| 7 | `combo` | `comboReached ≥1.2 (o softgate a 40 s)` | sì |
| 8 | `nonnoIntro` | `cameraDone` | — |
| 9 | `hireNonno` | `nonnoHired` | sì |
| 10 | `nonnoConfirm` | `nonnoFirstRestock` | — |
| 11 | `padNuovi` | `padPurchasedAny scarpe|insegna` | sì |
| 12 | `cassaPass` | `cassaPassed` | sì |
| 13 | `debtCard` | `firstDebtDeposit` | sì |
| 14 | `wallPad` | `goalSeen 3 s` | — |
| 15 | `freePlay` | `— (terminale)` | — |

## 4. Come lo usa il QA
- Durata passo = `dur` del `gate`; aiuto massimo = `lvl` più alto tra gli `hint` del passo; "blocco >10 s" = almeno un `hint lvl=1`.
- PT-03: conta di `hint lvl=3` in tutto l'onboarding (anche nel campo `lvl3` di `end`) ≤ 1.
- PT-05: 16 `gate` (o 15 `gate` + `softgate` per `combo`) nell'ordine della tabella, ciascuno una volta.
- Filtro in console del browser: `[ONB]`.

## 5. Compatibilità
- Se l'onboarding fosse ancora a 18 passi (main prima di #106): si aggiungono `phone` (gate `messageRead`) e `ordersApp` (gate `ordersTapShowRoute`) e gli indici `n` scorrono; il formato non cambia.
- Il formato è stabile: nuovi campi solo in coda, mai rinominare quelli esistenti (il QA li parsa).

## 6. Fuori dallo scope di design (per il responsabile del codice: TutorialManager, PR #101)
- Emissione delle righe in `TutorialManager`/escalation aiuti, con il tempo di gioco attivo.
- Test EditMode sul formato (regex §1) e sull'ordine `start`→`gate` per tutti i passi.
- Allineamento di PR #101 (oggi a 18 passi) ai 16 passi di PR #106.
