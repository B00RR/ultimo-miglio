# VFX & Technical Art – Sprint 1 (Capitolo 1) – Ultimo Miglio
Unity 6 + URP, Android. Stile "Diorama Solare" (00_BIBBIA, conceptA.jpg, 02_TECH §4-5, 3d/05_3D_PIPELINE).
Cartella: `Assets/_Game/VFX/` (segue la convenzione esistente `Assets/_Game/...` del progetto; se il Tech Lead sceglie `_Project`, spostare la cartella e aggiornare i path costanti in `Scripts/Editor/*.cs`).

## Contenuto
| File | Cosa |
|---|---|
| `Shaders/UM_ToonDiorama.shader` | Toon URP: rampa 2-3 bande morbide (half-lambert), ombra calda (mai nero), rim crema, ombre ricevute (bordo netto), fog, luci add. per-vertex/pixel, outline inverted-hull (pass `SRPDefaultUnlit`), ShadowCaster/DepthOnly propri. Un solo `UnityPerMaterial` → SRP Batcher compatibile. |
| `Scripts/Runtime/UM_Palette.cs` | Hex palette (base + derivati luce/FX). |
| `Scripts/Runtime/UM_LightingRig.cs` | Sole, gradiente ambiente Trilight, fog lineare; preset Day/Golden/Night + `SetNightBlend(t)` per il ciclo giorno. |
| `Scripts/Runtime/UM_QualityTier.cs` | Lato VFX dei 3 tier di 02_TECH (Base/Standard/Alto): render scale, ombre, outline, bloom, FXAA, scala particelle. |
| `Scripts/Runtime/UM_FxFactory.cs` | Costruzione procedurale ParticleSystem: package pop, coins, upgrade, dust. |
| `Scripts/Runtime/UM_FxPlayer.cs` | Pool round-robin: `UM_FxPlayer.Play(Fx.Coins, pos)`. |
| `Scripts/Editor/UM_FxPrefabBuilder.cs` | Crea `Materials/M_FX_Particles`, `M_Diorama_Atlas`, `M_Diorama_Atlas_Emissive`, `M_Char_Atlas` e `Prefabs/fx_*.prefab`. |
| `Scripts/Editor/UM_PostProcessBuilder.cs` | Crea `Profiles/PP_Diorama_Day/Night.asset` + Volume globale in scena. |
| `Scripts/Editor/UM_LightingSetup.cs` | Sole + rig in scena, clear color Cielo, impostazioni ombre mobile sull'URP asset. |
| `*.asmdef` | Assembly runtime/editor (riferimenti URP). |

