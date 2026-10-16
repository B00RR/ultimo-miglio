# STATO PROGETTO — Ultimo Miglio

> Punto di ingresso unico per chiunque (persona o agente) lavori su questo repo. Aggiornalo **nella stessa PR** in cui completi qualcosa.
> Ultimo aggiornamento: 2026-10-16 04:11 (Europe/Rome).

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

## 5. IN CORSO (oggi 16/10)
- **main @ 7dd1767** — QA live **v5 VIA** (PR #55: #47 deposito + #48 luce/camera). Build QA su `build/WebGL` :8777; art night su `build/WebGL_next` :8778 (non toccare).
- P2 aperti da QA v5: camera follow (Nina esce/coperta), pillola prezzo LN sotto riga obiettivo.
- **Merge del ramo `animator`** (fix camminata): conflitto su `chr_nina_vela.fbx` e `bot_nonno.fbx`.
- I **clienti sono ancora capsule** → sostituire con i personaggi del ramo animator.
- Rifinitura tutorial Nonna Ada e game feel; resto cap. 1.

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
3. Si apre una **Pull Request verso `main`** che cita la issue (`Closes #N` o `Refs #N`, vedi punto 10).
4. **Merge solo con test verdi** (EditMode + PlayMode) e build WebGL che compila.
5. Il **Tech Lead** fa review e merge (squash).
6. Quando qualcosa è completato, **aggiorna questo file nella stessa PR**.
7. **Messaggi di commit in italiano**, brevi e descrittivi.
8. **Ogni voce/modifica di questo file ha data e ora** (Europe/Rome, formato `AAAA-MM-GG HH:MM`) e, se pushata, il **link al commit** (hash corto). Aggiungi la voce in cima alla sezione *Cronologia*.
9. Non lavorare nel working tree di un altro agente: usa un clone tuo.
10. **Parole chiave nelle PR:** ogni PR che risolve una issue scrive nella descrizione `Closes #N`, **una riga per issue**: così GitHub chiude la issue al merge su `main`. Se la issue è risolta solo in parte si scrive `Refs #N` e la issue resta aperta. Le forme in italiano (es. "Chiude #N") **non** sono riconosciute da GitHub. Usa il modello `.github/pull_request_template.md`.

## 10. Da dove iniziare (nuovo agente)
1. Leggi questo file, poi `00_BIBBIA.md` e `11_CAP1_COMPLETO.md` nel repo docs.
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
