# VFX & Technical Art – Sprint 1 (Capitolo 1) – Bokobay
Unity 6 + URP. Target attuale: **WebGL** (ordine di Lorenzo); Android resta supportato dai tier Base/Standard/Alto.
**Cap. 1 = solo giorno e tramonto con lanterne, niente notte** (il preset Notte resta nel codice, inutilizzato). Camera cap. 1: ortografica size 4.5, distanza 15, pitch 35.3°, yaw 45°. **Su WebGL il post-process è spento** (dava magenta).
Stile "Diorama Solare" (00_BIBBIA, conceptA.jpg, 02_TECH §4-5, 3d/05_3D_PIPELINE). Cartella: `Assets/_Game/VFX/`.

## Contenuto
| File | Cosa fa |
|---|---|
| `Shaders/UM_ToonDiorama.shader` | Toon URP: 3 bande morbide (half-lambert), ombre calde (mai nero), rim crema, ombre ricevute con bordo toon filtrato (niente scalettature), fog, luci aggiuntive con falloff morbido (lanterne), variazione di tinta in world-space attivabile (`_NoiseStrength`), outline inverted-hull attivabile per materiale (`_OutlineOn`). ShadowCaster/DepthOnly propri, un solo `UnityPerMaterial` → compatibile SRP Batcher. |
| `Scripts/Runtime/UM_Palette.cs` | Esadecimali della palette (base + derivati luce/FX). |
| `Scripts/Runtime/UM_LightingRig.cs` | Sole, ambiente Trilight, fog lineare; preset Giorno/Dorato/Notte + `SetNightBlend(t)`. |
| `Scripts/Runtime/UM_DayCycle.cs` | Ciclo giorno → ora dorata → tramonto, eventi e API per forzare l'ora. |
| `Scripts/Runtime/UM_Lantern.cs` | Lanterna: emissivo + luce in dissolvenza al tramonto, luci reali limitate per tier. |
| `Scripts/Runtime/UM_QualityTier.cs` | Tier Base/Standard/Alto (02_TECH) + **Web**: render scale, ombre, outline, bloom, FXAA, scala particelle. |
| `Scripts/Runtime/UM_FxFactory.cs` | Particelle procedurali: pop pacco, monete, upgrade, polvere + blob di contatto (AO finta). |
| `Scripts/Runtime/UM_FxPlayer.cs` | Pool round-robin: `UM_FxPlayer.Play(Fx.Coins, pos)`. |
| `Scripts/Editor/UM_FxPrefabBuilder.cs` | Crea materiali (`M_Diorama_Atlas`, `_Emissive`, `M_Char_Atlas`, `M_Props_Interactive`, `M_FX_Particles`, `M_FX_Coin`, `M_FX_ContactShadow`) e prefab `fx_*`. |
| `Scripts/Editor/UM_PostProcessBuilder.cs` | Crea `Profiles/PP_Diorama_Day/Night.asset` + Volume globale; assegna `PostProcessData` al renderer URP se manca. |
| `Scripts/Editor/UM_LightingSetup.cs` | Sole + rig in scena, sfondo Cielo, ombre mobile/web sull'asset URP (soft Low). |
| `Scripts/Editor/UM_VfxVerify.cs` | Verifica headless: builder + scena test + PNG + luminanza media. |
| `Textures/T_FX_Sheet.png` | Foglio sprite 4x2 (512x256): punto, sbuffo, stella, anello (Kenney CC0) + scintilla pre-colorata (originale). |
| `Textures/T_FX_ContactBlob.png` | Blob radiale 64 px (originale) per l'ombra di contatto. |

