# QA tutorial HUD — issue #77 (+ #5)

Anteprima HTML fedele a `Hud.uxml` + `HudV2.uss` + `Tokens.uss` + nuovo `TutorialHud.uss` (stesse misure in dp, font Fredoka, texture cartone, ritratto `tex_ada_portrait.png`).
Testi reali dalla PR #62 (`onboarding_testi.json`, branch `narrative/testi-cap1`), non modificati.
Sfondo: `docs/concept/img/ref_bg_gameplay_iso.jpg` (solo riferimento, la scena vera va vista in Unity/WebGL).
Linee fucsia = fasce libere della GUIDA §10 (350 px alto / 500 px basso su 1170×2532, scalate all'altezza).

Script: `anteprima/shoot.js` (Playwright, viewport 360×800, 390×844, 412×915, DPR 2). Misure in `anteprima/risultati.json`.

Prima (build web pubblicata, 360×800): `prima_build_web_360.jpg` — nessun fumetto di Ada a schermo, obiettivo a tutta larghezza.

## Casi
| caso | contenuto |
|---|---|
| move | passo 1, testo Ada + suggerimento `onb.move.card` |
| shelve | passo 3, suggerimento più lungo `onb.deposit.hint` (50 caratteri) |
| nonno | pad acquisto obiettivo: freccia ▼ sopra la pillola "10 LN" |
| nonno_alto | pad in alto sullo schermo: colonna ribaltata sotto il pad, freccia ▲ |
| fine70 | testo più lungo (`fine`, 62 caratteri) |

## Checklist sovrapposizioni (esito anteprima)
| controllo | 360×800 | 390×844 | 412×915 |
|---|---|---|---|
| HUD alto (LN/giorno, obiettivo, ingranaggio, Ada) dentro la fascia alta | 103 ≤ 111 dp ✅ | 103 ≤ 117 dp ✅ | 103 ≤ 126 dp ✅ |
| Fumetto Ada: righe / ellissi | 2 righe, nessun taglio ✅ | 2 righe ✅ | 1–2 righe ✅ |
| Ada × obiettivo × pillola LN × ingranaggio | nessuna ✅ | nessuna ✅ | nessuna ✅ |
| Suggerimento/CTA dentro la fascia bassa | inizia a 642 = 642 ✅ | 686 ≥ 677 ✅ | 757 ≥ 734 ✅ |
| Suggerimento × joystick (a riposo, 12–124 dp dal fondo) | nessuna ✅ | nessuna ✅ | nessuna ✅ |
| Suggerimento senza ellissi | ✅ (13 dp) | ✅ | ✅ |
| Freccia × prezzo | nessuna ✅ | nessuna ✅ | nessuna ✅ |
| Freccia/pillola nella fascia alta | no (clamp 184 dp / ribaltamento) ✅ | ✅ | ✅ |
| Joystick flottante sopra il suggerimento | il suggerimento si nasconde (codice) — da verificare in Unity | | |
