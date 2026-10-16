# Playtest web LIVE v9 — QA indipendente fix P0 (8780)
- Data: 16/10/2026, 08:55–09:40 (Roma)
- URL: http://127.0.0.1:8780/ → `/workspace/um-recovery/build/WebGL_p0` (build 08:46). Branch `fix-p0-lorenzo`, fix `a87edf0`; PR #63 unita su main come `2a6b7de` (stessa build). Sorgenti letti solo per diagnosi
- md5 Build/*: `qa/playtest_v9_md5_start.txt` (data 7ef8bf90…, framework 6126f195…, loader 32196773…, wasm 359466a1…). Ricontrollo a metà (09:15) e a fine (09:36, `qa/playtest_v9_md5_end.txt`): **invariato**
- Metodo: Playwright Python (venv QA, chromium headless con swiftshader), sessione persistente con comandi incrementali (`/workspace/v9/srv.py` + `run.sh`). Input: tasti WASD e poi **joystick trascinato con il mouse**, perché la mappatura iso dei tasti non era affidabile a 5 fps. Touch: contesti `is_mobile=True, has_touch=True` (maxTouchPoints=1), drag con CDP `Input.dispatchTouchEvent` e `touchscreen.tap` (`/workspace/v9/touch.py`). Zoom 9.0 durante il gioco. Shot `qa/playtest_shots/v9_*.png`, console `qa/playtest_console_v9.log`. **Deviazione:** non ho usato MCP `browser_run_code_unsafe`: le chiamate da ~45 s non bastano per una partita continua
- Riferimenti: `qa/CHECKLIST_CAP1.md` (44 voci + 3 aggiunte in v9: C13, P07, Z02) e `12_CAP1_CRITERI_QA.md` (PR #75/#90)

## VERDETTO: **NO-VIA**
VIA 35 · NO-VIA 9 · non verificabili 3, su 47 voci.
Blocco principale: **soft-lock dopo l'assunzione di NONNO** (P0, nuova). Dopo l'acquisto nessuna zona funziona più: Arrivi non dà pacchi e il Bancone non vende. Il Cap.1 riparte solo con un reload.

## Esito P0
- **#58 collisioni: risolto, con un residuo.** Muri N/E, bordi S/O, angolo SO, scaffali, bancone, vasi, pile, lampione e staccionata fermano Nina; non l'ho mai vista attraversare un muro (`v9_C_walls_strip`, `v9_C_walls2_strip`, `v9_t2_b1`, `v9_C07_bancone_sud`, `v9_C08_parcel_lemon_block`). NONNO non attraversa i solidi (`v9_A03_nonno_strip24_crop`). Residuo, fuori dal perimetro di #58: NONNO e i clienti compenetrano Nina, e i clienti si compenetrano tra loro (C12, C13). La sacca dietro il muro "Vela & Figli" nasconde Nina (#82)
- **#59 NONNO: risolto.** Strip di 24 frame (`v9_A03_nonno_strip24.png`, ritaglio `v9_A03_nonno_strip24_crop.png`): forche e muso sempre nella direzione di marcia, all'andata verso lo scaffale (f0, f6, f12, f18) e al ritorno verso Arrivi (f1, f7, f14, f20). Mai in retromarcia
- **#60 spawn: risolto.** Reset dello storage + reload: Nina compare in (0,−1), in zona libera accanto al pad Zaino (`v9_60_a_boot`, `v9_M01_boot`). Dopo 1,3 s di D entra nella sacca dietro il muro "Vela & Figli" e resta coperta dal muro (`v9_60_strip_frames`, f2–f6). È la sacca nota (#82), non un'uscita dalla mappa

## Tabella
| ID | Esito | Shot / nota |
|---|---|---|
| C01 Muro N | VIA | v9_C01_N_centro (si ferma). Archi N non spinti uno per uno |
| C02 Muro/arco E | VIA | v9_C02_E_spinta, v9_C02_E_spinta2 |
| C03 Bordo S | VIA | v9_C03_S_push (fermata da lampione), v9_C03_S_lungo |
| C04 Bordo O | VIA | v9_C04_W_push |
| C05 Angoli | VIA | v9_C05_SW_diag (angolo SO pulito); NE, SE e NO fermati da props, nessuna uscita |
| C06 Scaffali | VIA | v9_t2_b1 (bloccata dalla testata), g_001 |
| C07 Bancone | VIA | v9_C07_bancone_sud; lato O chiuso da pila e vaso; il pad si raggiunge solo dal varco a NE (#82) |
| C08 Props | VIA | v9_C08_parcel_lemon_block, v9_C03_S_push |
| C09 Recinzioni | VIA | v9_C05_SW_diag |
| C10 Pad calpestabili | VIA | tutti i pad attraversati |
| C11 NONNO vs solidi | VIA | v9_A03_nonno_strip24_crop |
| C12 Clienti | **NO-VIA** | v9_C03_S_lungo2: i clienti compenetrano Nina e si sovrappongono tra loro; se restano bloccati >1,5 s vengono teletrasportati (Customer.cs r.24). #111 |
| C13 Nina vs NONNO (aggiunta) | **NO-VIA** | v9_w45: NONNO passa sopra Nina su Arrivi. #111 |
| A01 Nina 8 direzioni | VIA | v9_A01_8dir_strip |
| A02 Nina idle/walk/carry/deliver | VIA | v9_P01_arrivi_max (carry), v9_P03_vendita (+10, consegna) |
| A03 NONNO orientamento | VIA | strip 24 frame |
| A04 NONNO lift/carry/drop | VIA | strip (pila sulle forche) |
| A05 Clienti | VIA | in coda, girati nel verso del moto; la clip pay a 5 fps non si legge bene |
| A06 Ada | VIA | fumetto/obiettivo HUD "Muoviti → Vai agli Arrivi → Metti sullo scaffale → Porta al Bancone → Assumi NONNO → Compra lo Zaino → Deposito aperto!" |
| P01 Arrivi | VIA | MAX 5/5, poi MAX 6/6 dopo lo Zaino |
| P02 Scaffali | VIA | v9_P02_deposit (5→3) |
| P03 Bancone | VIA | v9_P03_vendita, v9_P03_vendite_41LN (10→23→37→41) |
| P04 NONNO 10 LN | VIA (incoerenza design) | v9_P04_strip6: pagamento a scalare 20→10, NONNO compare su Arrivi. Il design chiede 50 LN (ONB-09): #108 |
| P05 Zaino prezzo | VIA | "40 LN" (v9_M01_touch_strip), poi "38/34 LN" pagati a scalare |
| P06 Altri upgrade | non verificabile | Nella scena ci sono solo Pad_Zaino e Pad_AssumiNonno (SliceSceneBuilder.cs r.151–152). Scarpe e Insegna non esistono in questa build (vedi anche GATE-07) |
| P07 Zone dopo acquisto (aggiunta) | **NO-VIA P0** | v9_BUG_softlock_arrivi_vuoto, v9_t2e_strip: dopo l'assunzione LN restano fermi a 10, Arrivi non dà pacchi e il Bancone non vende. Dopo il reload torna tutto a funzionare (v9_b8). #111 |
| Z01 Zaino 40 LN | VIA | v9_Z01_strip2: comprato con LN guadagnati giocando, obiettivo "Deposito aperto!", capacità 5→6 (v9_C01_N_centro "MAX 6/6") |
| Z02 Pad dopo acquisto (aggiunta) | **NO-VIA P2** | v9_Z01_strip2: LN 71→29→0. Il pad passa al livello 2 ("39 LN") e prosciuga il portafoglio mentre Nina è sopra; il prezzo non si vede. Il saldo non torna: 71−34 = 37, non 29. #111 |
| B01 Ada B4 | VIA | testo leggibile a 360/390/1170 |
| S01 Ingranaggio | **NO-VIA P3** | v9_S_strip: si apre; un secondo tap sull'ingranaggio non lo chiude (solo SALVA). #111 |
| S02 Zoom | VIA | 4.5 → 9.0, vista più ampia |
| S03 Musica/SFX | non verificabile | I cursori si muovono e il valore si salva, ma in headless l'audio è bloccato ("AudioContext was not allowed to start") e l'effetto non si sente |
| S04 TAB telefono | **NO-VIA** | assente, duplicato di #64 |
| S05 Persistenza | **NO-VIA P2** | v9_S05_strip: Musica e Suoni a 0 restano dopo il reload; lo zoom 9.0 torna a 4.5. #111 |
| V01 Salvataggio | VIA (nota) | v9_V01_strip: passo tutorial, NONNO e LN restano; Nina torna allo spawn. LN 10→14 al reload (+4, da capire) |
| V02 Offline | VIA | v9_V02_strip: `context.set_offline(True)` dopo il boot, il gioco continua per 12 s senza errori. Progressione offline (chiudi/riapri) non misurata |
| K01 Camera | **NO-VIA** | v9_60_b_D13, v9_C_pocket_hidden: Nina coperta dal muro "Vela & Figli". Duplicato di #82 |
| K02 Mai fuori mappa | VIA | nessuna uscita |
| M01 390×844 touch | VIA | v9_M01_touch_strip (maxTouchPoints=1) |
| M02 360×800 touch | VIA | v9_M02_touch_strip |
| M03 1170×2532 touch | VIA | v9_M03_touch_strip (390×844 @3x) |
| M04 Joystick touch | VIA | il drag CDP muove Nina nel verso del drag; il tap sull'ingranaggio apre le impostazioni |
| F01 FPS | VIA (annotato) | headless 4–8 fps, frame massimo 150–333 ms (swiftshader) |
| F02 Memoria | VIA | heap JS 29.8–31.6 MB, stabile |
| F03 Console | VIA | 0 errori ed eccezioni, 0 Shader/NavMesh; solo warning AudioContext e GPU stall ReadPixels (screenshot) |
| R01 Regressioni #41–#49 | non verificabile | non ripetute caso per caso per mancanza di tempo |
| R02 Cap.1 completo | **NO-VIA** | Arrivi→Scaffale→Bancone→NONNO→Zaino completato, ma solo con un reload dopo NONNO (P07, #107) |

## Criteri 12_CAP1_CRITERI_QA.md
- Tempi ONB-07 (Zaino 1:23 ±20 s), ONB-09 (NONNO 2:10 ±5 s) e ECO-03 (rata 1 a 34:00 ±120 s, finestra 32:00–36:00): **non confrontabili.** Il mio giro non è il golden path del bot: headless a 5 fps, navigazione a tentativi e un reload obbligato. Ho osservato NONNO a ~20 min e lo Zaino a ~37 min di sessione reale, dati inutilizzabili come tempi di gioco. La rata 1 non l'ho raggiunta. Vanno misurati con il bot
- ONB-09 prezzo: la build chiede **10 LN**, il design 50 LN. Incoerenza: #108
- ONB-09a/b/c/d, GATE-01..07 e i criteri sui passi bloccati (PR #90): **n.a., build senza onboarding.** Il TutorialManager ha solo il banner a passi: niente `onboarding.json` né aiuti 10/20/40 s. Osservazione utile: dopo l'assunzione di NONNO il giocatore resta bloccato senza nessun suggerimento (P07)
- ECO-10: il pad Zaino livello 2 mostra 39 LN; da confrontare con `upgrades.json`
- Il Protocollo playtest dal vivo (F) è fuori scopo

## Bug (issue pubblicate 16/10 09:44, label bug + cap1; nessuna chiusa)
- #107 P0 — Soft-lock: dopo l'acquisto di un pad (NONNO) Arrivi, Scaffali e Bancone non funzionano più fino al reload (P07, R02). Correlata a #104 (fix del Tech Lead)
- #108 P2 — Incoerenza design/build: ASSUMI NONNO costa 10 LN, il design (ONB-09) 50 LN (P04)
- #109 P2 — Zoom impostazioni non persistente dopo il reload (S05)
- #110 P2 — Pad Zaino: dopo l'acquisto prosciuga gli LN nel livello 2 senza mostrare il prezzo (Z02)
- #111 P2 — Clienti e NONNO compenetrano Nina; i clienti si sovrappongono e vengono teletrasportati (C12, C13)
- #112 P3 — Impostazioni: il tap sull'ingranaggio non chiude il pannello (S01). Commentata come duplicato di #36 (secondo il Tech Lead già risolto lì)
- Duplicati: #82 (sacca dietro il Bancone, K01), #64 (S04)
- Commenti di esito su #58, #59, #60 (tutte risolte; residui in #111 e #82)
