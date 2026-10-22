# QA #71 — telefono: contrasto e barre vuote

Anteprima HTML della schermata home di `Phone.uxml` (misure di riferimento 900×1600 di `DioramaSolare.uss`, scalate come `PhoneController`), prima e dopo `Phone.uss` #71. Catture Playwright 360×800, 390×844, 412×915 (DPR 2).

| file | contenuto |
|---|---|
| `tel_*_prima.jpg` | testi crema #FFF9EE su crema (1,2:1) + due schede vuote |
| `tel_*_dopo.jpg` | testi bruno #3B2A20 (10,8:1), schede vuote nascoste |
| `tel_*_dopo_con_rata.jpg` | con riga rata (noce #7A4A2E, 5,8:1) e scheda obiettivo valorizzate |

Contrasti WCAG: bruno su crema #F3E3C3 10,8:1; bruno su scheda #E8D3AC 9,3:1; noce su crema 5,8:1 (tutti ≥ 4,5:1).
Nota: il telefono resta **non montato** nell'HUD di gioco (HUD minimo, decisione DevOn #64); la correzione riguarda solo `Phone.uxml`/`Phone.uss`.
