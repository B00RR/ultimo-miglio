# Indice della documentazione — Bokobay

Mappa di tutti i documenti del progetto, divisi per area. I percorsi sono relativi alla cartella `docs/`.
I documenti segnati con *(recuperato)* sono gli originali del 16/10/2026 ripresi dai backup, con i nomi aggiornati a quelli attuali: possono differire da quanto implementato oggi.

## 1. Idea e storia
- [Bibbia del progetto](design/00_BIBBIA.md) *(recuperato)* — visione, genere, vincoli di gameplay, la città di Solara, trama e personaggi.
- [Nomi del progetto: vecchio → nuovo](NOMI.md) — tabella di conversione dai nomi originali (Ultimo Miglio, Nina Vela…) a quelli attuali (Bokobay, Kaia Rio…).

## 2. Game design
- [01 — Core loop ed economia v3.3](design/01_CORE_LOOP.md) — ciclo di gioco fisico col joystick, valute, curve e ritmo dei capitoli.
- [03 — Monetizzazione e LiveOps v2](design/03_MONETIZZAZIONE.md) *(recuperato)* — competitor, valute, pubblicità, acquisti, pass e regole anti pay-to-win.
- [04 — UI/UX v3.3 «Controllo diretto»](design/04_UI.md) *(recuperato)* — HUD, joystick flottante, telefono, onboarding e schermate.
- [08 — Retention e post-game v1](design/08_RETENTION.md) *(recuperato)* — perché si torna a giocare, collezionabili, stagioni e contenuti dopo il finale.
- [10 — Gestionale v1.3](design/10_GESTIONALE.md) — stazioni gestionali, robot, mercato e decisioni finali del Director.
- [11 — Longevità v1.2](design/11_LONGEVITA.md) — progressione a lungo termine, Solara infinita e classifiche.
- [11 — Capitolo 1 completo](design/11_CAP1_COMPLETO.md) — contenuti, dipendenze e stima per chiudere il capitolo 1.
- [12 — Capitolo 1: criteri di accettazione QA](design/12_CAP1_CRITERI_QA.md) — criteri Given/When/Then misurabili per il capitolo 1.
- [13 — Capitolo 1: eventi di log dell'onboarding](design/13_CAP1_EVENTI_ONBOARDING.md) — specifica degli eventi registrati nei 16 passi dell'onboarding.
- [14 — Zaino 8 e curva dei capitoli 2-3](design/14_ZAINO8_CURVA_CAP2-3.md) — costo dello Zaino 8 e ripiego per la curva dei capitoli successivi.
- [UI_SPEC_BIGLIETTI](design/UI_SPEC_BIGLIETTI.md) *(recuperato)* — biglietti di Oma Tilde, HUD senza sovrapposizioni e API di game feel.

## 3. Arte
- [Guida visiva — Capitolo 1](concept/GUIDA_VISIVA_CAP1.md) — stile «Diorama Solare»: palette, camera, personaggi, robot e ambienti del capitolo 1.
- [Confronto build web ↔ Guida visiva](concept/CONFRONTO_BUILD_CAP1.md) — differenze tra la build WebGL e la guida visiva, con catture.
- [Registro licenze asset 3D](LICENZE_ASSET.md) — fonte e licenza di ogni asset esterno presente nel repo.

### Concept art (`concept/img/`)
- [conceptA.jpg](concept/img/conceptA.jpg) — primo concept: la protagonista davanti al deposito con scooter e robot (esplorazione iniziale, nomi e insegne non aggiornati).
- [conceptB.jpg](concept/img/conceptB.jpg) — primo concept notturno con robot e mercato nero (esplorazione scartata: contiene nomi vecchi e un riferimento a una città reale).
- [ref_conceptA.jpg](concept/img/ref_conceptA.jpg) — concept di riferimento rivisto.
- [ref_solara_keyart_16x9.jpg](concept/img/ref_solara_keyart_16x9.jpg) — key art di Solara, 16:9.
- [ref_solara_houses_v02.jpg](concept/img/ref_solara_houses_v02.jpg) — case e tetti solari di Solara.
- [ref_bg_gameplay_iso.jpg](concept/img/ref_bg_gameplay_iso.jpg) — sfondo isometrico di gioco.
- [ref_3d_beauty_bancone_v03_1170x2532.jpg](concept/img/ref_3d_beauty_bancone_v03_1170x2532.jpg) — beauty shot 3D del Bancone, formato portrait.
- [ref_sheet_nina_v01.jpg](concept/img/ref_sheet_nina_v01.jpg) — scheda personaggio della protagonista (oggi Kaia Rio).
- [sheet_nonna_ada_v01.jpg](concept/img/sheet_nonna_ada_v01.jpg) — scheda personaggio della nonna (oggi Oma Tilde).
- [ref_sheet_robot_v01.jpg](concept/img/ref_sheet_robot_v01.jpg) — scheda dei robot.
- [ref_turnaround_bot_nonno_v02.jpg](concept/img/ref_turnaround_bot_nonno_v02.jpg) — turnaround del robot OLDO.
- [ref_turnaround_bot_tico_v02.jpg](concept/img/ref_turnaround_bot_tico_v02.jpg) — turnaround del robot TIKTAK.
- [ref_sheet_clienti_v01.jpg](concept/img/ref_sheet_clienti_v01.jpg) — scheda dei clienti.
- [ref_clienti_lineup_v02.jpg](concept/img/ref_clienti_lineup_v02.jpg) — lineup dei clienti.
- [build_desktop_01.png](concept/img/build_desktop_01.png), [build_portrait_01.png](concept/img/build_portrait_01.png), [build_portrait_03.png](concept/img/build_portrait_03.png) — catture della build usate nel confronto con la guida.
- [cfr3/](concept/img/cfr3/) — sette catture del terzo confronto build ↔ guida (avvio, movimento, arrivi, scaffali, bancone, insegna, pad).

### Revisioni artistiche e UI (`review/`, `ui/`)
- [Review #2 — Personaggi e clienti vs guida visiva](review/2/README.md) — confronto del 3D Artist, con [lineup frontale](review/2/lineup_fronte.png) e [lineup iso 3/4](review/2/lineup_iso34.png).
- [review/70/](review/70/) — catture dell'insegna (fronte, lato, iso 3/4).
- [review/82/](review/82/) — modello 2D del layout e calcolo dei percorsi ([layout_82.py](review/82/layout_82.py), [detour_82.py](review/82/detour_82.py), [detour_82.txt](review/82/detour_82.txt)).
- [review/118/](review/118/) — zoom sulla scena della nonna.
- [review/187/](review/187/) — layout delle zone, vecchio contro nuovo.
- [review/224/](review/224/) — visibilità dei pad Zaino, Scarpe e Insegna ([misure_224.txt](review/224/misure_224.txt)).
- [review/256/](review/256/) — varchi verso Cassa e Cassaforte ([rosso_8e33a40.txt](review/256/rosso_8e33a40.txt): test rosso prima della correzione).
- [ui/catture/pr36/](ui/catture/pr36/) — catture HUD e impostazioni della PR #36 e script [uxml_to_html.py](ui/catture/pr36/uxml_to_html.py).

## 4. Tecnica
- [02 — Tech](design/02_TECH.md) *(recuperato)* — Unity 6 URP, architettura, salvataggi, offline, SDK, acquisti e performance Android.
- [Protocollo Unity condiviso](design/UNITY_PROTOCOLLO.md) *(recuperato)* — regole d'uso di Unity tra i ruoli: copie di lavoro, lock, consegne, lingua.
- [Analisi caricamento WebGL mobile](tech/ANALISI_CARICAMENTO_WEBGL.md) *(recuperato)* — dimensioni, compressione e tempi di caricamento della build WebGL.
- [LEGGIMI — Build WebGL, test e link giocabile](../LEGGIMI_BUILD.md) — pipeline, script di test, build e deploy.

## 5. QA
- [Checklist LIVE Capitolo 1](../qa/CHECKLIST_CAP1.md) — voci da eseguire a ogni LIVE, con esito VIA/NO-VIA.
- [Scheda di osservazione playtest](../qa/SCHEDA_OSSERVAZIONE_PLAYTEST.md) — scheda per osservare l'onboarding fino alla prima rata ([versione CSV](../qa/scheda_osservazione_playtest.csv)).
- [Playtest LIVE WebGL (build 25ab5cf)](../qa/playtest_web_live.md) — primo playtest dal vivo della build web.
- [Playtest LIVE v8 — baseline](../qa/playtest_web_live_v8_baseline.md) — stato prima delle correzioni P0.
- [Playtest LIVE v9 — QA indipendente](../qa/playtest_web_live_v9.md) — verifica indipendente delle correzioni P0.
- [LIVE P0](../qa/playtest_web_live_p0.md) — verifica della build WebGL_p0.
- [Fix QA #41–#44](../qa/fix_qa_41_44.md) — correzioni e test dopo il LIVE su 25ab5cf.
- [econ_sim_v3.py](../qa/econ_sim_v3.py) — simulazione dell'economia v3.3; risultati in [sim_v34_gd_matrix.txt](../qa/sim_v34_gd_matrix.txt).
- [playthrough_ch1.py](../qa/playthrough_ch1.py) — genera il golden path del capitolo 1 (`playthrough_expected.json`).
- [eval_liveops_ch1.py](../qa/eval_liveops_ch1.py) — valuta le proposte LiveOps sul golden path.
- [playtest_shots/](../qa/playtest_shots/), [shots_before/](../qa/shots_before/), [shots_after/](../qa/shots_after/) — catture dei playtest.
- [QA onboarding graduale (#94)](qa/onboarding_94/README.md) — anteprima HTML e catture a 360/390/412 dp.
- [QA telefono (#71)](qa/telefono_71/README.md) — contrasto e barre vuote, prima e dopo.
- [QA tutorial HUD (#77)](qa/tutorial_hud_77/README.md) — anteprima del tutorial HUD e catture.

## 6. Processo
- [Stato del progetto](../STATO_PROGETTO.md) — stato corrente, vincoli fissi, regole di lavoro e registro delle decisioni.
- [Sprint 1 — Vertical slice Capitolo 1](design/09_SPRINT1.md) *(recuperato)* — obiettivo, owner e piano del primo sprint.
- [Questo indice](INDICE.md).
