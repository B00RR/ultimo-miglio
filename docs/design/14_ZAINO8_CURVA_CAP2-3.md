# 14 - Zaino 8: costo e curva cap. 2-3 (Game Designer, 16/10/2026)
Proposta LiveOps (`liveops/REVISIONE_ECONOMIA_CAP1.md` §3): Zaino 8 = 3.000 LN invece di 5.120 (×1,76 come i livelli 4-7); ripiego se nel cap 3 si apre un buco: Zaino 9 = 5.600.

## Verifica
Sim: `qa/econ_sim_v3.py --sess 4x5` (versione breve, obiettivo 25-40 min per capitolo) e `long_check.py --mgr average` (storia lunga di 11_LONGEVITA, obiettivo ≤ ~135 min), con un registro degli acquisti per misurare il **buco massimo** (minuti senza nessun acquisto) nei cap 2 e 3.

**Versione breve** (min attivi cap 1/2/3 · buco max cap 2 / cap 3)
| eff | 5.120 (prima) | **Zaino 8 = 3.000** | 3.000 + Zaino 9 = 5.600 |
|---|---|---|---|
| 0,70 | 33,6/31,6/31,8 · 13,1/14,6 | 33,6/31,1/31,4 · **8,7**/14,6 | 33,6/31,3/30,7 · 11,3/7,2 |
| 0,85 | 30,9/29,3/30,1 · 12,2/13,8 | 30,9/29,3/30,1 · **8,0**/14,7 | 30,9/29,3/29,8 · 13,3/12,2 |
| 1,00 | 29,4/28,5/29,0 · 12,4/13,6 | 29,4/28,0/28,6 · **10,1**/13,2 | 29,4/28,1/28,0 · 12,0/10,4 |
| 1,20 | 27,6/27,3/28,2 · 11,1/12,6 | 27,6/26,9/27,7 · **10,2**/13,0 | 27,6/27,0/27,3 · 12,2/10,8 |
Zaino 8 si compra nel cap 2 a 15:08 (eff 1,0) invece di 20:09. Tutti i capitoli in stato "ok", durate dentro 25-40 min.

**Versione lunga** (buco max cap 2 / cap 3, capitolo più lungo, totale storia)
| eff | 5.120 (prima) | **Zaino 8 = 3.000** | 3.000 + Zaino 9 = 5.600 |
|---|---|---|---|
| 0,70 | 12,9 / 17,8 · max 139 · 27,1 h | **11,4** / 17,8 · 140 · 27,0 h | 12,8 / **26,7** · 140 · 27,0 h |
| 0,85 | 12,3 / 18,1 · 136 · 26,3 h | **10,6** / 18,1 · 136 · 26,3 h | 12,8 / **26,2** · 136 · 26,2 h |
| 1,00 | 12,3 / 17,1 · 133 · 25,8 h | **10,2** / 17,2 · 133 · 25,7 h | 11,4 / **25,0** · 133 · 25,7 h |
| 1,20 | 11,5 / 16,3 · 131 · 25,3 h | **9,8** / 16,4 · 131 · 25,2 h | 11,9 / **24,7** · 131 · 25,2 h |
Nessuna mora, nessuna proroga.

## Decisione
- **Adottato: Zaino 8 = 3.000 LN.** Accorcia il buco del cap 2 di 2-4 min in entrambe le versioni e non cambia la durata dei capitoli (±0,5 min) né il cap 1 (il golden path si ferma a Zaino 7).
- **Respinto il ripiego Zaino 9 = 5.600.** Nella versione breve aiuta il cap 3 (buco da 13 a 10 min), ma nella versione lunga, che è il riferimento attuale (11_LONGEVITA), anticipa Zaino 9 alla fine del cap 2 e apre nel cap 3 un buco di **25-27 min** prima di Zaino 10 (10.240 → 20.480). Livelli 9+ invariati.
- **Da tenere d'occhio (non causato da Zaino 8):** nella versione lunga il cap 3 ha già un buco di 16-18 min. Va coperto con contenuto (contratti, eventi di 11_LONGEVITA §2) e non con il prezzo dello Zaino. Il capitolo più lungo resta a 139-140 min a eff 0,70 come prima.
