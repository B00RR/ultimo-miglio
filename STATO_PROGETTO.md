# STATO PROGETTO — Ultimo Miglio

> Punto di ingresso unico per chiunque (persona o agente) lavori su questo repo. Aggiornalo **nella stessa PR** in cui completi qualcosa.
> Ultimo aggiornamento: 15/10/2026 16:30 (ora di Roma).

## 1. Il gioco
- **Ultimo Miglio**: gioco mobile **Android**, **Unity 6 (6000.3) URP**.
- Controllo diretto con **joystick** (stile *My Little Universe*): Nina corre, raccoglie pacchi, riempie scaffali, serve i clienti al bancone, incassa e sblocca/potenzia.
- Ambientazione: città immaginaria di **Solara**. Stile visivo **"Diorama Solare"** (low‑poly caldo, palette su atlas `T_Palette_Atlas`).
- Personaggi: **Nina** (protagonista), **il Nonno** (Nonna Ada guida l'onboarding del cap. 1), **NONNO** (robot muletto assumibile), TICO (robot timbratore), clienti.

## 2. Vincoli fissi di Lorenzo (non negoziabili)
1. Tutto in **italiano** (testi di gioco, documenti, commit, issue, PR).
2. Lorenzo testa **solo tramite build WebGL nel browser** (zip/preview). **Niente video** a lui.
3. Asset **gratuiti CC0 / licenza commerciale**, sempre **adattati** allo stile (registro in `ultimo-miglio-docs/LICENZE_ASSET.md`).
4. **Originali**: Nina, Nonno, robot, logo e tema non devono copiare altri giochi.
5. **Profondità gestionale**: energia/corrente, contratti, rotte, officina, OmniFreight (rivale).
6. **Longevità lunga**: progressione a capitoli + post‑game.

## 3. Team
- **Direttore**: DevOn (coordina, decide priorità).
- **Attivi (5)**: Tech Lead, 3D Artist, Animator, VFX & Technical Artist, UI/UX.
- **In pausa**: Game Designer, QA, Sound Designer, Narrative, LiveOps, Concept Artist (i loro documenti restano validi).

## 4. FATTO
- Scheletro progetto Unity 6 + vertical slice cap. 1 (EconomyCore con test EditMode, gameplay, audio, import dati, script build/cattura).
- HUD integrato agli eventi core (LN, Mora, giorno, bulloni), font Inter/Roboto (OFL), HUD responsive (1920x1080 / 1366x768 / 960x600).
- Ramo/build **WebGL** (`webgl-min`, ora su `main`): fix magenta (URP Forward, niente post‑process, shader sempre inclusi), template web a finestra intera, input tastiera unico via FloatingJoystick, camera ortografica 4.5.
- Ciclo cap. 1 completo: pacchi → scaffali → bancone → monete → acquisto Zaino; confini sui 4 lati; kit 3D deposito; materiali su atlas.
- Game feel: pila a molla, pacchi ad arco, monete verso HUD, pad che pulsano, SFX dopo il primo input; VFX v1.6 (pickup/consegna/upgrade, contact shadow).
- **Onboarding Nonna Ada**: 7 passi + chiusura, marker a terra, freccia a bordo schermo, salvataggio PlayerPrefs; pad ASSUMI NONNO.
- Test PlayMode **4/4 SUPERATI** (ciclo, 9 stati tutorial in ordine, confini).
- Ramo `animator` (non ancora su main): Nina rig Humanoid 24 ossa + clip (camminata nel verso giusto, Carry, Cheer), NONNO v03 (rig Generic), TICO v02, 6 clienti con rig di Nina e controller `chr_cliente`, verifica anti‑compenetrazione.
- Documentazione di design: bibbia, core loop, tech, UI, retention, sprint 1, gestionale, longevità, cap. 1 completo, protocollo Unity.

## 5. IN CORSO (oggi 15/10)
- **Consegna cap. 1 giocabile in WebGL entro le 17:45–18:00.**
- Rifinitura tutorial Nonna Ada (7 passi) e game feel.
- **Merge del ramo `animator`** (fix camminata): conflitto su `chr_nina_vela.fbx` e `bot_nonno.fbx`.
- I **clienti sono ancora capsule** → sostituire con i personaggi del ramo animator.
- **Costo NONNO troppo alto** → da abbassare.
- Controllo sovrapposizioni HUD.

## 6. DA FARE
- Resto del gioco in **3–4 giorni**: capitoli 2+ con **un sistema gestionale per capitolo** (energia/corrente, contratti, rotte, officina, OmniFreight).
- **Post‑game** e sistemi di longevità (vedi `11_LONGEVITA.md`).
- Build Android (APK/AAB) firmata quando il gameplay è stabile.

## 7. Dove trovare la documentazione
Repo **[B00RR/ultimo-miglio-docs](https://github.com/B00RR/ultimo-miglio-docs)**:
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
1. **Ogni attività = una issue GitHub**, con etichette di ruolo (`ruolo:tech`, `ruolo:3d`, `ruolo:animator`, `ruolo:vfx`, `ruolo:ui`), priorità (`priorita:alta`) e ambito (`cap1`, `bug`).
2. Si lavora su un **ramo per issue/ruolo** (es. `animator`, `ui/hud-sovrapposizioni-12`).
3. Si apre una **Pull Request verso `main`** che cita la issue (`Chiude #N`).
4. **Merge solo con test verdi** (EditMode + PlayMode) e build WebGL che compila.
5. Il **Tech Lead** fa review e merge (squash).
6. Quando qualcosa è completato, **aggiorna questo file nella stessa PR**.
7. **Messaggi di commit in italiano**, brevi e descrittivi.
8. Non lavorare nel working tree di un altro agente: usa un clone tuo.

## 10. Da dove iniziare (nuovo agente)
1. Leggi questo file, poi `00_BIBBIA.md` e `11_CAP1_COMPLETO.md` nel repo docs.
2. Guarda le issue aperte: `gh issue list -R B00RR/ultimo-miglio` e scegli quelle del tuo ruolo con `priorita:alta`.
3. Clona in una cartella tua, crea il ramo, lavora, lancia i test, apri la PR.
4. Rispetta i vincoli della sezione 2 (italiano, WebGL, niente video, asset liberi adattati).
