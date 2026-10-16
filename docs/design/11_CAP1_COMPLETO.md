# 11 - CAPITOLO 1 COMPLETO: contenuti, dipendenze, stima (Game Director: DevOn)
Data: 15/10/2026. Regola di Lorenzo: **nessuna build va consegnata a Lorenzo finché il cap. 1 non è completo e curato.** Le build WebGL di verifica restano interne alla squadra (1-2 al giorno).
Fonti verificate: 00_BIBBIA, 01_CORE_LOOP v3.3, 02_TECH, 04_UI v3.3, 09_SPRINT1, data/ch1/*.json, README in wt/*, wt_vfx_progress.md, unity/ (ramo `webgl-min`, ultimo commit 13cca59), conceptA.jpg (nella radice del progetto, non in art/concept).

## 0. Stato verificato (ramo webgl-min)
- **Fatto:** ciclo giocabile minimo Arrivi→scaffale→Bancone→monete→Cassa/pad (LoopAutoTest + PlayMode Ch1LoopTest PASS); Nina finale (FBX + controller); NONNO v02 (bot_nonno.fbx + controller); HUD responsive (da wt/ui); VFX v1.3 nel ramo (v1.4 in wt/vfx, non ancora unita); dati ch1 importati (Ch1Data, DataLoader); 24 SFX in `_Game/Audio`; 4 stem musicali `mus_day_vela_e_figli_*` in `Assets/Audio/Music` e AudioManager con supporto stem.
- **Parziale:** kit 3D (env_*, prop_* + 15 prop CC0 ThirdParty) copiato in `_Game/Art/Environment` ma **non piazzato** in Ch1_Deposito; Customer/CustomerQueue esistono ma senza modelli/tipologie animate; Coin.prefab e fx_coin presenti, animazione monete→HUD non ancora fatta (prevista nello sprint game feel).
- **Mancano nel ramo:** SFX passi, deposito, NONNO (sfx_step_*, sfx_parcel_deposit_*, sfx_nonno_*) generati in Assets/Audio ma da verificare il cablaggio; onboarding (nessuno script dedicato); ciclo giorno (nessuno script DayCycle in gameplay, solo dati); clienti 3D.
- **Scoperta VFX (15:22):** postProcessData nullo nel renderer mobile → il post non era mai attivo, anche su main. Fix in wt/vfx (serve solo ai tier con post; su WebGL il post resta spento).
- **TICO** è pronto (3d/export/bot/bot_tico.fbx) ma da design entra al **cap. 4** (Smistamento): NON rientra nel cap. 1. Si può usare al massimo come cameo statico/teaser.
- **Ciclo giorno/notte – nota di coerenza:** da 01_CORE_LOOP e daycycle.json il cap. 1 ha **giorno 6:55 + tramonto 5 s, nessuna notte** (notte dal cap. 6). Proposta: nel cap. 1 implementare il ciclo completo (alba→giorno→tramonto, luci calde delle lanterne a fine ciclo) con il sistema già pronto per la notte, e mostrare la notte solo come "chiusura bottega" di fine giornata. **Da confermare con Lorenzo** se vuole una notte giocabile già nel cap. 1 (cambierebbe l'economia: +2-3 gg GD/QA).

## 1. Contenuti del cap. 1 per ruolo
Legenda stato: ✅ fatto · 🟡 parziale · ⬜ da fare. gp = giorni-persona.

### Game Designer
*(aggiornato dal Game Designer il 16/10/2026)*
| Voce | Stato | gp |
|---|---|---|
| Tabelle ch1 (economy, debt, upgrades, robots, zones, offline, daycycle) in JSON | ✅ (16/10: Zaino 5-7 a 560/960/1.700, `rewards` in economy.json, robots.json riallineato a NONNO 50 LN) | 0 |
| Golden path playthrough_expected.json | ✅ (rigenerato 16/10: rata a 34:00, 32:00-36:00) | 0 |
| Tipologie clienti (4: normale, frettoloso, anziano, VIP Mamma Rosa) con pazienza/valore/frequenza/sblocco in customers.json | ✅ | 0 |
| Script onboarding come dati (onboarding.json + onboarding.md, 15 passi con trigger, obiettivo, suggerimento, fallback) | ✅ | 0 |
| Ricompense intermedie (traguardi pacchi, tappe debito, mancia Cassa, freccia prossimo pad) | ✅ dati e sim · ⬜ verifica in build | 0,5 |
| Criteri di accettazione QA (12_CAP1_CRITERI_QA.md) | ✅ | 0 |
| Taratura game feel (velocità, stack, tempi pad) dopo playtest interni | ⬜ | 1,5 |
| Decisione notte nel cap. 1 + eventuale ritaratura | ⬜ | 0,5 (+2 se notte giocabile) |
| **Totale residuo** | | **2,5** |

### Narrative
| Voce | Stato | gp |
|---|---|---|
| Plot e personaggi (Bibbia) | ✅ | 0 |
| Testi onboarding (Nina, Nonna Ada lettera, Carico 0 sul bancone) | ⬜ | 1 |
| Battute brevi NONNO (bolle/grammelot) e clienti (barks per tipologia) | ⬜ | 1 |
| Testo fine capitolo / gancio cap. 2 (prima tranche debito, Viktor) | ⬜ | 0,5 |
| **Totale** | | **2,5** |

### Concept Artist
| Voce | Stato | gp |
|---|---|---|
| conceptA, sheet Nina/robot, key art Solara, review Bancone v03/NONNO v02/TICO v02/case Solara v02 | ✅ | 0 |
| Pianta set dressing del deposito (layout props, punti focali, Borgo Gradoni fuori dalle finestre) | ⬜ | 1 |
| Sheet clienti (5 tipologie: residente, rider, anziana, turista, VIP) – art/sheet_clienti_v01.jpg; resta il foglio varianti teste/colori | 🟡 v01 consegnato | 0,5 |
| Review stile su ogni build (overpaint/correzioni) | 🟡 continuo | 2 |
| **Totale** | | **3,5** |

### 3D Artist
_Aggiornato dal 3D Artist il 15/10/2026, 15:30 CEST. Percorsi relativi a `/workspace/ultimo-miglio/`; "wt" = `wt/3d/Assets/_Game/Art/`._

**Esiste (verificato su disco):**
- Atlas: `wt/Textures/T_Palette_Atlas.png` + `.json`. Extra accodati: `arancio`, `intonaco_terra`, `intonaco_sole`, `intonaco_menta` (row 15, col 12-15). Generatore: `3d/scripts/make_palette_atlas.py`.
- Kit Vela & Figli: `wt/Environment/Kit/`, 21 FBX: env_floor_2x2_a/b, env_wall_2m_a/b, env_wall_corner, env_wall_arch_4m, env_plinth_edge_2m, prop_counter_2m, prop_shelf_2m, prop_box_s/m, prop_carico0, prop_register, prop_safe, prop_lantern_wall, prop_lemontree_pot, prop_plant_small, prop_sign_vela_figli, prop_wallshelf, prop_books, env_kit_vela_figli.
- Prop CC0 Kenney adattati (approvati Concept v02): 23 FBX in `wt/Environment/ThirdParty/Props/`, GLB in `3d/export/thirdparty/`. Scaffali, carrello, banchi frutta/pane, scatole, casse, botte, panca, lampade, piante, limone, arancia, tenda, ombrellone. 27-650 tri. Licenze in `LICENZE_ASSET.md`.
- Case di Solara originali (v02, direzione approvata): `wt/Environment/Solara/Houses/` con env_solara_house_a..e (2,0-3,0k tri), env_solara_steps_2m (1,3k) ed env_solara_terrace_base_4x4 (1,0k). GLB in `3d/export/solara/`. Render: `3d/renders/solara_houses_v02.png`.
- Pacco singolo impilabile: `wt/Props/prop_parcel_box.fbx` (76 tri, 0,4x0,3x0,3 m). GLB: `3d/export/props/prop_parcel_box.glb`.
- Layout stanza Bancone (muri chiusi, coordinate Unity): `wt/Environment/Layouts/bancone_layout.json` + README. Script Editor: `wt/3d/Assets/_Game/Scripts/Editor/LayoutInstantiator.cs` (menu Ultimo Miglio > Layout).
- Nina: `wt/Characters/chr_nina_vela.fbx` + controller. NONNO v02: `wt/Characters/bot_nonno.fbx` + controller (sorgente in `3d/export/bot/`, in modifica in questo momento). TICO: `3d/export/bot/bot_tico.fbx` (cap. 4, al massimo cameo).
- Moneta: `wt/FX/fx_coin.fbx`.

| Voce | Stato | gp |
|---|---|---|
| Nina finale, NONNO v02, TICO (cap. 4) | ✅ | 0 |
| Kit modulare (21 FBX) + 23 prop CC0 adattati + atlas | ✅ in wt/3d (da portare nel ramo con la procedura Unity) | 0 |
| Layout di riferimento Bancone (JSON + script istanziazione) | ✅ (da verificare in Unity il verso della conversione) | 0 |
| Piazzamento in Ch1_Deposito: Bancone dal layout, poi Arrivi/Magazzino (scaffali, Cassa, Cassaforte, pad, set dressing con prop ThirdParty) | 🟡 Bancone pronto come layout, Magazzino/Arrivi da comporre | 2 |
| Esterno Borgo Gradoni (case, gradoni, tetti solari, panni stesi, scorcio Faro spento) | 🟡 5 case + gradoni pronti; mancano vicolo/arco, insegne, Faro spento impostor, sfondo baia | 1,5 |
| Clienti: base modulare + 4-5 varianti (teste/abiti/colori), rig compatibile con l'Animator | ⬜ (serve prima lo sheet clienti del Concept) | 3 |
| Pacchi: prop_parcel_box ✅, prop_box_s/m ✅; mancano prop_box_l, varianti colore/etichetta, prop_box_special_rosa | 🟡 | 0,5 |
| Moneta LN rifinita (fx_coin esiste, rifinitura con VFX) | 🟡 | 0,25 |
| Fix UV/atlas Nina "a strisce" (segnalato da VFX) | ⬜ da verificare | 0,5 |
| Materiale Unity M_Palette_Atlas condiviso + prefab dei prop (collider _COL, LOD1) | ⬜ | 1 |
| LOD/ottimizzazione WebGL e mobile (conta tri per scena, draw call, istanziazione pacchi) | ⬜ | 1 |
| **Totale residuo** | | **9,75** |

### Animator
Ramo `animator` (`wt/animator`). Nina, NONNO e TICO sono animati a mano (originali); i clienti possono partire da Mixamo o mocap CC0, sempre ritoccati e registrati in `LICENZE_ASSET.md`. Naming `chr_<nome>@<clip>` / `bot_<nome>@<clip>`, 30 fps, export FBX in `anim/export/`.

| Voce | Stato | Priorità | gp |
|---|---|---|---|
| Nina v02 (Humanoid): Idle, Walk, Run, Carry_Idle, Carry_Walk, Deliver, Cheer + secondary motion coda di cavallo | ✅ | — | 0 |
| Nina: fix direzione Walk | 🟡 | P0 | 0,25 |
| Nina: posa Carry migliorata con pila di pacchi (Avatar Mask upper body, layer additivo) | 🟡 | P0 | 0,5 |
| Nina: fix stiramento giacca in Cheer (pesi/clip) | 🟡 | P1 | 0,25 |
| Controller Nina (Speed/Carry + trigger Deliver, Cheer) | ✅ (da verificare in gioco con carry/cheer) | P0 | 0,25 |
| NONNO v02 muletto ruggine (Generic 8 ossa): Idle, Drive, Lift (forche max 0,40 m), Drop, Happy + controller (Speed/Lift + trigger Drop, Happy) | ✅ | — | 0 |
| NONNO: re-import/verifica clip dopo fix geometria baffi/ruota anteriore (in attesa del 3D Artist) | ⬜ bloccato | P1 | 0,25 |
| NONNO "che lavora": loop prendi→porta→deposita, transizioni Drive/Lift/Drop pulite, idle con personalità (baffi, trombetta, sbuffo), reazione all'upgrade; eventi animazione per TL/VFX/Sound | 🟡 | P0 | 2 |
| TICO v02 (Idle, Move, Stamp, Happy) + controller (Speed + trigger Stamp, Happy) — cap. 4, nessun lavoro extra nel cap. 1 | ✅ | — | 0 |
| Materiali: assegnare `T_Palette_Atlas` a Nina/NONNO/TICO in Unity (coordinare con VFX per il fix UV "a strisce") | ⬜ | P0 | 0,25 |
| Clienti: rig condiviso Humanoid sulla base modulare del 3D Artist (dipende da modello clienti) | ⬜ | P0 | 0,5 |
| Clienti: clip walk in / walk out, idle, attesa/impaziente, felice (pagamento), arrabbiato (uscita) — base Mixamo/CC0 ritoccata in stile toon | ⬜ | P0 | 2 |
| Clienti: varianti di personalità per 4-5 tipologie (velocità, override clip) + controller condiviso (Speed, Patience + trigger Pay, Angry) e Animator Override per tipologia | ⬜ | P1 | 1 |
| Clienti: voci in `LICENZE_ASSET.md` per ogni clip Mixamo/CC0 usata | ⬜ | P0 | 0,1 |
| Pacchi: squash&stretch su pickup/drop, oscillazione della pila | ⬜ | P1 | 1 |
| Monete: spawn, salto, volo verso HUD (curve, in coordinamento con VFX game feel) | ⬜ | P1 | 0,5 |
| GIF di anteprima e verifica in Unity (screenshot-isolated) di tutte le clip | ⬜ | P2 | 0,5 |
| **Totale residuo** | | | **≈ 9,4** |

### VFX & Technical Artist
Stato verificato il 15/10 su `wt/vfx/Assets/_Game/VFX/README.md`, `wt_vfx_progress.md` e `art/vfx/` (vfx_day.png, vfx_sunset.png, vfx_fx_*.png, vfx_toon_outline_vs_set.png, luminance.txt). Legenda: ✅ fatto · 🟡 in corso · ⬜ da fare.
**Decisione di Lorenzo:** il cap. 1 è solo **GIORNO e TRAMONTO con lanterne, nessuna notte** (preset Notte resta nel codice, inutilizzato). **Camera cap. 1:** ortografica, size 4.5, distanza 15 (pitch 35.3°, yaw 45°): usata anche per gli screenshot VFX. **WebGL: post-process OFF** sulla camera (causava schermo magenta); il tier Web non lo riattiva (`UM_QualityTier.AllowPostOnWeb = false`, opt-in solo dopo verifica in build).

| Voce | Stato | Criteri di accettazione | gp |
|---|---|---|---|
| Shader `UM_ToonDiorama`: 3 bande morbide, ombre calde (mai nero), rim crema, outline inverted-hull solo su personaggi/robot/prop interattivi (`M_Char_Atlas`, `M_Props_Interactive`) | ✅ | Nessun nero puro in ombra; outline assente su set/pavimento; SRP Batcher compatibile; `#pragma target 3.0`, niente compute | 0 |
| `UM_LightingRig` Giorno/Dorato + `SetGoldenBlend(t)` | ✅ | Interpola sole (colore, intensità, angolo radente), ambiente Trilight e fog senza scatti | 0 |
| Tier `UMTier` Web/Base/Standard/Alto | ✅ | Web: scala 0.9, DPR ≤ 1.5, ombre soft Low 24 m/1024, MSAA 2x, **post OFF**, outline solo char/prop, particelle ×0.75, max 2 luci lanterna reali | 0 |
| Particelle pop pacco / monete (lira `fx_coin.fbx` LOD1, `M_FX_Coin`) / upgrade / polvere (foglio Kenney CC0 `T_FX_Sheet`) | ✅ | Sprite non quadrati, monete con colori atlas; un solo materiale; licenza CC0 in LICENZE_ASSET | 0 |
| Review 2 Concept Artist (v1.4): lanterne 1.7 falloff morbido, polvere `#B8906A`, scintille +30% nucleo limone `#F7E26B`/alone `#F4C430`, fix scalettature ombre, variazione pavimento (`_NoiseStrength` 0.06), AO di contatto (`fx_contact_shadow`) | 🟡 in wt/vfx, da unire | Ok del Concept Artist sui PNG; ombre senza scalettature | 0,5 |
| Sprint Game Feel: `fx_package_pickup`, `fx_package_deliver`, `fx_coin_flyer` (monete → HUD con scia e scintille, API `FlyCoins*`), `fx_pad_fill_ring` (`UM_PadFillRing.SetProgress`) + `fx_upgrade_complete` | 🟡 compilati e catturati in wt/vfx, da cablare (Tech Lead) | Burst ≤ 0,5–0,8 s; monete in arco verso il contatore HUD (bersaglio mobile) con callback all'arrivo; anello 0→1 morbido, flash + burst una sola volta a 1; budget Web ×0.75 | 1 |
| **Ciclo giorno → tramonto con lanterne** (`UM_DayCycle` + `UM_Lantern`) | 🟡 scritto e testato in headless (wt/vfx), da integrare in Ch1_Deposito | Vedi sotto | 0,5 |
| VFX NONNO (fumo/scintille muletto), polvere passi, insegna | ⬜ | In palette, ≤ 12 particelle vive; polvere passi legata all'Animator | 1 |
| Performance WebGL (draw call, batching, ombre) | ⬜ | Build WebGL reale (non solo llvmpipe) a 60 fps su laptop medio; draw call < 100 | 1 |
| **Totale** | | | **4** |

**Ciclo giorno/tramonto – stato**
- Fatto (wt/vfx): `UM_DayCycle` (giorno 415 s + tramonto 5 s da daycycle.json, ora dorata dall'80% del giorno, blend morbido, eventi `OnSunset`/`OnDayEnd`, `StartDay()`), API per Design/Narrative `ForceTimeOfDay(TimeOfDay.Morning|Day|GoldenHour|Sunset)` e `SetTime01(t)`; `UM_Lantern` (emissivo `#FFB65C` + luce 1.7 in dissolvenza al tramonto, spente al mattino, luci reali limitate per tier: Web 2, Alto 4, Base/Standard solo emissivo).
- ⬜ Integrazione in scena di gioco (Tech Lead): `UM_DayCycle` sul sole, `UM_Lantern` sulle lanterne del deposito, `StartDay()` a ogni nuovo giorno.
- Il Volume "tramonto" è usato solo se il post è attivo (non su WebGL).
- Criteri: transizione senza pop visibili; lanterne accese con emissivo + luce al tramonto; personaggi leggibili; costo Web invariato entro 1 ms.

### UI/UX
Stato verificato il 15/10 alle 16:15 su `webgl-min` (HUD v2 del TL: `Tokens.uss`, `HudV2.uss`, font Fredoka) e `wt/ui`. Spec applicabile: `UI_SPEC_BIGLIETTI.md`. **Direttiva di Lorenzo:** niente mockup né immagini per lui; conta solo la build giocabile. Priorità: biglietti di Nonna Ada chiari e HUD senza sovrapposizioni.

**HUD minimo del Cap. 1 (decisione di Lorenzo):** a schermo solo LN, giorno, riga obiettivo, joystick e pad. Niente telefono: rimandato a 'Gioco completo' (issue #64, decisione di DevOn). Combo: solo '+N' sopra Nina (clienti serviti di fila), sparisce in 1,5 s; il moltiplicatore x1,2 non si mostra. Debito: solo nella schermata di fine giornata e nel fumetto di Ada quando lo spiega. Colori dei pad: ritiro teal `#5E7F86`, consegna menta `#8FC9A8`, acquisto sole `#F4C430`, potenziamento `#F7E26B`.

| Voce | Stato | Criteri di accettazione | gp |
|---|---|---|---|
| HUD responsive WebGL (ResponsivePanel, layout flex, TAB/ESC, WASD/mouse) | ✅ nel ramo, 🟡 da ridurre all'HUD minimo | Nessuna sovrapposizione a 1920x1080, 1366x768, 960x600 e in portrait (checklist §2 della spec); a schermo solo LN, giorno, riga obiettivo, joystick e pad; nessun pulsante telefono (#64) | 0 |
| HUD v2 (pillole compatte, pulsanti 3D, token) | 🟡 TL | Solo variabili di `Tokens.uss`; contrasto testo ≥ 4,5:1; target ≥ 48 dp | 0,5 |
| **Biglietti di Nonna Ada** (onboarding 7 passi + fine, testi in `onboarding_testi.json`) | 🟡 logica TL fatta, layout da correggere | Una nota alla volta con coda; banda medio-alta sotto la safe area, mai negli angoli HUD né nella metà bassa (joystick); corpo ≥ 16 dp, titolo ≥ 20 dp; ≤ 90 caratteri e 2 righe; minimo 1,5 s a schermo, sticky fino al completamento; nome con `--c-terracotta-dk` (7,2:1). Componente `AdaNoteView` nella spec | 0,5 |
| Game feel HUD: monete sull'icona LN, conteggio e pulse, clamp schede fuori dai rettangoli HUD | 🟡 base TL fatta (monete verso la pillola, clamp nel pannello) | Moneta atterra sull'icona LN; numero conta all'arrivo, non al credito; schede Cassa/pad mai sopra pillole, riga obiettivo, biglietto; combo solo '+N' sopra Nina, sparisce in 1,5 s, nessun x1,2 a schermo. `HudFeel` nella spec | 0,5 |
| Pad in-world (ritiro, consegna, acquisto, upgrade: anello, costo, riempimento) | 🟡 | Colori: ritiro `#5E7F86`, consegna `#8FC9A8`, acquisto `#F4C430`, potenziamento `#F7E26B`; anello 0→1 morbido; prezzo pagato/totale dai dati; scheda visibile solo vicino a Nina | 0,5 |
| Freccia obiettivo a bordo schermo | ✅ | Clamp alla safe area con margine 24 dp; diventa marker quando il bersaglio è in camera | 0 |
| ~~Telefono di Nina (Home, Ordini, Messaggi, Flotta)~~ | ➡️ Gioco completo (#64) | Fuori dal Cap. 1: la struttura già fatta resta nel ramo ma non va nell'HUD | 0 |
| Splash/caricamento WebGL | ⬜ | Logo + barra terracotta→sole, avvio < 5 s su laptop medio | 0,5 |
| Schermata fine giornata / fine capitolo (debito, versamento tranche, prossimo capitolo) | ⬜ | Unico punto dell'HUD con il debito (oltre al fumetto di Ada che lo spiega); nessuna modale bloccante oltre 3 s; un solo CTA | 1 |
| **Totale** | | | **4** |

### Sound Designer
| Voce | Stato | gp |
|---|---|---|
| 40 SFX OGG in `wt/sound/Assets/_Game/Audio` (game feel + passi, deposito, NONNO aggiunti alle 15:30) | ✅ file | 0 |
| Musica giorno "Vela & Figli" a 4 stem (base/perc/mel/full, 76,8 s, sample-aligned) | ✅ file, 🟡 qualità | 0 |
| Cablaggio e verifica in gioco di passi (per superficie), deposito, NONNO, pitch della pila | 🟡 | 1 |
| Seconda passata timbrica sulla musica (strumenti più ricchi, campioni CC0 rielaborati) e SFX NONNO meno sintetici | ⬜ | 1 |
| SFX clienti per tipologia (4-5 voci grammelot + barks), UI onboarding (tap, freccia obiettivo) | ⬜ | 1,5 |
| ➡️ Gioco completo (#64): SFX UI telefono (swipe, notifica) | rimandato, da rivedere col Sound Designer | 0 |
| Ambiente Borgo Gradoni (gabbiani lontani, vicolo, bottega, Faro Spento lontano) a 2 layer giorno/tramonto | ⬜ | 1 |
| Tramonto e "chiusura bottega": stinger + variante musicale; stinger tranche del debito e fine capitolo | ⬜ | 1 |
| Mix finale: AudioMixer (Musica/SFX/UI/Ambiente), snapshot giorno/tramonto, ducking, cap voci, test su speaker di smartphone (WebGL mobile), sblocco audio WebGL al primo input | ⬜ | 1 |
| **Totale** | | **6,5** |

### QA
| Voce | Stato | gp |
|---|---|---|
| Loop test PlayMode + LoopAutoTest | ✅ | 0 |
| Test cap. 1 completo da inizio a fine (log UMQA vs playthrough_expected) | ⬜ | 2 |
| Smoke giornaliero sulle 1-2 build WebGL (checklist) | ⬜ continuo | 3 |
| Performance (fps WebGL desktop/mobile, memoria) | ⬜ | 1 |
| Regressioni finali + verifica criteri 09_SPRINT1 | ⬜ | 1,5 |
| **Totale** | | **7,5** |

### Tech Lead
| Voce | Stato | gp |
|---|---|---|
| Architettura, EconomyCore, gameplay base, build WebGL, test | ✅ | 0 |
| Merge worktree (vfx v1.4, ui, 3d, animator, sound) su webgl-min senza regressioni | 🟡 | 2 |
| Clienti: spawner per tipologie, stati, animazioni, pazienza | 🟡 (Customer/CustomerQueue base) | 2 |
| NONNO lavoratore: percorso, stati animati, integrazione upgrade | 🟡 | 1,5 |
| Sistema onboarding (step machine, trigger da eventi core) | ⬜ | 2 |
| DayCycle runtime (da daycycle.json) + eventi luce/musica | ⬜ | 1,5 |
| Fine capitolo, salvataggio, offline/Cassa | 🟡 | 2 |
| Pipeline build sotto 16 GB (build serializzate, una istanza Unity per volta) | 🟡 | 1 |
| **Totale** | | **12** |

**Totale complessivo: ≈ 64,5 giorni-persona** (≈ 66,5 se si vuole la notte giocabile nel cap. 1; la UI/UX scende da 9 a 4 gp perché HUD responsive, freccia e logica onboarding sono già nel ramo e il telefono è rimandato a Gioco completo, #64).

## 2. Dipendenze e percorso critico
- Concept pianta set dressing → 3D piazzamento kit → VFX luci/ciclo giorno sul deposito vero → Concept review.
- Concept sheet clienti → 3D clienti → Animator clienti → TL spawner tipologie → Sound voci clienti → QA.
- GD dati clienti/onboarding + Narrative testi → UI onboarding → TL step machine → QA run completo.
- Animator NONNO "lavora" → TL NONNO → VFX/Sound NONNO.
- VFX v1.4 + fix post → merge TL (blocca ogni review di stile: senza post i colori sono falsi).
- Tutto → merge su webgl-min → build WebGL → QA (ogni build pesa: una sola istanza Unity per volta sulla box).

**Percorso critico (≈ 11 giorni di calendario al realistico):**
merge v1.4/fix post (1) → sheet clienti (1,5) → modelli clienti (3) → animazioni clienti (3) → integrazione tipologie (1,5) → game feel + rifinitura (1) → QA completo + fix (2) ≈ **13 gg** sulla catena clienti, in parte sovrapponibile (il TL prepara lo spawner su placeholder) → **≈ 11 gg**.
Percorso quasi-critico: set dressing (3+2) → luci/ciclo giorno (2) → perf (1,5) → QA.

## 3. Stima in giorni di calendario
Ipotesi: 10 ruoli in parallelo; 1-2 build WebGL di verifica al giorno; box con 16 GB di RAM condivisa (swap già usato: ~7 GB occupati e 3 GB di swap alle 15:28) → **massimo una istanza Unity in batchmode alla volta**, build e capture serializzate in una coda, Blender e Unity non insieme durante le build. Questo collo di bottiglia aggiunge ~20-30% al calendario rispetto al puro parallelo.

| Scenario | Giorni di calendario | Ipotesi |
|---|---|---|
| Ottimistico | **9** | clienti da base CC0 ritoccata, nessuna rottura nei merge, nessuna notte giocabile |
| **Realistico** | **13** | 2-3 cicli di review di stile, 1-2 giorni persi tra merge/RAM/build rotte |
| Pessimistico | **19** | clienti fatti da zero, problemi WebGL/perf, crash per RAM, notte giocabile richiesta |

Consegna a Lorenzo solo dopo la milestone M5 (vedi sotto).

## 4. Milestone
- **M1 – Sprint GAME FEEL (gg 1-3):** merge VFX v1.4 + fix post; pickup/delivery juice, monete→HUD, anello pad, combo; squash pacchi; cablaggio SFX passi/deposito; musica a stem attiva. Uscita: loop minimo che "si sente bene" (review interna Concept + GD).
- **M2 – Deposito vero (gg 3-6):** set dressing completo Arrivi/scaffali/Bancone/Cassa/Cassaforte/pad + esterno Borgo Gradoni; fix UV Nina; luci sul deposito reale. Uscita: screenshot approvati dal Concept, fedeli a conceptA.
- **M3 – Vita nel deposito (gg 5-9):** clienti 4-5 tipologie animate con voci; NONNO che lavora (animazioni, VFX, suoni); DayCycle runtime con tramonto e lanterne.
- **M4 – Onboarding e HUD finale (gg 7-11):** onboarding primi 5 minuti, freccia obiettivo, HUD minimo (LN, giorno, riga obiettivo, joystick, pad), debito solo a fine giornata, splash, fine capitolo; testi Narrative.
- **M5 – Rifinitura e QA (gg 11-13):** run completo cap. 1 vs playthrough_expected, performance, mix audio, regressioni, review stile finale. **Solo allora build a Lorenzo.**

## 5. Le 5 voci più a rischio
1. **Clienti animati di varie tipologie** – partono da zero (modelli + animazioni + logica), sono sul percorso critico.
2. **RAM della box (16 GB, swap già in uso)** – build/capture serializzate; un crash di Unity costa mezza giornata e limita a 1-2 build/giorno.
3. **Merge dei worktree su webgl-min** – 6 worktree divergenti (vfx v1.4, ui, 3d, animator, sound, qa); il fix del post cambia tutti i colori → rischio regressioni visive e di test.
4. **Ciclo giorno/notte** – ambiguità di design (cap. 1 senza notte in 01_CORE_LOOP/daycycle.json) da chiarire con Lorenzo; se serve notte giocabile cambia l'economia.
5. **Onboarding iniziale** – nessuno script esistente, dipende da GD+Narrative+UI+TL insieme ed è l'ultima cosa testabile solo a contenuti finiti.
