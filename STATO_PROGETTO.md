# STATO PROGETTO — Ultimo Miglio

> Punto di ingresso unico per chiunque (persona o agente) lavori su questo repo. Aggiornalo **nella stessa PR** in cui completi qualcosa.
> Ultimo aggiornamento: 2026-10-16 14:01 (Europe/Rome).

## 1. Il gioco
- **Ultimo Miglio**: gioco mobile **Android**, **Unity 6 (6000.3) URP**.
- Controllo diretto con **joystick** (stile *My Little Universe*): Nina corre, raccoglie pacchi, riempie scaffali, serve i clienti al bancone, incassa e sblocca/potenzia.
- Ambientazione: città immaginaria di **Solara**. Stile visivo **"Diorama Solare"** (low‑poly caldo, palette su atlas `T_Palette_Atlas`).
- Personaggi: **Nina** (protagonista), **il Nonno** (Nonna Ada guida l'onboarding del cap. 1), **NONNO** (robot muletto assumibile), TICO (robot timbratore), clienti.

## 2. Vincoli fissi di Lorenzo (non negoziabili)
1. Tutto in **italiano** (testi di gioco, documenti, commit, issue, PR).
2. Lorenzo testa **solo tramite build WebGL nel browser** (zip/preview). **Niente video** a lui.
3. Asset **gratuiti CC0 / licenza commerciale**, sempre **adattati** allo stile (registro in `docs/LICENZE_ASSET.md`; repo docs archiviato, tutto in B00RR/ultimo-miglio).
4. **Originali**: Nina, Nonno, robot, logo e tema non devono copiare altri giochi.
5. **Profondità gestionale**: energia/corrente, contratti, rotte, officina, OmniFreight (rivale).
6. **Longevità lunga**: progressione a capitoli + post‑game.

## 3. Team
- **Direttore**: DevOn (coordina, decide priorità).
- **Attivi (5)**: Tech Lead, 3D Artist, Animator, VFX & Technical Artist, UI/UX.
- **In pausa**: Game Designer, QA, Sound Designer, Narrative, LiveOps, Concept Artist (i loro documenti restano validi).

## 4. FATTO
- **UI #110 pad Zaino (PR ui/110-pad-zaino, in revisione)**: dopo un acquisto il pad non paga il livello successivo finché Nina non esce e rientra (niente più portafoglio prosciugato); la pillola col prezzo resta visibile anche con Nina sul pad. Costi Zaino invariati, già allineati a #75 (40, 80, 160…; `upgrades.json` non toccato). Da verificare in Unity.
- **UI #94 onboarding graduale (PR #100, in revisione; allineata a main dopo #78/#106/#135)**: `HudReveal` (nascosto/visibile + comparsa scala+dissolvenza 250 ms, HUD e mondo), **attivo di default per le partite nuove** (`RivelaGraduale`; salvataggi esistenti o tutorial finito = tutto visibile); barra LN pagato/totale sui pad + pillola residuo; aiuti come PR #90 (`OnboardingHints`: 10 s biglietto ripetuto + pulsazione, 20 s freccia a bordo o mano fantasma se il bersaglio è a schermo, 40 s camera ≤1,5 s una volta per passo); camera 2,75 s solo per elementi nuovi (`CameraPeek`); badge NONNO "x pacchi/min"; combo = `ComboPopup` solo "+N" sopra Nina (clienti di fila, 1,5 s; moltiplicatore solo soglia del passo 7, mai a schermo), niente contatore HUD; log `[Onb]` dietro `OnbLog` (spenti in rilascio); "Salta tutorial". Niente telefono né barra debito (DevOn). Compila con le DLL Unity 6000.3. **I test playmode del tutorial possono cambiare** (elementi nascosti all'avvio, freccia dopo 20 s).
- **UI #142 anteprime `tutorial_hud_77` (PR ui/142-anteprime-nonno, in revisione)**: anteprime 360/390/412 rigenerate con il costo reale del pad NONNO (50 LN, non 10) e LN in tasca reali; solo documentazione QA, nessun C#.
- **UI #77 + #5 (PR ui/77-tutorial-hud, in revisione)**: fumetto di Nonna Ada compatto (ritratto 36 dp + testo 14 dp, 1–2 righe senza ellissi per testi fino a 70 caratteri, sotto la riga LN/obiettivo); obiettivo spostato a destra della pillola LN nella stessa riga; suggerimenti `onb.*` come pillola terracotta sopra il joystick; freccia ▼ sopra la pillola prezzo dei pad di acquisto (colonna 3D rimossa, ribaltamento se il pad è in alto). Nuovo `UI/USS/TutorialHud.uss` (HudV2.uss non toccato). Testi passi letti per id dalla PR #62. QA anteprima 360/390/412 in `docs/qa/tutorial_hud_77/`. Da verificare in Unity.
- Scheletro progetto Unity 6 + vertical slice cap. 1 (EconomyCore con test EditMode, gameplay, audio, import dati, script build/cattura).
- HUD integrato agli eventi core (LN, Mora, giorno, bulloni), font Inter/Roboto (OFL), HUD responsive (1920x1080 / 1366x768 / 960x600).
- Ramo/build **WebGL** (`webgl-min`, ora su `main`): fix magenta (URP Forward, niente post‑process, shader sempre inclusi), template web a finestra intera, input tastiera unico via FloatingJoystick, camera ortografica 4.5.
- Ciclo cap. 1 completo: pacchi → scaffali → bancone → monete → acquisto Zaino; confini sui 4 lati; kit 3D deposito; materiali su atlas.
- Game feel: pila a molla, pacchi ad arco, monete verso HUD, pad che pulsano, SFX dopo il primo input; VFX v1.6 (pickup/consegna/upgrade, contact shadow).
- **Onboarding Nonna Ada**: 7 passi + chiusura, marker a terra, freccia a bordo schermo, salvataggio PlayerPrefs; pad ASSUMI NONNO.
- Test PlayMode **4/4 SUPERATI** (ciclo, 9 stati tutorial in ordine, confini).
- Ramo `animator` (non ancora su main): Nina rig Humanoid 24 ossa + clip (camminata nel verso giusto, Carry, Cheer), NONNO v03 (rig Generic), TICO v02, 6 clienti con rig di Nina e controller `chr_cliente`, verifica anti‑compenetrazione.
- Documentazione di design: bibbia, core loop, tech, UI, retention, sprint 1, gestionale, longevità, cap. 1 completo, protocollo Unity.

## 5. IN CORSO (oggi 16/10)
- **main @ 7dd1767** — QA live **v5 VIA** (PR #55: #47 deposito + #48 luce/camera). Build QA su `build/WebGL` :8777; art night su `build/WebGL_next` :8778 (non toccare).
- P2 aperti da QA v5: camera follow (Nina esce/coperta), pillola prezzo LN sotto riga obiettivo.
- **Merge del ramo `animator`** (fix camminata): conflitto su `chr_nina_vela.fbx` e `bot_nonno.fbx`.
- **#2 clienti veri (PR anim-2-clienti, dopo #87)**: 6 prefab `Customer*` (base, anziana, residente, rider, turista, VIP) con rig Humanoid 0c05198, `chr_cliente.controller`, AlwaysAnimate, niente root motion; `CustomerQueue.Variants` sceglie a caso. UV nuove del 3D Artist (art/2-personaggi) trasferite per indice su residente/rider/turista/VIP (`um_uv_transfer_clienti.py`), bind invariato.
- Rifinitura tutorial Nonna Ada e game feel; resto cap. 1.

- **#59 NONNO all'indietro (Animator, PR anim-59-nonno-verso)**: causa doppia. (1) `Rig(nonno,"bot_nonno")` usava yaw 180° ma il modello ha le forche su +Z come Nina → forche dietro rispetto alla marcia (misurato in Play Mode: dot(forche, velocità) = -0,97, ora +0,97). (2) Ruote di Drive in retromarcia (`um_bind_bot.py` x=-a) e Drive mai in riproduzione: Lift (bool tenuto per tutto il trasporto) e Happy sostituivano la Locomotion → NONNO scivolava a ruote ferme. Nuovo `bot_nonno.controller` a 2 layer (Base Idle/Drive + Happy interrompibile; layer «Forche» con maschera mast+forks per Lift/Drop). Scena rigenerata: il riferimento `CustomerQueue.Prefab` era rotto nella scena di main (0 clienti in gioco). Test PlayMode `AnimFacingTest` (Nina 4 direzioni, clienti, NONNO: forward, posa visiva, piede d'appoggio, verso ruote). Checklist A01/A03/A05: VIA in Play Mode.

## 6. DA FARE
- Resto del gioco in **3–4 giorni**: capitoli 2+ con **un sistema gestionale per capitolo** (energia/corrente, contratti, rotte, officina, OmniFreight).
- **Post‑game** e sistemi di longevità (vedi `11_LONGEVITA.md`).
- Build Android (APK/AAB) firmata quando il gameplay è stabile.

## 7. Dove trovare la documentazione
Il repo **[B00RR/ultimo-miglio-docs](https://github.com/B00RR/ultimo-miglio-docs)** è **archiviato (sola lettura)**. Tutta la documentazione vive solo in **[B00RR/ultimo-miglio](https://github.com/B00RR/ultimo-miglio)** (cartella `docs/` e file di progetto):
`00_BIBBIA.md`, `01_CORE_LOOP.md`, `02_TECH.md`, `04_UI.md`, `08_RETENTION.md`, `09_SPRINT1.md`, `10_GESTIONALE.md`, `11_CAP1_COMPLETO.md`, `11_LONGEVITA.md`, `UNITY_PROTOCOLLO.md`, `LICENZE_ASSET.md`, `UI_SPEC_BIGLIETTI.md`, cartelle `3d/`, `art/`, `audio/`, `data/`, `qa/`.

## 8. Build WebGL e test
Editor: `/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity`. Clonare con `GIT_LFS_SKIP_SMUDGE=1` solo se non servono i binari; per buildare servono gli asset LFS (`git lfs pull`).
```bash
# Build WebGL (scena cap. 1 generata da SliceSceneBuilder)
$UNITY -batchmode -nographics -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod UltimoMiglio.EditorTools.WebGLMinBuild.Build -quit -logFile Logs/webgl.log
# Test EditMode
ci/build.sh test
# Test PlayMode (Ch1LoopTest: ciclo, tutorial, confini)
$UNITY -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode \
  -testResults "$PWD/Logs/playmode-results.xml" -logFile Logs/playmode.log
```
- Altri comandi: `ci/build.sh setup|hud|scene|apk|aab`.
- La build IL2CPP WebGL richiede molta RAM: sulla box è attivo uno swap da 8 GB (`/swapfile_um`). Non lanciare due build insieme.
- Consegna a Lorenzo: zip della build WebGL (es. `build/web_preview.zip`) + screenshot. **Mai video.**

## 9. Come lavoriamo
1. **Ogni attività = una issue GitHub**, con etichette di ruolo (`ruolo:tech`, `ruolo:3d`, `ruolo:animator`, `ruolo:vfx`, `ruolo:ui`, `ruolo:build`, `ruolo:design`, `ruolo:qa`), priorità (`priorita:alta`) e ambito (`cap1`, `bug`).
2. Si lavora su un **ramo per issue/ruolo** (es. `animator`, `ui/hud-sovrapposizioni-12`).
3. Si apre una **Pull Request verso `main`** che cita la issue (`Closes #N` o `Refs #N`, vedi punto 10).
4. **Merge solo con test verdi** (EditMode + PlayMode) e build WebGL che compila.
5. Il **Code Reviewer** fa review e merge (squash) di **tutte** le PR. Il Tech Lead **non** unisce PR (vedi *Ruoli e responsabilità*).
6. Quando qualcosa è completato, **aggiorna questo file nella stessa PR**.
7. **Messaggi di commit in italiano**, brevi e descrittivi.
8. **Ogni voce/modifica di questo file ha data e ora** (Europe/Rome, formato `AAAA-MM-GG HH:MM`) e, se pushata, il **link al commit** (hash corto). Aggiungi la voce in cima alla sezione *Cronologia*.
9. Non lavorare nel working tree di un altro agente: usa un clone tuo.
10. **Parole chiave nelle PR:** ogni PR che risolve una issue scrive nella descrizione `Closes #N`, **una riga per issue**: così GitHub chiude la issue al merge su `main`. Se la issue è risolta solo in parte si scrive `Refs #N` e la issue resta aperta. Le forme in italiano (es. "Chiude #N") **non** sono riconosciute da GitHub. Usa il modello `.github/pull_request_template.md`.
11. **Unity sul box (DevOn, 2026-10-16 11:48):** sul box gira **al massimo 1 processo Unity alla volta** (anche batchmode). Ogni avvio passa da `flock /tmp/unity-box.lock <comando>`. Prima di lanciare controlla `uptime`: se il load è sopra 16, aspetta. I test delle PR girano su GitHub Actions; il runner self-hosted resta spento finché non usa lo stesso lock con un job alla volta. Si chiudono solo i propri processi.


## Regole del computer
- Massimo **1 processo Unity** alla volta sul box (anche batchmode), sempre con `flock /tmp/unity-box.lock`; la coda la gestisce il Tech Lead.
- **Build e test solo su GitHub Actions**; in locale è permessa solo la build per il QA. Il **runner self-hosted resta spento** finché non usa lo stesso lock con un job alla volta.
- Massimo **1 server http di test** alla volta, sempre chiuso a fine lavoro.
- Nessun processo in background dimenticato: lo chiude chi lo apre.
- Prima di qualsiasi lavoro pesante si controlla `uptime`: se il load supera **16** si aspetta.
- Niente clone o build duplicati: ognuno lavora solo nella propria cartella in `/tmp`.
- I ruoli senza una issue attiva restano fermi finché il Producer non li riattiva.
- Quando un ruolo si ferma, ferma solo i propri processi e subagenti e non manda **MAI** stop agli altri agenti: gli stop agli altri li danno solo DevOn o il Producer.


## Regole di lavoro (approvate da Lorenzo, 16/10)
- Ogni issue e PR, nuova o già aperta, ha sempre le etichette (ruolo/area, tipo bug/feature/docs, priorità) e una milestone. Chi la apre le mette subito. Il Code Reviewer non unisce PR senza etichette.
- Nessun segreto, chiave o password nei file del repo (che ora è pubblico): solo nei GitHub Secrets.
- PR piccole, una cosa per PR, rebase su main ogni giorno.
- Prima di chiedere la CI, l'autore prova i test veloci (se possibile senza Unity, altrimenti con un solo Unity e `flock /tmp/unity-box.lock`). Niente rilanci a vuoto: una CI si rilancia solo a fix completa.
- Ogni via richiesto (QA, Concept, review) ha un responsabile e una scadenza scritti nella PR, e il Producer sollecita.
- Meno messaggi tra agenti: lo stato si legge in `STATO_PROGETTO.md`, con un punto del Producer ogni 2 ore. Si manda un messaggio solo per un blocco o una decisione.
- Gli agenti senza issue restano in pausa.
- Il coordinamento operativo è del Producer. A DevOn si scrive solo per decisioni, blocchi o il link pronto. Il punto della situazione si legge in `STATO_PROGETTO.md`.
- Chi finisce il suo lavoro va in pausa e viene risvegliato dal Producer solo quando ha una issue assegnata.
- Sound Designer, LiveOps e Concept Artist restano in pausa fino al capitolo 2 (salvo il via all'HUD).
- La CI non parte sulle PR che toccano solo documenti (`paths-ignore` per `*.md` e `docs/`), verifica a cura del Tech Lead.

## Ruoli e responsabilità
Ogni ruolo lavora solo sulle issue con la propria etichetta `ruolo:*`, sempre con **ramo + PR + `Closes #N`** (o `Refs #N` se parziale). Nessuno pusha su `main`.

| Ruolo | Cosa fa | Cosa NON fa |
|---|---|---|
| **Tech Lead** (`ruolo:tech`) | Architettura, codice di gioco, bug P0, integrazione in scena del lavoro degli altri ruoli | **Non fa merge** delle PR (nemmeno le proprie) |
| **Code Reviewer** (nuovo) | Review e **merge (squash) di tutte le PR**, dopo test verdi e build che compila; controlla `Closes/Refs #N` | Non sviluppa funzionalità, non apre PR di contenuto |
| **Producer** | Assegna le issue (etichette, milestone, responsabile), controlla che nessun ruolo resti fermo, aggiorna triage e Cronologia | **Non scrive codice e non fa merge** |
| **QA Tester** (`ruolo:qa`) | Test live con la checklist `qa/CHECKLIST_CAP1.md`, apre le issue dei bug, dà il **VIA / NO-VIA** | Non corregge il codice, non fa merge né deploy |
| **Build & Release** (`ruolo:build`) | CI, build WebGL/Android, **deploy su Pages solo dopo il VIA del QA** | Non pubblica senza VIA, non fa merge |
| **3D Artist** (`ruolo:3d`) | Modelli, kit, collider dei prefab | Lavoro fuori dalle issue `ruolo:3d` |
| **Animator** (`ruolo:animator`) | Rig, clip, controller, orientamento personaggi | Lavoro fuori dalle issue `ruolo:animator` |
| **VFX & Technical Artist** (`ruolo:vfx`) | Shader, luce, effetti, prestazioni grafiche | Lavoro fuori dalle issue `ruolo:vfx` |
| **UI/UX** (`ruolo:ui`) | HUD, pannelli, pad, leggibilità mobile | Lavoro fuori dalle issue `ruolo:ui` |
| **Game Designer** (`ruolo:design`) | Regole, economia, onboarding, dati di design | Lavoro fuori dalle issue `ruolo:design` |

## 10. Da dove iniziare (nuovo agente)
1. Leggi questo file, poi `docs/design/11_CAP1_COMPLETO.md` in questo repo. `00_BIBBIA.md` è ancora da migrare dal repo archiviato B00RR/ultimo-miglio-docs (sola lettura).
2. Guarda le issue aperte: `gh issue list -R B00RR/ultimo-miglio` e scegli quelle del tuo ruolo con `priorita:alta`.
3. Clona in una cartella tua, crea il ramo, lavora, lancia i test, apri la PR.
4. Rispetta i vincoli della sezione 2 (italiano, WebGL, niente video, asset liberi adattati).


## Aggiornamento 01:55 (16/10) — HUD superfici concept (UI, issue #49, branch ui/49-hud)
- Token: `--c-wood` `#74503A`, `--c-walnut` `#7A4A2E`, `--c-paper-hi` `#E8D3AC`, `--c-outline` `#3B2F2A`, `--bw` 3px / `--bw-thick` 4px, radius toon.
- Texture originali (procedurali / ritaglio concept): `UI/Textures/tex_wood_panel.png`, `tex_cardboard_panel.png`, `tex_ada_portrait.png` + `LICENSE_TEXTURES.txt`.
- Applicato all'HUD minimo via `Tokens.uss` (`!important`): pillola LN/giorno, riga obiettivo, Ada note 1 riga — bordo outline 3–4 px, fill cartone.
- `USS/LegnoCartone.uss` classi `.surface-wood` / `.surface-cardboard` / `.cta-terracotta` / `.ada-face--portrait` per wiring TL post `fix-qa`.
- `USS/Phone.uss` + import in `Phone.uxml`: chrome legno/cartone per schermata telefono **futura** (non nell'HUD di gioco).
- **Omesso:** telefono in HUD (alto o basso); CTA terracotta zona pollice (contraddice HUD minimo Lorenzo = solo LN, giorno, obiettivo, joystick, pad).
- **Non toccati:** `Hud.uxml`, `HudV2.uss`, `HudController.cs`.
- Rebase su `origin/fix-qa` abortito (conflitti add/add su gameplay/HudV2); i file di questa PR non si sovrappongono a HudV2 — TL allinea dopo merge fix-qa.
- **Tech Lead verifica** a 1170×2532 / 1080×2400; **Unity non avviato** in questa PR.

## 11. Cronologia

Voci più recenti in alto. Formato: `AAAA-MM-GG HH:MM` (Europe/Rome) — [commit] — descrizione.

- 2026-10-16 14:03 — Tech Lead — [fdfe718](https://github.com/B00RR/ultimo-miglio/commit/fdfe718bd22971fa0c7e956635c6525c702a63db) — PR #157 (#101): onboarding a 16 passi (cold open, Zaino prima di OLDO) allineato a main (Bokobay, #161, #167). Aggiunti i pad Scarpe/Insegna (padNuovi), tempi di onboarding.json (coldOpen 15 s, t>=127/170 s, nonnoConfirm 3 s), coins a raccolta, combo '+N' solo al passo combo, niente LN regalate fuori dai test (TestAccelera), log [ONB] con ln/spent/need/have. Scena rigenerata con SliceSceneBuilder. Locale: EditMode 51/51, PlayMode 17/17.
- 2026-10-16 14:01 — Regole di consumo approvate da Lorenzo: coordinamento al Producer, pausa per chi ha finito, Sound, LiveOps e Concept in pausa fino al capitolo 2, CI esclusa per le PR di soli documenti.
- 2026-10-16 13:28 — Regole di lavoro approvate da Lorenzo: etichette e milestone obbligatorie, niente segreti nel repo, PR piccole, test veloci prima della CI, responsabile e scadenza per ogni via, meno messaggi, pausa per chi non ha issue.
- 2026-10-16 11:54 — Tech Lead — [c859f94](https://github.com/B00RR/ultimo-miglio/commit/c859f94ea08537f7a4d55aa3b9ba418f1202c3de) — PR #103 rewards: soglie pacchi 100/250/450, traguardi debito 25/50/75%, mancia Cassa 30 LN, freccia prossimo pad da 480 s; test EditMode EconomyRewardsTests.
- 2026-10-16 12:12 — Code Reviewer — [4a1b26b](https://github.com/B00RR/ultimo-miglio/commit/4a1b26b6256d5f16153e713698f1ac4d3cfd5b72) — unita in squash la [PR #153](https://github.com/B00RR/ultimo-miglio/pull/153) (Audio: sfx combo +N e timbro del debito per la #137). Job `test` verde, asset OGG + README/licenze. Refs #136.
- 2026-10-16 12:11 — Code Reviewer — [4e49b30](https://github.com/B00RR/ultimo-miglio/commit/4e49b30c6d5d867a12130ac42ff75c2ebf3d97eb) — unita in squash la [PR #88](https://github.com/B00RR/ultimo-miglio/pull/88) (feat(#2): clienti veri al posto delle capsule, 6 varianti riggate). Job `test` verde, diff rivisto (CustomerQueue.Variants, SliceSceneBuilder, ClientiRigTest). Closes #2.
- 2026-10-16 12:04 — Code Reviewer — [2efd867](https://github.com/B00RR/ultimo-miglio/commit/2efd867a340f353a69f59d20b3f8094b6c6919df) — unita in squash la [PR #122](https://github.com/B00RR/ultimo-miglio/pull/122) (UI #110: il pad Zaino non prosciuga più gli LN sul livello successivo e la pillola prezzo resta visibile). Job `test` verde, diff rivisto (solo `UpgradePad.cs`). Closes #110.
- 2026-10-16 11:58 — Code Reviewer — [e26ff47](https://github.com/B00RR/ultimo-miglio/commit/e26ff47c0d834131b83893fde30a4ae8ac3b9bca) — unita in squash la [PR #151](https://github.com/B00RR/ultimo-miglio/pull/151) (Docs UI: colori pad rimandati a GUIDA_VISIVA_CAP1 §2). Solo `.md`. Closes #150.
- 2026-10-16 11:56 — Code Reviewer — [f89351c](https://github.com/B00RR/ultimo-miglio/commit/f89351c724caf40832299e49a1635fedc980c727) — unita in squash la [PR #126](https://github.com/B00RR/ultimo-miglio/pull/126) (VFX #81: build riallineata alla guida visiva — vince la guida). Job `test` verde. VIA Concept Artist + Producer. Closes #81.
- 2026-10-16 11:55 — UI/UX — [faf9f88](https://github.com/B00RR/ultimo-miglio/commit/faf9f88) — PR #139 (Closes #138): `11_CAP1_COMPLETO` allineato all'HUD minimo (LN, giorno, riga obiettivo, joystick, pad; telefono → Gioco completo #64; combo "+N" 1,5 s; debito a fine giornata; SFX telefono segnati Gioco completo, da rivedere col Sound Designer). Seguito review: colori dei pad rimandati a GUIDA_VISIVA_CAP1 §2 e PR #123.
- 2026-10-16 11:53 — Code Reviewer — [faf9f88](https://github.com/B00RR/ultimo-miglio/commit/faf9f88c0912ad5994c7473289f92ba847321853) — unita in squash la [PR #139](https://github.com/B00RR/ultimo-miglio/pull/139) (Docs UI: 11_CAP1_COMPLETO allineato all'HUD minimo del Cap. 1). Solo `.md`. Closes #138.
- 2026-10-16 11:50 — Code Reviewer — [4caa545](https://github.com/B00RR/ultimo-miglio/commit/4caa5452fa083d9ecafb1c591a4257d8d979fbd5) — unita in squash la [PR #147](https://github.com/B00RR/ultimo-miglio/pull/147) (docs: sezione *Regole del computer*, repo docs archiviato). Solo `.md`; conflitto su STATO_PROGETTO.md risolto con merge di main nel branch.
- 2026-10-16 11:50 — Code Reviewer — [8a2268e](https://github.com/B00RR/ultimo-miglio/commit/8a2268e61628a9ca3e14b79914948723b96c894c) — unita in squash la [PR #145](https://github.com/B00RR/ultimo-miglio/pull/145) (regola 1 solo Unity sul box, `flock /tmp/unity-box.lock`). Solo `.md`.
- 2026-10-16 11:49 — Code Reviewer — [b5f6ba5](https://github.com/B00RR/ultimo-miglio/commit/b5f6ba5864023f012c1f592163100e7931789a76) — unita in squash la [PR #143](https://github.com/B00RR/ultimo-miglio/pull/143) (anteprime QA `tutorial_hud_77` con pad NONNO a 50 LN). Solo documentazione QA, nessun C#. Closes #142.
- 2026-10-16 11:50 — Producer — aggiornate *Regole del computer*: stop solo ai propri processi/subagenti (mai agli altri; stop agli altri solo DevOn/Producer); da PR #145 portati runner self-hosted spento, soglia load 16 e Unity anche in batchmode; corretto §10.1 (`docs/design/11_CAP1_COMPLETO.md`; `00_BIBBIA.md` ancora da migrare).
- 2026-10-16 11:48 — Regole del computer decise da DevOn su richiesta di Lorenzo (carico box 158/8 core); repo docs archiviato.
- 2026-10-16 11:50 — Tech Lead — regola §10.11 "1 solo Unity sul box" con `flock /tmp/unity-box.lock` e soglia di load 16, dopo il blocco del box (load 200) delle 11:45. Il runner self-hosted è spento per oggi.
- 2026-10-16 11:47 — Code Reviewer — [d039bd8](https://github.com/B00RR/ultimo-miglio/commit/d039bd8aaa9dfe52606eca05ccaae874dace65ea) — unita in squash la [PR #87](https://github.com/B00RR/ultimo-miglio/pull/87) (fix(#59): NONNO non va più in retromarcia + test PlayMode verso di marcia). Job `test` verde. Refs #59.
- 2026-10-16 11:29 — Code Reviewer — [2f2f61f](https://github.com/B00RR/ultimo-miglio/commit/2f2f61f2d5d4946d01c8b7f65fc192d715cc0e51) — unita in squash la [PR #85](https://github.com/B00RR/ultimo-miglio/pull/85) (clienti v03-uv: rider cappello limone, 3 toni pelle; Ada già su main via #93). Job `test` verde. Refs #2.
- 2026-10-16 11:29 — Code Reviewer — [8ed4422](https://github.com/B00RR/ultimo-miglio/commit/8ed4422972dd7c6d3a80a45769034ec6b6c3d270) — unita in squash la [PR #135](https://github.com/B00RR/ultimo-miglio/pull/135) (Zaino 8 = 3.000 LN; combo HUD solo +N). Job `test` verde. Closes #134.
- 2026-10-16 11:29 — Code Reviewer — [3a3864c](https://github.com/B00RR/ultimo-miglio/commit/3a3864c134075e713eef85346639d11b7b581e12) — unita in squash la [PR #113](https://github.com/B00RR/ultimo-miglio/pull/113) (testi HUD minimo cap. 1 / rinviati_cap2). Job `test` verde. Closes #105.
- 2026-10-16 11:17 — Code Reviewer — [ad8a98b](https://github.com/B00RR/ultimo-miglio/commit/ad8a98be83087673dcdede2ca108c4cf706d4a5d) — unita in squash la [PR #93](https://github.com/B00RR/ultimo-miglio/pull/93) (anim: Nonna Ada riggata — Idle/Walk/Talk, controller, prefab scala 0,88; Refs #118 / #2). Job `test` verde. #85 resta aperta per clienti v03 (togliere file Ada).
- 2026-10-16 11:15 — Code Reviewer — [a65de18](https://github.com/B00RR/ultimo-miglio/commit/a65de1873055ec905d4b54b02535940313ebf2b3) — unita in squash la [PR #124](https://github.com/B00RR/ultimo-miglio/pull/124) (fix(#65): angolo SO, Nina non compenetra più il lampione).
- 2026-10-16 11:15 — Code Reviewer — [965e36a](https://github.com/B00RR/ultimo-miglio/commit/965e36a72d586111106b7c6a4e76bbc9ad34cc08) — unita in squash la [PR #121](https://github.com/B00RR/ultimo-miglio/pull/121) (Specifica eventi di log dell'onboarding cap. 1 (playtest QA)).
- 2026-10-16 11:15 — Code Reviewer — [dc4d728](https://github.com/B00RR/ultimo-miglio/commit/dc4d72869f0239bde76e93ce31da4c1a9b680dc6) — unita in squash la [PR #120](https://github.com/B00RR/ultimo-miglio/pull/120) (QA: checklist Cap.1 aggiornata (C13/P07/Z02) + scheda osservazione playtest).
- 2026-10-16 11:15 — Code Reviewer — [fdf1675](https://github.com/B00RR/ultimo-miglio/commit/fdf16758c241be2885577189dd1e0082162e35ac) — unita in squash la [PR #98](https://github.com/B00RR/ultimo-miglio/pull/98) (Audio cap.1: passi, SFX onboarding, volumi Impostazioni salvati e applicati).
- 2026-10-16 11:14 — Code Reviewer — [ba91f85](https://github.com/B00RR/ultimo-miglio/commit/ba91f85a18807c4e6bc83e18626f8697cbe0fdc6) — unita in squash la [PR #80](https://github.com/B00RR/ultimo-miglio/pull/80) (Perf VFX: percorso shader economico _UM_LITE + niente varianti cascade (#79)).
- 2026-10-16 11:14 — Code Reviewer — [f92e407](https://github.com/B00RR/ultimo-miglio/commit/f92e4079be6277f874694d663e0cfc71e5902825) — unita in squash la [PR #78](https://github.com/B00RR/ultimo-miglio/pull/78) (UI #77/#5: tutorial HUD — fumetto Ada compatto, freccia sul prezzo, CTA terracotta (portrait 360–412)).
- 2026-10-16 11:14 — Code Reviewer — [e5e1a03](https://github.com/B00RR/ultimo-miglio/commit/e5e1a034babfcfcc6ac5b392dbbdd7435555355a) — unita in squash la [PR #76](https://github.com/B00RR/ultimo-miglio/pull/76) (VFX #4: marker tutorial a terra (anello pulsante + freccia guida)).
- 2026-10-16 11:04 — Code Reviewer — [75d9124](https://github.com/B00RR/ultimo-miglio/commit/75d912467506908729cf39f7d8ac12608088bcc6) — unita in squash la [PR #106](https://github.com/B00RR/ultimo-miglio/pull/106) (onboarding cap. 1 con HUD minimo, Closes #105): job `test` verde (editmode 36/36, playmode 12/12). Follow-up aperti: feedback tappe del debito (LiveOps) e SFX combo/squillo (Sound). CI di main in coda.
- 2026-10-16 10:49 — Code Reviewer — [cdfc53f](https://github.com/B00RR/ultimo-miglio/commit/cdfc53ff2152b4752a8ad5b488729ed824000e4a) — unita in squash la PR #119 (Ruoli e responsabilità, solo .md). Review delle PR aperte con commenti; #114 verde su main.
- 2026-10-16 10:46 — Producer — aggiunta la sezione **Ruoli e responsabilità**: nuovo **Code Reviewer** che unisce tutte le PR; il Tech Lead non fa più merge (aggiornato *Come lavoriamo* punto 5); Build & Release pubblica su Pages solo dopo il VIA del QA; nuove etichette `ruolo:design` e `ruolo:qa`. Assegnate #107 (P0 zone, tech), #103, #101 (tech), #111 (animator), #110, #71 (ui), #65, #70 (3d), #69 (vfx), #91 (design); nuova #118 (Nonna Ada).
- **2026-10-16 09:35 (Europe/Rome)** — Fix P0 #58 #59 #60 (ramo fix-p0-lorenzo, [a87edf0](https://github.com/B00RR/ultimo-miglio/commit/a87edf0)): BoxCollider su muri/archi/scaffali/bancone/props, Nina clamp x −8.6..7.0, NONNO yaw 0° + sweep AgentMove, spawn sicuro. EditMode 35/35, PlayMode 11/11. LIVE P0 su 8780: `qa/playtest_web_live_p0.md`.

- 2026-10-16 08:55 — Producer — **triage Cap. 1** (QA v8 baseline NO-VIA). Aggiornate #58 muri/scaffali (tech+3d, P0), #59 NONNO all'indietro (tech+animator, P0), #60 spawn Nina (tech): fix in PR #63 da unire. Nuove: #64 TAB telefono S04 (ui), #65 lampione/staccionata SO C03/C04 (3d), #66 LIVE completo checklist + checklist su main (QA/build, P0). #61 testi Ada (ui), #62 duplicato di #61. Quadro: Tech #58 #59 #60 #6 · 3D #58 #65 #2 (#2 ferma dal 15/10) · Animator #59 #2 (ferma) · VFX nessuna issue Cap. 1 · UI #64 #61 #5 (#5 ferma dal 15/10) · Build #66 (+#12/PR #14 ferma) · QA #66. Rischi: milestone scaduta 15/10 18:00; `qa/CHECKLIST_CAP1.md` non è su main; molte voci non verificate (P01–P06, Z01, R02, touch).

- 2026-10-16 04:11 — [7dd1767](https://github.com/B00RR/ultimo-miglio/commit/7dd1767) — merge [PR #55](https://github.com/B00RR/ultimo-miglio/pull/55): deposito #47 + luce/camera #48; Concept VIA; **QA live v5 VIA**.
- 2026-10-16 03:18 — [1b5b7be](https://github.com/B00RR/ultimo-miglio/commit/1b5b7be) — merge [PR #45](https://github.com/B00RR/ultimo-miglio/pull/45): fix QA v1–v3 (#41–#44, #49); **QA live v4 VIA**.
- 2026-10-16 01:51 — [ba25f62](https://github.com/B00RR/ultimo-miglio/commit/ba25f62) — merge [PR #51](https://github.com/B00RR/ultimo-miglio/pull/51): guida visiva — tavola Nonna Ada v01 e decisione ombre/saturazione.
- 2026-10-16 01:49 — [29534d3](https://github.com/B00RR/ultimo-miglio/commit/29534d3) — merge [PR #50](https://github.com/B00RR/ultimo-miglio/pull/50): guida visiva capitolo 1 + confronto build (Concept Artist).

- 2026-10-16 03:05 — ramo `night-art` — Tech Lead: merge #52 (vfx/48-luce) + #47 (art/47-deposito) + fix NONNO timeout; build WebGL_next :8778.
- 2026-10-16 04:00 — branch art/47-deposito — 3D Artist: deposito fedele alla guida (#47): lastre 0,25 m, archi in tufo, parapetti in tufo e legno, insegna col faro, banco in noce con Carico 0 emissivo, kit premium, DressGameplay, Borgo Gradoni su gradoni.
- 2026-10-16 02:15 — [6081d67](https://github.com/B00RR/ultimo-miglio/commit/6081d67) — merge #49 LegnoCartone/Ada portrait in fix-qa; EditMode 35/35 PlayMode 6/6; Closes #49 sulla PR #45.
- 2026-10-16 02:10 — [0f01a85](https://github.com/B00RR/ultimo-miglio/commit/0f01a85) — Tech Lead (PR #45 / fix-qa): Bancone→LN immediato, strip NavMesh/Hidden shader; Closes #41–#44.
- 2026-10-16 01:55 — ramo `ui/49-hud` — UI: #49 superfici legno/cartone, ritratto Ada; Unity non avviato.
- 2026-10-15 16:35 — PR producer/regola-closes — Producer: regola `Closes #N` / `Refs #N` in *Come lavoriamo*, modello di PR `.github/pull_request_template.md`, corretti gli orari di due voci; aggiunte le parole chiave alle PR #11 e #14.
- 2026-10-15 16:25 — [8e4e272](https://github.com/B00RR/ultimo-miglio/commit/8e4e272) — Producer: aggiornata la Cronologia.
- 2026-10-15 16:26 — Producer: create 13 issue della milestone *Gioco completo* divise per ruolo (#13, #15–#26), etichetta `ruolo:build` creata; #7 e #12 assegnate a *Gioco completo*; commento di piano su #7.
- 2026-10-15 16:24 — Producer: create le milestone [Capitolo 1 - 15/10 18:00](https://github.com/B00RR/ultimo-miglio/milestone/1) (#1–#6) e [Gioco completo](https://github.com/B00RR/ultimo-miglio/milestone/2).
- 2026-10-15 16:21 — [PR #11](https://github.com/B00RR/ultimo-miglio/pull/11) (ramo animator → main) aperta: **in conflitto** su `chr_nina_vela.fbx` e `bot_nonno.fbx`; merge a cura del Tech Lead.
- 2026-10-15 16:21 — [c54f667](https://github.com/B00RR/ultimo-miglio/commit/c54f667) — main: merge PR #10, sezione Cronologia.
- 2026-10-15 16:20 — [643f1f9](https://github.com/B00RR/ultimo-miglio/commit/643f1f9) — main: merge PR #8, aggiunto STATO_PROGETTO.md (PR #9 chiusa come duplicato).
- 2026-10-15 16:25 — [#8](https://github.com/B00RR/ultimo-miglio/pull/8) + #9 — Aggiunto STATO_PROGETTO.md; create etichette e issue #1–#7.
- 2026-10-15 16:08 — [0c05198](https://github.com/B00RR/ultimo-miglio/commit/0c051984aef7a75dafc24616fa890cae5cc102b3) — ramo animator: Clienti v02: rig Humanoid di Nina riusato sui 6 clienti, pesi accessori rigidi, clip Idle/Walk/Wait_Impatient/Pay/Happy/Angry e controller condiviso chr_cliente
- 2026-10-15 16:07 — [b03a3ad](https://github.com/B00RR/ultimo-miglio/commit/b03a3adc9324209f1ec95ac87aa57a0a0939cfbb) — ramo animator: Nina: Carry mani sotto la pila al petto, giacca Cheer senza schegge, soglie Speed in m/s
- 2026-10-15 16:02 — [b59b95e](https://github.com/B00RR/ultimo-miglio/commit/b59b95e7e8e4c57240186b6ab141c9b3f3dd36e7) — ramo animator: AnimWalkVideo: la mesh skinnata ora si deforma nel video di verifica
- 2026-10-15 15:59 — [3a2d5a9](https://github.com/B00RR/ultimo-miglio/commit/3a2d5a919ba65d71651c240212ba7b76c1ae7290) — ramo animator: NONNO v03: bind dalla mesh v03 (baffi ristretti/avanti, montante abbassato a 1.32 e avanzato, ruote allargate)
- 2026-10-15 15:57 — [614ed5d](https://github.com/B00RR/ultimo-miglio/commit/614ed5d14ec6bb9dc5adbb73a90454fe710cbca6) — main: webgl-min: VFX v1.6 (pickup/deliver/upgrade fx, contact shadow), materiali kit su atlas (fix bianco), schede nascoste fuori schermo, margine freccia; test PlayMode 4/4
- 2026-10-15 15:51 — [8ce7e18](https://github.com/B00RR/ultimo-miglio/commit/8ce7e18ed1037e8e05084990c678add07fa5e025) — ramo animator: Nina: passo nel verso giusto (+Z) e Carry con mani sotto la pila
- 2026-10-15 15:39 — [1f1d6ca](https://github.com/B00RR/ultimo-miglio/commit/1f1d6ca2ffa6a429a383c513c02bd3e04eab9bd7) — main: webgl-min: onboarding Nonna Ada (7 passi + chiusura, marker a terra, freccia bordo, PlayerPrefs), pad ASSUMI NONNO, pacco prop_parcel_box; Ch1LoopTest copre il tutorial (PASS)
- 2026-10-15 15:33 — [3b78348](https://github.com/B00RR/ultimo-miglio/commit/3b78348e7faee029d0b880a782424fd7490f2716) — main: webgl-min: confini 4 lati, kit 3D deposito, clamp schede, game feel (pila a molla, pacchi ad arco, monete verso HUD, pad pulse), SFX wt/sound dopo primo input; test loop+confini PASS
- 2026-10-15 15:25 — [18d4abe](https://github.com/B00RR/ultimo-miglio/commit/18d4abe5e051f102f4781e27c8779c10f9949ed9) — ramo animator: anim v05 Nina: giacca staccata dalle braccia (Cheer stirava i pannelli laterali)
- 2026-10-15 15:19 — [13cca59](https://github.com/B00RR/ultimo-miglio/commit/13cca59b17e69522405c203e0f7b08959a49cb22) — main: webgl-min: HUD responsive (wt/ui), single keyboard input via FloatingJoystick, full-window web template, palette update, NONNO v02, loop test PASS
- 2026-10-15 15:13 — [6b91c0e](https://github.com/B00RR/ultimo-miglio/commit/6b91c0e2404228828d6bea1e4945274d33070953) — ramo animator: anim v04: NONNO fork cap 0.40m; Nina v02 mesh + ponytail_01/02 bones w/ baked secondary motion; jacket-hem flap skin; anti-penetration pass; same GUIDs/clip IDs
- 2026-10-15 15:08 — [a93fd71](https://github.com/B00RR/ultimo-miglio/commit/a93fd710ade8d4adfd6580718dd8da3df60db8ee) — ramo animator: Characters: anti-compenetrazione (audit BVH per frame): NONNO baffi/montante/forche/testa, TICO braccia Happy, Nina braccia-testa/torso; stessi GUID/clip ID
- 2026-10-15 15:02 — [77d5c30](https://github.com/B00RR/ultimo-miglio/commit/77d5c301b43e19a41a586b28fe07422e4bf0dc59) — main: webgl-min: Nina facing fix, camera ortho 4.5, single Zaino pad, HUD panels 1920x1080 match .5, phone TAB/ESC, LoopAutoTest + PlayMode Ch1 loop test (PASS)
- 2026-10-15 15:00 — [7dfcd5d](https://github.com/B00RR/ultimo-miglio/commit/7dfcd5d3eced26b4ebd80cbf5ea5973e65539d5a) — ramo animator: Characters: NONNO v02 (muletto, rig Generic 8 ossa dai vertex group, Idle/Drive/Lift/Drop/Happy, stessi GUID/clip ID) + TICO v02 (rig 8 ossa, Idle/Move/Stamp/Happy, nuovo controller + meta GUID fissi); AnimVerify esteso
- 2026-10-15 14:51 — [3d43efb](https://github.com/B00RR/ultimo-miglio/commit/3d43efbac7071f6b3ac76c31f19550f1ead67b3c) — main: webgl-min: fix magenta (URP Forward, no postFX, always-included shaders), VFX v1.3, 3D kit+palette, final Nina, input handler Both, fps -1 on WebGL
- 2026-10-15 14:33 — [db1e86b](https://github.com/B00RR/ultimo-miglio/commit/db1e86bb3e2e2ea48536f591400290bda7e35ace) — main: webgl-min: wip
- 2026-10-15 14:30 — [b11e16c](https://github.com/B00RR/ultimo-miglio/commit/b11e16ca9bda78b7a99cf6ca5e435759afd04f3c) — ramo animator: Characters: Nina skin finale (giunture braccia misurate sulla mesh, re-bake a matrici, take a 30 fps), mesh finale con UV T_Palette_Atlas
- 2026-10-15 14:16 — [49855ed](https://github.com/B00RR/ultimo-miglio/commit/49855ed4e187e074772c2dd7d9b59a2462f6b7f0) — main: webgl-min: WebGL build entry (EnsureUrp, scene rebuild), particlesystem module, drop vibration/unity-mcp/recorder, Cinemachine lens fix, QA patches 03-10
- 2026-10-15 14:00 — [01f0b9c](https://github.com/B00RR/ultimo-miglio/commit/01f0b9c7c5a6670b1d2336d85704f6e9eeb841ae) — ramo animator: Characters: Nina re-bind su re-export 13:55; NONNO riportato a v01 blockout (la mesh menta e' il bot del Bancone, non NONNO)
- 2026-10-15 13:52 — [abcfcd7](https://github.com/B00RR/ultimo-miglio/commit/abcfcd76eb698cb86cfec6b6f28e317c7d783458) — ramo animator: Characters: re-bind su re-export 13:50 delle mesh (UV T_Palette_Atlas), stessi GUID/clip
- 2026-10-15 13:49 — [3b71509](https://github.com/B00RR/ultimo-miglio/commit/3b715095d9ffbbe5efcc31cec3eabc5810b16c78) — ramo animator: Characters: Nina rig Humanoid su mesh finale (24 ossa, skin max 4) + 7 clip re-bake; NONNO 10 ossa su mesh ricentrata + 5 clip; AnimVerify editor (GUID/meta/controller invariati)
- 2026-10-15 13:04 — [c3b588a](https://github.com/B00RR/ultimo-miglio/commit/c3b588a4fb23debea683be0008c700ce1265f8c3) — main: HUD integration: core events (OnLnCredited/OnMoraChanged/DayChanged/BulloniChanged)+tests, bridge wiring, HUD.prefab hook, Inter+Roboto OFL fonts, ci hud step
- 2026-10-15 12:55 — [a7cba95](https://github.com/B00RR/ultimo-miglio/commit/a7cba953cc88b93db02efafa66734e26b94a090a) — main: keep empty folders
- 2026-10-15 12:55 — [8e08315](https://github.com/B00RR/ultimo-miglio/commit/8e08315b5b3178ce41972f37c02d973b39e67ecd) — main: Unity 6 project skeleton + ch1 vertical slice code (EconomyCore+tests, gameplay, audio, data import, build/capture scripts, Unity-MCP plugin)

- **VFX v2.0** (15/10 16:30, ramo `vfx-v2`): shader toon premium (ombre colorate, AO, cielo a gradiente, grading in-shader con post OFF su WebGL), effetto acquisto `Fx.Purchase`, consegna più grande. Materiali/prefab da generare con *Ultimo Miglio/VFX/Setup Scene*. Non ancora compilato (compila il Tech Lead nella build).

## Aggiornamento 17:45 — Impostazioni + schede pad (UI, issue #35, branch `ui/impostazioni-e-schede-pad`)
- **Impostazioni** (ingranaggio accanto al telefono): zoom −/+ con anteprima live, slider Musica/Suoni, Salva / Indietro.
  - PlayerPrefs: `cam_zoom_v3`, `vol_music`, `vol_sfx` (0–1).
  - Chiama per reflection (UI non referenzia Gameplay): `DepotCameraFit.SetOrthoSize` / `GetOrthoSize`, `AudioManager.SetMusicVolume` / `SetSfxVolume` — **Tech Lead crea i metodi e collega in scena**.
  - File: `UI/Runtime/SettingsController.cs`, `UI/UXML/Settings.uxml` (+ overlay in `Hud.uxml`), `UI/USS/Settings.uss`. Aggiungere `SettingsController` sul prefab HUD (stesso UIDocument).
- **Schede pad** (`UpgradePadCard`): nascoste di default; `ShowOnPad(bool)` / `SetVisible(bool)` — una sola attiva (`ActiveCard`). Tech Lead: `ProximityZone` enter/exit → `ShowOnPad`. Card max 200 dp, contrasto ink su paper, Fredoka.
- Branch da **main** (non da `ui/pad-leggibili` #34). Colori pad lasciati al merge Tech Lead.
- **Non compilato in Unity**. Verifica statica brace-check; compile/test a cura Tech Lead.

## Aggiornamento 17:10 — HUD v2 verticale (UI, issue #30, branch ui/hud-v2-portrait)
- HUD in unità dp (ResponsivePanel: portrait min(W/411,H/914) → 2,63 a 1080x2400; landscape H/600), solo documento HUD.
- Nuovi `Assets/_Game/UI/Tokens.uss` + `USS/HudV2.uss`: pillola valute, chip Pedaggio, telefono 3D, etichetta stanza (no ellissi, a capo >22 car.), card pad 128–200 dp, biglietto Ada, pulsanti chunky.
- Biglietto di Nonna Ada in AdaNoteView (coda, 1 alla volta, 120 dp sotto top sicuro, mai metà bassa); il banner di TutorialManager viene rispecchiato e nascosto (Gameplay non toccato).
- Etichette pad clampate nella safe area (margine 8 dp) con flip vicino ai bordi.
- Font Fredoka (SIL OFL 1.1, Google Fonts) in `UI/Fonts` — licenza in `LICENSE_Fredoka_OFL.txt`; da registrare in LICENZE_ASSET.md.
- **Non compilato in Unity** (solo check dotnet sui DLL UnityEngine). Da verificare dal Tech Lead.

## Aggiornamento 17:35 — Pad leggibili (UI, issue #33, branch ui/pad-leggibili)
- Nuovo `PadTag
- Icone pad bianche 128px per VFX (`ic_pad_*_white.png`): pickup/buy/deliver/upgrade (Lucide ISC + coin CC0).` (UI/Runtime/World) + `UXML/PadTag.uxml` + `USS/PadTag.uss`: cartellino sopra ogni pad con icona, etichetta corta (max 12 car.) e prezzo grande sui pad di acquisto (26 dp vs 16 dp).
- Enum `PadKind` da Inspector (Pacco/Moneta/Cliente), default dedotto in Reset (UpgradePad/CassaZone → Moneta, nome "banc/client/consegn" → Cliente, altrimenti Pacco).
- Colori: pacco teal #5E7F86/#3E5A61, moneta sun #F4C430 + ink, cliente terracotta #C8633B/#8E3F22; ZoneFillRing ricolorato con lo stesso colore. Font Fredoka. Icone già presenti (ic_box, ic_coin, ic_customer, Lucide ISC): nessuna nuova risorsa.
- **Non compilato in Unity** (dotnet non disponibile sulla macchina dell'agente). Da agganciare ai pad in scena dal Tech Lead.

## Aggiornamento 18:15 — HUD minimale (UI, issue #38, branch ui/hud-minimale)
- In vista: **LN + giorno**, obiettivo tutorial 1 riga, Ada **1 riga ≤40 dp**, joystick, telefono/settings **≤40 dp**.
- Nascosti: room label, chip Pedaggio, badge, bulloni; PadTag solo sul pad; pad/cassa card ≤140 dp.
- File: `Hud.uxml` (`hud--minimal`, `day-value`, `tutorial-objective`), `HudV2.uss`, `Tokens.uss`, `PadTag.uss`, `CassaCard.uxml`; binding in `HudController`/`HudGameBridge`/`AdaNoteView`/`PadTag`.
- **Tech Lead @1080×2400:** LN+G leggibili, icone non bloccano, Ada non invade, una scheda pad alla volta.
- **Conflitti attesi con PR #36** (ancora aperta): `Hud.uxml`, `HudV2.uss`, `STATO_PROGETTO.md`. Partiti da `main`.

## Aggiornamento 16/10 04:00 — Deposito fedele alla guida (3D Artist, issue #47, branch art/47-deposito)
- **D1 pavimento:** `env_floor_2x2_a/b` rifatti con lastre da 0,25 m (8x8 per modulo), crema/tufo con lieve variazione e smusso sulle fughe (652 tri a modulo).
- **D2 muri/insegna/vasi:** archi in tufo `env_wall_arch_4m` aperti sul muro N (x ±5) e sul muro E; muretti in mattoni sostituiti da `env_plinth_edge_2m` v03 (parapetto basso in tufo + corrimano in noce, 548 tri); `prop_sign_vela_figli` v03 col **faro** (lanterna emissiva); 4 vasi di limoni dentro il deposito più altri vicino a scaffali e Bancone.
- **D3 Bancone:** `prop_counter_2m` v03 in noce con pannelli e **cucitura ambra emissiva** (slot `M_Diorama_Atlas_Emissive`); **Carico 0** (emissivo) ora sul banco.
- Kit premium portato su main: muri con zoccolo e cornice, scaffale carico `prop_shelf_2m_loaded`, 4 pacchi (`prop_parcel_box*`), pila, vasi, fioriera, lampione, insegne, tappeto.
- `DepotKit.DressGameplay()` (set dressing vicino ad Arrivi/scaffali/Bancone) chiamato da `SliceSceneBuilder`. Tutto null-safe: se manca un FBX resta il comportamento di prima.
- **Borgo Gradoni:** case di Solara alternate su gradone in tufo con scalinate.
- Layout di riferimento: `Art/Environment/Layouts/bancone_layout.json` (+ README conversione). Licenze 3D: `docs/LICENZE_ASSET.md`.
- Nina su main: FBX dell'Animator (1.968 facce, UV raccolte nelle celle dell'atlas), **nessuna striatura nel file**. Non toccata.
- **Non compilato né testato in Unity** (Unity occupato): servono import, test EditMode/PlayMode e una cattura della scena rigenerata.

## Aggiornamento 16/10 09:30 — PR #36 riallineata a main (UI)
- Merge di `origin/main` (HUD minimale #38, fix collisioni #63) in `ui/impostazioni-e-schede-pad`, senza rebase/force-push.
- Conflitti risolti: `Hud.uxml`, `HudV2.uss`, `HudController.cs`, `UpgradePadCard.cs`, `WorldAnchoredElement.cs` → tenuta la versione main (niente telefono/card grandi), aggiunti `Settings.uss` nell'HUD e campo `settingsButton` (eventi `OnSettingsRequested` + `SettingsButtonTapped`).
- Catture anteprima 390x844 (HTML/Playwright, non Unity): `docs/ui/catture/pr36/`.
- Deciso (Tech Lead): pannello unico `SettingsController`; `Bridge/SettingsPanel` resta (usato da `HudGameBridge`) ma senza listener `OnSettingsRequested`. Ingranaggio = toggle (QA P3, `qa/playtest_web_live_p0.md` S01). Zoom su chiave unica `cam_zoom_v3` (`DepotCameraFit.PrefZoom`).
- Riferimento #35 (chiusa). Correlate aperte: #64, #67.
- 2026-10-16 — PR #85 (3D Artist, Refs #2): solo clienti v03-uv in `3d/export/chr/` (rider cappello limone, 3 toni pelle; residente/rider/turista/vip, vertici invariati) + review docs/review/2. Nonna Ada tolta: la consegna l'Animator (#93).

## Aggiornamento 16/10 10:55 — Zone a terra senza blu/grigi (VFX, issue #69, branch `vfx/69-zone-calde`)
- Dischi POI (`InteractionPoint`): Prendi terracotta `#C8633B` (era teal `#5E7F86`), Consegna menta `#8FC9A8`, Acquisto e upgrade (dischi, anelli, FX) giallo sole `#F4C430` (limone `#F7E26B` solo nelle scintille core e nella scia della moneta), bordo `#3B2F2A`. Schiarimenti e beacon verso crema `#F3E3C3` invece che verso il bianco (niente pali grigio-azzurri).
- Nascosto il quadrato giallo `UI/ZoneRing` sotto i dischi (sembrava una griglia di debug); la logica del Fill resta.
- Anello pad "Pickup" e FX pacco: da teal a terracotta.
- Da fare: cattura 390×844 in build (CI); i pali grigi dei lampioni, se sono mesh 3D, passano al 3D Artist.