## Installazione
1. Pacchetti: `com.unity.render-pipelines.universal` (incluso nel template URP). Nessun pacchetto a pagamento; VFX Graph NON usato (Shuriken = più sicuro su GLES3/low-end).
2. Copiare/lasciare `Assets/_Game/VFX` nel progetto; aprire Unity (o batchmode) per compilare.
3. Menu **Ultimo Miglio/VFX/**: `Build FX Prefabs + Materials` → `Build Post-Process Profiles` → `Setup Warm Lighting (open scene)`.
   Batchmode: `-executeMethod UltimoMiglio.VFX.EditorTools.UM_FxPrefabBuilder.BuildAll` (idem `UM_PostProcessBuilder.BuildAll`; il setup luci richiede una scena aperta).
4. Camera: Post Processing ON nel componente URP Camera. Al boot il Tech Lead chiama `UM_QualityTier.Apply(tier, toonMats)`.
5. Atlas palette atteso in `Assets/_Game/Art/Textures/T_Palette_Atlas.png` (ASTC 4x4/ETC2, no mip, Point, sRGB); mesh moneta opzionale `Assets/_Game/Art/FX/fx_coin.fbx` (24-48 tri, faro inciso via swatch/normal faceting, bordo `#3B2A20`: richiesta al 3D Artist). Path modificabili nel builder.

## Palette (05_3D_PIPELINE + conceptA)
Terracotta `#C8603A` · Giallo sole `#F2B53A` · Menta `#8FC9A8` · Limone `#F4E04D` · Teal OmniFreight `#4E7F86` · Tufo `#D9C4A0` · Pavimento `#E6D2B0` · Legno `#9A6A42` · Cartone `#C99A62` · Scuro caldo `#3B2F2A` · Arancio Nina `#E5743A` · Crema `#FFF4D6` · Cielo `#F3DDB6`.
Derivati VFX (proposta, da validare): Sole `#FFE6B8`, ambiente sky/equator/ground `#F3DDB6`/`#E2C49C`/`#8C6A50`; notte luna `#C9A98A` int 0.5, ambiente `#7E6A72`/`#6E5A52`/`#41342E`, lanterne `#FFB65C` (luce principale sui personaggi). Esposizione notte ≈45-50% del giorno, mai horror.
Moneta: lira/gettone di Solara con faro inciso, oro `#F2B53A`, bordo `#3B2A20`. Upgrade: giallo sole `#F4C430`, highlight/scintille limone `#F7E26B`, anello morbido con pulsazione lenta.

## Valori chiave
- **Sole**: `#FFE6B8`, 1.05 (direct ×0.82, ambient ×0.25 nello shader per headroom LDR), euler (50, -35, 0) → luce da alto-sinistra, ombre in basso a destra come nel concept. Ombre Hard (toon), strength 0.6; Soft solo tier Alto.
- **Ombre URP**: distanza 25 m, 1 cascata, shadowmap 1024, bias 0.05/0.4; Base/Standard = niente ombre (02_TECH).
- **Fog** lineare `#F3DDB6` 45→140 m, leggera (foschia calda sulla torre OmniFreight).
- **Toon**: 3 bande, softness 0.06, shadow tint `#9E7873` @0.55 (moltiplicato: le ombre restano calde), rim crema 0.25 pow 3.5, outline `#3B2F2A` 0.012 m: toggle per materiale `_OutlineOn` (ON in `M_Char_Atlas` e `M_Props_Interactive` per pacchi/furgone/pad; OFF in `M_Diorama_Atlas`), sempre solo tier Alto.
- **Post (Day/Night)**: Tonemapping Neutral (ACES sposta i toni della palette); Bloom thr 0.92/0.8, int 0.25/0.6, scatter 0.55, quarter res, 3 iter, tint crema; Color Adj exposure -0.1/0.0, contrast 6/8, sat +4/-6, WB +4/+10, filtro caldo; White Balance +8/+12; Vignette `#3B2F2A` 0.20/0.26. HDR off (LDR). Bloom off su Base.
- **Particelle** (max vivi): pop 10 (16), monete 9+glint (12), upgrade ~16+anello (25), polvere 3/m (8). Tutte unlit, 1 materiale condiviso, niente ombre/collisioni/luci/trail; scala ×0.5 Base, ×0.75 Standard.

## Workflow skill (09_SPRINT1 "SKILL DA USARE")
Appena Unity-MCP è live:
- `script-update-or-create` per scrivere i `.cs` (validazione Roslyn) invece della copia su disco.
- `package-add` solo se manca URP (`com.unity.render-pipelines.universal`).
- `script-execute` per lanciare `UM_FxPrefabBuilder.BuildAll()`, `UM_PostProcessBuilder.BuildAll()`, `UM_LightingSetup.Setup()`.
- `rql-mutate` per ritocchi valori live (es. `objects | get components | where $ is Light | set intensity = 1.1`).
- `screenshot-isolated` (prefab FX, materiali su `env_`/`chr_`), `screenshot-camera`/`screenshot-game-view` per la review del Concept Artist.

## Changelog
- v1.1 (feedback Concept Artist 15/10): fog alleggerita (45→140 m); notte più leggibile (sole 0.5, ambiente schiarito, post exposure 0, sat -6, vignette 0.26); outline per-materiale `_OutlineOn` + nuovo `M_Props_Interactive`; 3 bande confermate; moneta lira Solara (oro/bordo marrone); upgrade #F4C430/#F7E26B, anello morbido pulsante; nuovi colori in `UM_Palette`.

## Target WebGL (priorità attuale, ordine di Lorenzo)
- Unity lanciato con `-buildTarget WebGL` (nessuna build). Shader `UM_ToonDiorama` compatibile WebGL2/GLES3: `#pragma target 3.0`, niente compute/geometry/tessellation/UAV, solo keyword URP standard (main shadows, soft shadows, additional lights, lightmap, fog, instancing). Particelle Shuriken (niente VFX Graph, che richiede compute → non disponibile su WebGL).
- LDR, niente MSAA (FXAA), bloom quarter-res a 2 iterazioni: il fill-rate del browser è il collo di bottiglia.
- Rimuovere `com.unity.modules.vibration` dal manifest per target WebGL (non risolve; haptics Android da reintrodurre col target Android).

### Tier `Web` (`UM_QualityTier.Apply(UMTier.Web, mats)`)
| Voce | Valore |
|---|---|
| Render scale | 0.9 + cap devicePixelRatio ≤ 1.5 nel template WebGL (`config.devicePixelRatio = Math.min(window.devicePixelRatio, 1.5)`) |
| Ombre | Hard, distanza 15 m, 1 cascata, shadowmap 512 |
| Outline | ON solo su materiali con `_OutlineOn=1` (`M_Char_Atlas`, `M_Props_Interactive`) |
| Bloom | ON, quarter-res, 2 iterazioni, no HQ filtering |
| Particelle | ×0.75 |
| HDR / MSAA | off / off (FXAA) |

## Moneta
`M_FX_Coin` (URP Particles/Unlit opaco, `_BaseMap` = `T_Palette_Atlas`): le monete mesh (`fx_coin.fbx`, LOD1 34 tri) campionano l'atlas con i propri UV, colore particella bianco → hex dell'atlas esatti. Senza mesh: billboard oro.

## Verifica headless
`UM_VfxVerify.Run` (Editor): esegue i 3 builder, crea una scena test, renderizza PNG (giorno, notte, toon outline vs set, 4 FX) e misura la luminanza media. Serve contesto GL: `xvfb-run -a Unity -batchmode -buildTarget WebGL -projectPath <worktree> -executeMethod UltimoMiglio.VFX.EditorTools.UM_VfxVerify.Run -quit` (senza `-nographics`). Output: env `UM_VFX_OUT`.

- v1.2: tier Web, materiale moneta atlas, verifica headless, note WebGL.
- v1.3 (verifica headless 15/10): giorno meno sovraesposto (luma 0.91→0.73), luci lanterna morbide, sprite FX da Kenney Particle Pack CC0 (`Textures/T_FX_Sheet.png`, 2x2: punto, sbuffo, stella, anello; vedi LICENZE_ASSET.md), `M_FX_Coin` = ToonDiorama + atlas (era nero col materiale particelle). Misura notte/giorno 43.8%.
