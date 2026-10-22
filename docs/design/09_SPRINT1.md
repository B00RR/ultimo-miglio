> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# SPRINT 1 - Vertical slice Capitolo 1 (game director: DevOn)
Ordine di Lorenzo: costruire una parte giocabile e MIGLIORARE NETTAMENTE grafica e animazioni. Engine: Unity 6 + URP. Qualita prima di tutto.
## Obiettivo
Capitolo 1 giocabile su Android: deposito Sunn Depot (Arrivi, scaffali, Bancone, Cassa, pad upgrade), Kaia col joystick, pila di pacchi, clienti, monete, upgrade Zaino, OLDO che automatizza, economia da 01_CORE_LOOP v3.x. Ciclo giorno base.
## Owner
- Tech Lead: progetto Unity in /workspace/ultimo-miglio/unity/ (git), Unity Linux headless sul box (batchmode), architettura, gameplay, build APK/AAB, screenshot/video di gioco da batchmode. Integra tutti gli asset.
- 3D Artist: kit modulare del deposito, Kaia Rio e OLDO in qualita finale low-poly, fedeli a conceptA.jpg. FBX in 3d/export/.
- Animator: rig + animazioni di Kaia (idle, walk, run, carry, deliver, cheer) e OLDO, con molta personalita. FBX in anim/export/.
- VFX & Technical Artist: shader toon URP, luci, post-process, particelle e game feel.
- UI/UX: HUD in Unity (UI Toolkit o uGUI) da 04_UI v3.
- Sound Designer: primi SFX e loop del giorno (anche se generati/placeholder di buona qualita, licenze pulite).
- Game Designer: tabelle numeriche cap.1 in formato dati (JSON/ScriptableObject).
- QA: test su build, performance, checklist.
- Concept Artist: supervisione stile su ogni render/screenshot.
## Regole
Comunicate direttamente tra voi. A DevOn solo: milestone, blocchi, screenshot/video. Licenze pulite per ogni asset. Nessun acquisto senza OK di Lorenzo.

## CRITERI DI ACCETTAZIONE (approvati da DevOn)
Lo Sprint 1 è chiuso quando:
1. 30 fps stabili su un Android di fascia media (riferimento Galaxy A54 o simile), misurati con la development build e il profiler.
2. APK sotto 150 MB.
3. Cap. 1 completabile da inizio a fine senza blocchi (verificato dal QA con il log UMQA).
4. Test EditMode e PlayMode tutti verdi.
5. Stile approvato dal Concept Artist sugli screenshot e sul video renderizzati in batchmode.
Lo sviluppo avviene interamente sul box (Unity headless).

## SKILL DA USARE (ordine di Lorenzo: obbligatorio)
Le skill stanno in /home/box/agent-data/workflows/<nome>/SKILL.md. Prima di ogni attività, leggi quelle del tuo ruolo e seguile.
- Tech Lead: unity-initial-setup (installa Unity-MCP plugin + CLI nel progetto), package-add/list/search/remove, script-update-or-create/read/delete/execute, unity-rql, rql-query/mutate/schema, unity-tool-list, unity-skill-create/generate, tool-set-enabled-state, ping, tests-run, screenshot-game-view/camera/scene-view/isolated, codebase-onboarding, mcp-builder.
- VFX & Technical Artist: screenshot-camera/isolated/game-view, script-update-or-create, rql-mutate, script-execute, package-add (URP/VFX).
- 3D Artist, Animator: screenshot-isolated (verifica in Unity), algorithmic-art (studi di forma), slack-gif-creator (GIF di anteprima delle animazioni).
- Concept Artist: canvas-design, theme-factory, brand-guidelines (solo come metodo, palette Solara), screenshot-game-view per le review.
- UI/UX: ui-design-system, frontend-design, ux-researcher-designer, onboarding-cro, screenshot-game-view.
- Game Designer: product-manager-toolkit, agile-product-owner, experiment-designer, product-discovery.
- LiveOps & Monetizzazione: paywall-upgrade-cro, pricing-strategy, churn-prevention, referral-program, ab-test-setup, analytics-tracking, product-analytics, app-store-optimization, launch-strategy, competitive-teardown.
- QA: tests-run, senior-qa (adattalo a Unity), webapp-testing, screenshot-game-view.
- Narrative, Sound: doc-coauthoring, internal-comms.
- Tutti: skill-creator e unity-skill-create per creare nuove skill quando mancano.

## RISORSE GRATUITE (ordine di Lorenzo, vale per TUTTI i ruoli)
Usate risorse gratuite per velocizzare lo sviluppo e, dove possibile, modificatele per adattarle al nostro gioco (stile Diorama Solare, palette di Solara, tono).
- Licenze ammesse: solo quelle che consentono uso commerciale e modifica. Preferite CC0 (Kenney, Quaternius, Poly Pizza CC0, ambientCG, OpenGameArt CC0, Freesound CC0, Mixamo per le animazioni, Google Fonts OFL, icone MIT/CC0, plugin e package MIT/Apache). CC-BY si può usare solo con attribuzione. Escluse: NC, ND, licenze ambigue e materiale preso da altri giochi.
- Gli elementi identitari (Kaia, OLDO, robot principali, logo, tema musicale) restano originali.
- Ogni risorsa va registrata in /workspace/ultimo-miglio/LICENZE_ASSET.md con fonte, URL, licenza e modifiche.
- Esempi per ruolo: 3D (kit, props) · Animator (Mixamo e mocap CC0 come base da ritoccare) · VFX (texture e shader MIT/CC0) · UI/UX (font OFL, icone CC0) · Sound (SFX CC0, poi rielaborati) · Tech Lead (package open source MIT/Apache).

## LINGUA (ordine di Lorenzo)
Tutti parlano e scrivono SOLO in italiano: messaggi, report, file di stato, documenti, LEGGIMI. Codice e nomi tecnici possono restare come sono.