## Installazione
1. Pacchetti: solo `com.unity.render-pipelines.universal`. Niente VFX Graph (richiede compute, non disponibile su WebGL).
2. Menu **Ultimo Miglio/VFX/**: `Build FX Prefabs + Materials` → `Build Post-Process Profiles` → `Setup Warm Lighting (open scene)`.
   Batch: `-executeMethod UltimoMiglio.VFX.EditorTools.UM_FxPrefabBuilder.BuildAll` (idem `UM_PostProcessBuilder.BuildAll`).
3. Camera: Post Processing ON. Al boot il Tech Lead chiama `UM_QualityTier.Apply(tier, materialiToon)`.
4. Atlas palette: `Assets/_Game/Art/Textures/T_Palette_Atlas.png` (no mip, Point; meta corretto in wt/3d). Moneta: `Assets/_Game/Art/FX/fx_coin.fbx` (si usa la LOD1, 34 tri).
5. Ombra di contatto: istanziare `Prefabs/fx_contact_shadow.prefab` come figlio di personaggi, robot e pacchi (scala 0.7–0.9).

## Game feel (sprint "My Little Universe")
Prefab in `Prefabs/` (da assegnare al componente `UM_FxPlayer` nella scena):
| Prefab | Uso | Costo |
|---|---|---|
| `fx_package_pickup` | Kaia/OLDO raccoglie un pacco: pop verso l'alto cartone/crema + 2 stelline | ~11 particelle, 0.4 s |
| `fx_package_deliver` | pacco consegnato (bancone, cliente, furgone): anello giallo sole + coriandoli palette + stella | ~16 particelle, 0.7 s |
| `fx_upgrade_complete` | pad completato: flash a terra + fontana di scintille | ~17 particelle, 0.8 s |
| `fx_coin_flyer` | una moneta volante: mesh lira + scia corta (`M_FX_Trail`) + scintille per distanza | 34 tri + scia + ≤6 particelle |
| `fx_pad_fill_ring` | anello del pad upgrade (`UM_FxRingFill`, `_Fill/_Color/_Pulse/_Flash` compatibili con `ZoneFillRing` della UI) | 1 quad, nessuna texture |

API (namespace `UltimoMiglio.VFX`):
```csharp
UM_FxPlayer.Play(UM_FxPlayer.Fx.PackagePickup, worldPos);   // anche PackageDeliver, UpgradeComplete, Coins, Upgrade, PackagePop
int UM_FxPlayer.FlyCoins(Vector3 worldStart, RectTransform hudTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null); // HUD uGUI
int UM_FxPlayer.FlyCoinsToScreen(Vector3 worldStart, Func<Vector2> screenPoint, int count = 1, Action onEachArrive = null, Action onAllArrived = null); // HUD UI Toolkit (pixel, origine in basso a sinistra)
int UM_FxPlayer.FlyCoinsTo(Vector3 worldStart, Vector3 worldTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null);
int UM_FxPlayer.FlyCoinsTo(Vector3 worldStart, Func<Vector3> worldTarget, int count = 1, Action onEachArrive = null, Action onAllArrived = null); // bersaglio mobile
UM_PadFillRing.SetProgress(float p01);  // insegue morbido; a 1 → flash + Play(UpgradeComplete) + evento OnCompleted
UM_PadFillRing.ResetRing();  UM_PadFillRing.SetImmediate(float p01, float flash = 0);
```
- Volo moneta: Bézier quadratica (altezza arco 1.6 m), easeInOutQuad, 0.6 s, sfasamento 0.06 s tra monete, scala 1→0.55, rotazione 720°/s. Il bersaglio è rivalutato ogni frame (l'HUD può muoversi). Profondità a metà strada verso la camera → non passa dietro i muri.
- Pool: `poolSize` 4 per burst, `coinFlyerPool` 12 (×0.75 su Web/Standard). Se le monete richieste superano il pool, le eccedenti chiamano subito `onEachArrive`: il conteggio economico resta giusto. Ritorna quante monete sono partite davvero.
- Per l'HUD UI Toolkit (04_UI) il Tech Lead passa `() => { var c = el.worldBound.center; return new Vector2(c.x * s, Screen.height - c.y * s); }` con `s` = scala del pannello.
- Materiali: tutti i burst usano `M_FX_Particles` (1 materiale, 1 texture); moneta `M_FX_Coin`; scia `M_FX_Trail`; anello `M_FX_RingFill` (valori per pad via MaterialPropertyBlock: pochi pad, costo trascurabile).

## Ciclo del giorno (cap. 1)
```csharp
UM_DayCycle.Instance.StartDay();                                   // nuovo giorno di gioco
UM_DayCycle.Instance.ForceTimeOfDay(UM_DayCycle.TimeOfDay.Sunset, pause: true, instant: false); // Morning | Day | GoldenHour | Sunset
UM_DayCycle.Instance.SetTime01(0.9f);                              // 0..1 sull'intera giornata
UM_DayCycle.Instance.Pause(); .Resume();  eventi OnSunset, OnDayEnd;  proprietà Time01, GoldenBlend, LanternsOn, Current
lantern.SetOn(true, instant: false);                               // UM_Lantern singola (di solito la pilota il ciclo)
```
- Durate da `daycycle.json`: giorno 415 s + tramonto 5 s. Ora dorata dall'80% del giorno, lanterne accese a blend 0.6 (ritardo casuale ≤ 0.6 s, dissolvenza 1.2 s).
- Tramonto: sole `#FFC27A` 0.85, angolo (22, -65, 0); ambiente `#F2C29A`/`#D9A27E`; fog/sfondo `#F0C8A0`.
- Lanterne: luci reali max **Web 2, Alto 4, Base/Standard 0** (solo emissivo); scelte ogni 0.5 s le più vicine al centro della vista.
- Volume "tramonto" opzionale: pesato solo se `UM_QualityTier.PostProcessingEnabled` (mai su WebGL).

## Palette (05_3D_PIPELINE + conceptA)
Terracotta `#C8603A` · Giallo sole `#F2B53A` · Menta `#8FC9A8` · Limone `#F4E04D` · Teal GrayBox `#4E7F86` · Tufo `#D9C4A0` · Pavimento `#E6D2B0` · Legno `#9A6A42` · Cartone `#C99A62` · Scuro caldo `#3B2F2A` · Arancio Kaia `#E5743A` · Crema `#FFF4D6` · Cielo `#F3DDB6`.
Derivati VFX: Sole `#FFE6B8`; ambiente giorno `#F3DDB6`/`#E2C49C`/`#8C6A50`; notte luna `#C9A98A`, ambiente `#8C7880`/`#7C665C`/`#4A3C35`, lanterne `#FFB65C`; polvere `#B8906A`.
Moneta: lira/gettone di Solara con faro inciso, oro `#F2B53A`, bordo `#3B2A20`. Upgrade: giallo sole `#F4C430`, nucleo/scintille limone `#F7E26B`.

## Valori chiave
- **Sole**: `#FFE6B8`, 1.05, euler (50, -35, 0) → luce da alto-sinistra come nel concept. Nello shader luce diretta ×0.82, ambiente ×0.25 (margine in LDR).
- **Notte**: luna 0.5, lanterne 1.7 (luce principale sui personaggi), falloff morbido `smoothstep(0, 0.6)`, peso 0.6.
- **Fog** lineare `#F3DDB6` 45→140 m (torre GrayBox leggibile).
- **Toon**: 3 bande, softness 0.06, tinta ombra `#9E7873` @0.55, rim crema 0.25, bordo ombra `smoothstep(0.3, 0.7)` sull'attenuazione (mantiene il PCF), outline `#3B2F2A` 0.012 m solo su `M_Char_Atlas` e `M_Props_Interactive`.
- **Variazione pavimento/muri**: `_NoiseStrength` 0.06, `_NoiseScale` 0.35/m su `M_Diorama_Atlas` (2 ottave di value noise, solo ALU, 0 = spento).
- **Ombra di contatto**: quad 2 tri, `#3B2F2A` alpha 0.45, unlit trasparente, niente SSAO.
- **Post (Giorno/Notte)**: Tonemapping Neutral; Bloom soglia 0.92/0.8, intensità 0.25/0.6, scatter 0.55, quarter-res, tinta crema; Color Adjustments esposizione -0.1/+0.45 (in LDR non ha effetto: la notte si schiarisce col colorFilter ×1.12), contrasto 6/8, saturazione +4/-6; White Balance +4/+10; Vignette `#3B2F2A` 0.20/0.26. LDR.
- **Particelle** (max vive): pop 10 (16), monete 9 + bagliore (12), upgrade ~16 scintille 0.11–0.21 m + anello pulsante (25), polvere 3/m, 0.18–0.32 m (8). Unlit, un solo materiale, niente ombre/collisioni/luci/trail.

## Tier di qualità
| | Base | Standard | Alto | **Web** |
|---|---|---|---|---|
| Render scale | 0.6 | 0.75 | 1.0 | 0.9 + cap devicePixelRatio ≤ 1.25 nel template (`config.devicePixelRatio = Math.min(window.devicePixelRatio, 1.25)`) |
| Ombre | no | no | soft (Low, 4 tap), 26 m, 2048, 1 cascata | soft (Low), 24 m, 1024, 1 cascata |
| Outline | no | no | solo `_OutlineOn=1` | solo `_OutlineOn=1` |
| Post-process | sì | sì | sì | **OFF** (opt-in `UM_QualityTier.AllowPostOnWeb`, solo dopo verifica in build) |
| Bloom | no | sì | sì | – (post spento) |
| Particelle | ×0.5 | ×0.75 | ×1 | ×0.75 |
| MSAA / AA | – | FXAA | 2x | MSAA 2x (niente FXAA: passa dal post) |
| HDR | off | off | off | off |
**Distanza ombre**: si misura dalla camera. Con camera ortografica conviene `CameraRigSetup.Distance = 15` (oggi 30): così 24–26 m coprono l'area di gioco e la shadowmap non spreca risoluzione; con Distance 30 le ombre sparirebbero quasi tutte. Da decidere col Tech Lead.

## Note WebGL
- Shader `#pragma target 3.0`, niente compute/geometry/tessellation/UAV, solo keyword URP standard; il value noise è puro ALU (nessuna texture in più).
- Post-process spento su WebGL: `UM_QualityTier.Apply(UMTier.Web)` imposta `renderPostProcessing = false` e AA None sulle camere; nessun Volume viene pesato. Riattivarlo solo con `AllowPostOnWeb = true` dopo una build verificata senza magenta.
- Il renderer `URP_Mobile_Renderer` creato da `ProjectSetup` aveva `postProcessData` nullo: **il post-process non girava** (neanche in main). `UM_PostProcessBuilder` ora lo assegna.
- Per il target WebGL `com.unity.modules.vibration` nel manifest non si risolve: va tolto o reso per-piattaforma (Tech Lead).
- La verifica headless gira su OpenGL Core (llvmpipe), non su un vero browser: la resa WebGL2 va confermata con una build quando sarà consentita.

## Verifica headless
`xvfb-run -a Unity -batchmode -buildTarget WebGL -projectPath <worktree> -executeMethod UltimoMiglio.VFX.EditorTools.UM_VfxVerify.Run -quit` (senza `-nographics`). Output in env `UM_VFX_OUT` (review: `/workspace/ultimo-miglio/art/vfx/`).

## Workflow skill (09_SPRINT1)
Con Unity-MCP attivo: `script-update-or-create` per i `.cs`, `script-execute` per i builder, `rql-mutate` per ritocchi live, `screenshot-isolated`/`screenshot-camera`/`screenshot-game-view` per la review del Concept Artist, `package-add` solo se manca URP.

## Changelog
- v1.0: shader toon, luci, post, 4 particelle, builder.
- v1.1 (Concept Artist): fog più leggera, notte leggibile, outline per materiale + `M_Props_Interactive`, moneta lira di Solara, colori upgrade.
- v1.2: tier Web, materiale moneta su atlas, verifica headless, note WebGL.
- v1.3: giorno meno sovraesposto, sprite Kenney CC0, `M_FX_Coin` su ToonDiorama, lanterne morbide.
- v1.4 (review 2 Concept Artist): lanterne 1.7 con falloff morbido (luna 0.5); polvere `#B8906A` con alpha più persistente; scintille upgrade +30% con nucleo limone `#F7E26B` e alone `#F4C430`; ombre filtrate (soft Low + bordo toon smoothstep), distanze/risoluzioni per tier; variazione world-space sul set; ombra di contatto `fx_contact_shadow`; assegnato `PostProcessData` (il post non era attivo); esposizione notte +0.45 per il rapporto notte/giorno.
- v1.5 (sprint game feel): `fx_package_pickup`, `fx_package_deliver`, `fx_upgrade_complete`, `fx_coin_flyer` + API `FlyCoins*`, `fx_pad_fill_ring` + `UM_PadFillRing.SetProgress`; shader `UM_FxRingFill`; notte schiarita col colorFilter (postExposure ignorato in LDR).
- v1.6: cap. 1 senza notte → `UM_DayCycle` (giorno → tramonto) + `UM_Lantern`; `UM_LightingRig.SetGoldenBlend`; tier Web con post OFF e MSAA 2x; screenshot con camera 15/4.5; `vfx_night.png` sostituito da `vfx_sunset.png`.

## v2.0 – Look premium + acquisto (ricostruito dopo la perdita di wt/vfx)
- `UM_ToonDiorama` premium: ombre colorate viola (`_ShadowTint` 0.55/0.50/0.72), gradiente in altezza (`_HeightDark`), AO alla base dei muri e di vertice (alpha del vertex color), highlight facce superiori (`_TopLight`), rim fresnel (fresco dal lato ombra), variazione piastrelle (`_TileVar`), vignetta a terra e grading in-shader (tonemap morbido, contrasto 1.14, saturazione 1.18, glow 0.35) pilotati da `UM_LookSettings`. Funziona con post-processing OFF (WebGL).
- Nuovo `UM_SkyGradient.shader` + `UM_LookSettings`: cielo a gradiente dietro la scena, giorno → tramonto seguendo `UM_DayCycle`.
- Luce: sole `#FFD08A` intensità 1.3 (tramonto `#FF9F5A` 1.0), ambiente cielo fresco `#9FBCD8`, nebbia `#F6D7B0`.
- Nuovo effetto `fx_purchase` → `UM_FxPlayer.Play(UM_FxPlayer.Fx.Purchase, pos)`.
- `fx_package_deliver` più grande (coriandoli +40%, anello 2.3 m, stella 1.0).
- **Materiali e prefab si generano in Unity**: menu *Ultimo Miglio/VFX/Build FX Prefabs + Materials* e poi *Ultimo Miglio/VFX/Setup Scene* (`UM_VfxSceneSetup.Setup`), che cabla UM_FX (FxPlayer + LookSettings) e UM_DayCycle sul sole. Salvare la scena.

## v2.1 – Colori UI/UX esatti
- Pacco / `Fx.PackagePickup` = teal `#5E7F86` (particelle + anello a terra nuovo).
- Moneta / `Fx.Purchase` e riempimento durante il pagamento = sole `#F4C430`.
- Cliente / `Fx.PackageDeliver` = terracotta `#C8633B` (coriandoli 60% terracotta + crema, anello terracotta).
- Upgrade = sole `#F4C430` (alternativa `UM_Palette.UiMenta` `#8FC9A8`).
- `UM_PadFillRing.kind` (`Purchase`/`Upgrade`/`Pickup`/`Delivery`) e `SetKind(kind)`: imposta il colore UI su pulsazione e riempimento (via MaterialPropertyBlock, un solo materiale). Default `Purchase` = sole.
- Costanti in `UM_Palette`: `UiPacco`, `UiSole`, `UiCliente`, `UiMenta`. Rigenerare i prefab (*Ultimo Miglio/VFX/Build FX Prefabs + Materials*).

## v2.2 – Icone fluttuanti + colori Lorenzo (17:28)
- Nuovo `UM_PadIcon` su ogni oggetto pad: billboard a 1.6 m, bob 0.12 m / 1.2 s, pulsazione scala, disco colorato + glifo.
  - API: `SetKind(Kind)`, `SetIcon(Texture2D)`, `SetPlayerNear(bool)`, `SetVisible(bool)`, `AssignDefaultIcon()` (editor).
  - Icone UI/UX per path: `Assets/_Game/UI/Icons/ic_pad_{pickup,buy,deliver,upgrade}_white.png` (non copiate; se mancano, glifo procedurale).
  - Spazio sopra l'icona lasciato libero per etichette/prezzi UI.
- Colori (correzione Lorenzo): Pickup teal `#5E7F86`; Delivery menta `#8FC9A8` (NO terracotta); Purchase sole `#F4C430`; Upgrade limone `#F7E26B` (distinto dalla consegna).
- `UM_PadFillRing.ColorOf` e `fx_package_deliver` aggiornati (coriandoli menta + crema, anello menta).
- Setup: aggiungere `UM_PadIcon` a ciascun pad; *Ultimo Miglio/VFX/Setup Scene* assegna le icone UI/UX presenti.

## v2.1 — Issue #48: luce e palette «Diorama Solare» (senza post-processing)

Riferimento: `docs/concept/GUIDA_VISIVA_CAP1.md` §2 Palette, §9 Illuminazione, §11 Camera. Tutto il look viene da luci, materiali e shader (WebGL: post OFF, niente magenta).

**Da eseguire in Unity (una volta, scena gameplay aperta):** menu `Ultimo Miglio/VFX/Applica Look #48 (luce + palette + camera)` → salvare scena e asset.

| Area | Prima | v2.1 |
|---|---|---|
| Sole | `#FFD08A` 1.3 | `#FFE6B8` 1.35, euler (50, −35) |
| Ambient trilight | cielo `#9FBCD8` (azzurro) | cielo `#C9B4A6` / equatore `#D8B898` / terra `#9E7873` |
| Tinta ombre (shader) | lavanda (0.55,0.50,0.72) | `#9E7873` + `_ShadowCool` 0.12 (lieve, mai blu/viola) |
| Grading in-shader | sat 1.18, contr 1.14, lift viola | sat 1.12 (+12%), contr 1.18, lift `#5A4A55` |
| Vignetta a terra | viola, 0.35 | `#9E7873`, 0.25 |
| Cielo/fondo | top azzurro | crema `#F3E3C3` → pesca `#F6D7B0` → tufo `#E9D2A8`; tramonto `#E8B48A`/`#E8913A`/`#F4B88E` |
| Pavimento | atlas arancio ×tint, celle 1 m | lastre 0,25 m: `#F3E3C3`/`#E8D3AC`/`#E9D2A8` per cella, var 0.04, fuga 0.12 (`_FloorBaseMix`=1) |
| Ombre URP | 26 m, 1024 (Web), PCF Low, bias 1/1 | 30 m, 2048, 1 cascata, PCF Medium, bias 0.6/0.7, bordo shader `_ShadowSoft` 0.35 |
| Camera | ortho portrait 13.5, distanza 40 m | ortho portrait 7 (landscape 6.5, min 5.5), distanza 20 m, pref `cam_zoom_v2` |
| Nebbia | 45–140 m | 30–90 m dalla scena (= camera 20 m + 30..90) |

Perché la scalettatura: la shadow map copre l'intervallo [near, shadowDistance] dalla camera; con la camera a 40 m e distanza 24 m le ombre erano quasi tutte nel vuoto/bordo e i texel enormi. Camera a 20 m + 30 m + 2048 → texel ~1,7 cm; il PCF Medium resta filtrato perché lo shader non binarizza più l'attenuazione (smoothstep 0.15–0.85).
