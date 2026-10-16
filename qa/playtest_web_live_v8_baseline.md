# Playtest web LIVE v8 — BASELINE pre-fix (8779)
- Data: 16/10/2026, 07:44–07:50 (Roma)
- URL: http://127.0.0.1:8779/ → `/workspace/um-recovery/build/WebGL_p2`. Commit dichiarato `cec53a1` (nella cartella build non c'è git; sorgenti letti con `git show cec53a1:`; il working tree del repo è sul branch `fix-p0-lorenzo` con fix in corso, non usato)
- md5 Build/*: `qa/playtest_v8_md5_start.txt` (data fc37120b…, framework 6126f195…, loader 32196773…, wasm 5aadee32…). Ricontrollo a metà (07:47) e a fine: **OK, invariato**
- Metodo: MCP Playwright `browser_run_code_unsafe`, un file JS nuovo per ogni chiamata (`/workspace/pw8.sh`, prefisso shot `v8b_`). Storage svuotato + reload, avvio ~20 s, viewport 390×844. Console: `qa/playtest_console_v8_baseline.log`
- Checklist di riferimento: `qa/CHECKLIST_CAP1.md`. Questo è un **giro baseline sulle voci principali**. Il LIVE definitivo sarà sulla 8780

## VERDETTO: NO-VIA
Muri e scaffali sono attraversabili: riprodotto (#58). NONNO va all'indietro: confermato dal codice di cec53a1 (#59). Molte voci non sono state verificate (vedi tabella).

## Tabella checklist (baseline)
| ID | Esito | Evidenza / nota |
|---|---|---|
| C01/C02/C05 Muri N/E + angolo NE | **NO-VIA** | `v8b_strip_muri1.png`, `v8b_c_n2.png`, `v8b_c_w1.png`, `v8b_c_w2.png`: dopo 10 s di W e 10 s di A Nina sparisce **dietro/dentro il muro**; resta visibile solo la freccia fuori schermo al centro. In `v8b_01_arrivi.png` dopo il reload Nina è dietro il muro dell'insegna "Vela & Figli" (si vede solo la punta del casco). Codice: `DepotKit.Place(..., solid:false, ..., wall:true)` per tutti i muri e archi (DepotKit.cs r.25–36) → nessun collider. I veri bordi `Bound_*` stanno a ±9.15/±11.15 (SliceSceneBuilder.cs r.40–42) e il clamp di Nina è x ±8.2 (NinaController.cs), oltre il muro E a x 7.6 |
| C03/C04 Bordi S/O | VIA parziale | `v8b_c_sh2.png`: Nina ferma alla staccionata/lampione SO (al bordo combaciano plinto e clamp). Lampione in leggera sovrapposizione |
| C06 Scaffali | **NO-VIA** | `v8b_strip_scaffale2.png` (q1→q2): con A tenuto Nina passa da davanti a dietro la fila degli scaffali. In q1 è nascosta dentro lo scaffale. Codice: collider del corpo distrutto (SliceSceneBuilder.cs r.127 "B5 P3") e mesh kit posata con `solid:false` (r.129) |
| C07 Bancone | NO-VIA (da codice) | Collider distrutto (SliceSceneBuilder.cs r.139 "B5"). Non provato in gioco |
| C08 Props | VIA parziale | `v8b_strip_scaffale.png`: il vaso del limone blocca Nina (4 s di A). Altri props non provati |
| C09–C12 | Non verificato | Tempo. NONNO non è stato assunto in questo giro |
| A01/A02 Nina | VIA parziale | Faccia nella direzione di moto, walk/idle ok negli shot. Carry e pay non visti |
| A03 NONNO orientamento | **NO-VIA (da codice)** | A cec53a1: `Rig(nonno.transform, "bot_nonno")` usa lo yaw di default **180°** (SliceSceneBuilder.cs r.157 + `Rig(..., yaw=180f)` r.284). `NonnoRobot.StepMove` ruota con `LookRotation(d)` verso la meta (NonnoRobot.cs ~r.140), quindi il modello con le forche su +Z, girato di 180°, avanza in retromarcia. Il branch fix-p0-lorenzo ha già `0f` con commento #59. Strip ≥20 frame **non fatta**: in questo giro NONNO non è stato assunto |
| A04/A05/A06 | Non verificato | Clienti: anche `Customer.cs` r.23 usa LookRotation; il suo orientamento non è stato controllato a schermo |
| P01–P06, Z01, R02 | Non verificato | Cap.1 non percorso in baseline (navigazione a tasti lenta, ~10–14 fps) |
| B01 Ada | Non verificato | — |
| S01 Ingranaggio | VIA | `v8b_settings_open.png`: pannello IMPOSTAZIONI con Zoom (−/+ 4.5), Musica, Suoni e SALVA |
| S02/S03/S05 | Non verificato | Effetto e persistenza non provati |
| S04 TAB telefono | **NO-VIA?** | Nel pannello non c'è nessun TAB telefono. Da verificare sul design se è previsto in Cap.1 |
| V01/V02 Salvataggio | Non verificato | Nota: dopo il clear + reload Nina è partita dietro il muro N (`v8b_01_arrivi.png`) e non dalla posizione iniziale (0,0) vista nel primo boot (`v8b_00_boot.png`). Da indagare |
| K01/K02 Camera | NO-VIA (conseguenza di C01) | Nina fuori vista dietro i muri, `v8b_c_w2.png` |
| M01–M03 Viewport | VIA (solo layout) | `v8b_strip_vp.png`: HUD, pillola LN e obiettivo leggibili a 360×800, 390×844 e 1170×2532. **Touch non verificato**: il contesto MCP non è mobile (`maxTouchPoints=0`), quindi has_touch/is_mobile non sono attivabili su una pagina già aperta |
| M04 Joystick touch | Non verificato | Come sopra |
| F01 FPS/hitch | Annotato | Headless: 9–14 fps, frame massimo 83–167 ms (limite del browser headless) |
| F02 Memoria | VIA | Heap JS 31.0 → 31.1 MB in ~1 min |
| F03 Console | VIA | Solo 2× 404 di favicon.ico. 0 errori Shader/NavMesh |
| R01 #41 #43 #44 #49 #42 | Non verificato | — |

Conteggio sulle righe della tabella: VIA 6 voci (S01, M01, M02, M03, F02, F03) · VIA parziale 3 righe (C03/C04, C08, A01/A02) · NO-VIA 6 righe (C01/C02/C05, C06, C07, A03, K01, S04 da confermare) · F01 solo annotato · tutto il resto non verificato.

## Bug
- #58 muri/scaffali attraversabili: **RIPRODOTTO** su 8779. Bozza di commento qui sotto (non pubblicata)
- #59 NONNO all'indietro: **confermato da codice**, non visto a schermo. Bozza di commento qui sotto (non pubblicata)
- Nuovi candidati (issue non aperte, bozze qui sotto): (a) Nina spawna/riappare dietro il muro N dopo reset + reload; (b) TAB telefono assente nelle impostazioni (da confermare col design)
