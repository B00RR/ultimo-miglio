# QA anteprima — onboarding graduale UI (#94)

Anteprima HTML (`anteprima/index.html`, stessa base della #78) con le classi di `Onboarding.uss`; testi reali della PR #62. Script: `anteprima/shoot.js` (Playwright, 360×800, 390×844, 412×915, DPR 2); misure in `anteprima/risultati.json`. Sfondo = concept, non la scena vera.

| file | stato |
|---|---|
| `onb_*_01_avvio` | avvio: pillola LN nascosta, nessun pad; restano obiettivo, Ada, suggerimento, joystick |
| `onb_*_02_comparsa_LN` | pillola LN a metà comparsa (scala 0,85, opacità 0,55; animazione 0,6→1 in 250 ms) |
| `onb_*_03_pad_barra` | pad obiettivo: freccia ▼ (#78) → barra LN "32/50" → pillola "18 LN" |
| `onb_*_04_aiuto20_freccia` | aiuto 20 s: freccia a bordo schermo (bersaglio fuori camera) |
| `onb_*_05_aiuto40_mano` | aiuto 40 s: mano fantasma nella zona joystick, il suggerimento testuale si nasconde |
| `onb_*_06_badge_nonno` | badge NONNO "6 pacchi/min" (22 dp) |
| `onb_*_07_salta` | "Salta tutorial" a sinistra dell'ingranaggio, riga obiettivo stretta |

## Esito (21 catture)
| controllo | 360 | 390 | 412 |
|---|---|---|---|
| HUD alto entro la fascia libera (350/2532) | 103/111 ✅ | 103/117 ✅ | 103/126 ✅ |
| Sovrapposizioni fra LN, obiettivo, ingranaggio, Salta, Ada, suggerimento, joystick, mano, freccia, barra, prezzo, badge | nessuna ✅ | nessuna ✅ | nessuna ✅ |
| Obiettivo senza ellissi con "Salta tutorial" visibile ("Assumi NONNO") | ✅ | ✅ | ✅ |
