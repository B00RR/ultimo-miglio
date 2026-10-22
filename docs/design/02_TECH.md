> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# 02_TECH - Bokobay (Tech Lead)
Stato: v1.3, allineato a 01_CORE_LOOP.md v3.3 e data/ch1 (offline alla Cassa), 04_UI.md v3 §5 e qa/07_REVIEW.md. Gioco a controllo diretto (joystick, camera che segue Kaia), non idle. Lancio iniziale SOLO Android (Google Play). iOS fuori dalla v1.

## 1. Engine: Unity 6 (LTS), URP
- **Perché Unity**: ecosistema mobile maturo (Ads/IAP: LevelPlay, Unity IAP, Firebase, AppsFlyer SDK ufficiali), profiling Android solido, toon shading URP ben documentato, pool di sviluppatori e asset (Synty-like low-poly) enorme. Standard di fatto per publisher tipo SayGames/Playstrom: soft launch e SDK di mediazione senza attrito.
- **Godot 4**: ottimo e gratuito, ma plugin ads/IAP/analytics meno maturi, renderer mobile meno ottimizzato su Android di fascia bassa. Valido solo per un prototipo.
- **Altro** (Unreal, Defold, Cocos): Unreal troppo pesante per un hybrid-casual mobile; Defold/Cocos 2D-first, scomodi per diorama 3D.
- Costo: Unity Personal gratuito sotto 200k$ di ricavi annui.

## 2. Architettura di massima
- **Simulazione separata dalla presentazione**: un `EconomyCore` in C# puro (niente MonoBehaviour), deterministico, a tick (es. 0,1 s per zone/pad); l'offline usa lo stesso core: 50% della resa reale dei robot sul tempo trascorso, versata nella Cassa (§4). Le scene leggono lo stato e animano.
- Moduli: Economy (risorse, produzione, upgrade, debito), Logistics (ordini, rotte, robot OLDO/FIZZ/TIKTAK/HUGO/NULO come "produttori" con tasso), DayNight (fase legale / mercato nero, modificatori), Narrative (capitoli gated da debito e flag, dialoghi da dati), Quest (giornaliere Auntie Lulu), Meta (prestigio/eventi), Services (Ads, IAP, Analytics, RemoteConfig, Save).
- **Data-driven**: bilanciamento in ScriptableObject/CSV importati, override via Remote Config per LiveOps senza update.
- Numeri grandi: `double` + formattazione (K, M, B...); passare a BigDouble solo se il design prevede >1e300.
- Event bus leggero + UI Toolkit o uGUI con prefab; un'unica scena diorama con zone sbloccabili (Addressables) anziché tante scene.

## 2b. Gameplay e UI (da 01 v3.1 e 04_UI §5)
- **Input**: `FloatingJoystick` a schermo intero (Input System, `EnhancedTouch`), vince il primo `touchId`; unità in dp (`Screen.dpi/160`), zona morta 8 dp, raggio 56 dp, base trascinata, fade 150 ms. Esclusi i rect degli overlay (`EventSystem.IsPointerOverGameObject`) e 24 dp sui bordi laterali (gesture Indietro). Output ruotato di 45° di yaw nello spazio camera. Azioni solo per prossimità, nessun pulsante.
- **Kaia**: `CharacterController` cinematico, 3 m/s (+60% Scarpe), **-10% a pila MAX**; 0→max in 0,1 s, frenata 80 ms, rotazione slerp ~12/s; collisioni con scaffali/robot, niente pathfinding.
- **Zone**: collider trigger con `ProximityZone` (tipo, tick 0,1 s, permanenza, priorità se sovrapposte). `UpgradePad`: trasferimento LN a tick 0,1 s con curva accelerante, **pagamento a rate salvato** nel save. Anelli di riempimento: shader su quad/decal a terra con parametro `_Fill`. Muri: `OnUnlocked` → crollo + update NavMesh robot + nuovo confiner.
- **Pila (5-30)**: pacchi da pool, offset con inerzia/oscillazione procedurale (no joint); GPU instancing oltre ~20.
- **Robot**: AI Navigation con NavMesh per stanza, macchina a stati (prendi/porta/deposita); max ~15 agenti su tier Standard, 8 su Base; repath diradato.
- **Ronde notturne**: coni di visione = angolo + distanza + raycast ogni 0,1-0,2 s; mesh generata una volta, palette senza neon.
- **Monete a terra**: pool, raccolta magnetica a raggio, merge in un pickup sopra ~50. Le LN offline non generano monete: restano nella Cassa (§4).
- **Camera**: Cinemachine ortografica a 3/4, `Framing Transposer` damping 0,25 s, look-ahead 1,5 m, un `CinemachineConfiner` per stanza, blend 600 ms al trigger porta (+ evento mini-etichetta stanza). **Nessun pan/zoom del giocatore.** Aree lontane in LOD/disattivate.
- **UI**: overlay Screen Space solo per valute (in alto a sinistra), **telefono in alto a destra**, freccia a bordo schermo e card dei pad; pila e anelli in world-space. Freccia: proiezione dell'obiettivo **clampata alla safe area** con margine 24 dp. Telefono = overlay, il gioco non va in pausa (blur solo tier Alto); Indietro Android a stack (app → telefono → niente). Edge-to-edge con WindowInsets/display cutout, notifiche Android 13+ a fine tutorial, TextMeshPro Inter/Roboto, icone SDF. Haptics `VibrationEffect` leggero/medio/doppio.

## 3. Target device
- Portrait 9:16 (safe area per notch), un dito.
- **Solo Android al lancio**: minSdk 26 (Android 8), targetSdk richiesto da Play; IL2CPP ARM64 + ARMv7; Vulkan con fallback OpenGL ES 3.
- **Frammentazione: 3 tier di qualità** (adottata la proposta QA 07 §3, accorpando High e Mid). **Base** (2-3 GB, Mali-G57 MP1, PowerVR GE8320, Adreno 5xx; es. Galaxy A03/A10, Moto G7): 30 fps, render 0,6, niente ombre/outline/blur, 8 agenti, GLES3. **Standard** (3-4 GB, Adreno 610/Mali-G52; es. Redmi 10C/12C): 30 fps, render 0,75, niente ombre, 15 agenti. **Alto** (≥6 GB, Adreno 6xx+/Mali-G68+): 60 fps opzionale, ombre morbide, blur telefono, Vulkan. Tier scelto all'avvio da GPU/RAM, override da Remote Config.
- **Perché 3**: il QA mostra che il pavimento reale (2-3 GB, PowerVR, ARMv7) differisce dal "low" su crash/OOM e driver più che sul framerate; serve un tier proprio. High/foldable/tablet non hanno bisogno di un tier grafico in più (Alto basta, 60 fps cap): restano solo **righe della matrice di test** (120 Hz frame pacing, termica, cambio config, ratio 16:9→22:9 e 16:10). Matrice: 8-10 device reali di 07 §3 + Firebase Test Lab; ANR/crash su Play Console (Android vitals).
- Budget: <150k triangoli a schermo, <100 draw call, texture atlas 2048, **AAB** obbligatorio: base <150 MB, contenuti di zona e capitolo via Play Asset Delivery (install-time/fast-follow/on-demand) o Addressables, avvio a freddo <5 s.

## 4. Salvataggi e offline (Cassa)
- Save locale JSON versionato (con migrazioni), scrittura atomica (file tmp + rename), autosave ogni 30 s e su pausa/background.
- Cloud save tramite **Play Games Services v2** (Saved Games, login automatico, achievement e classifiche), fallback Firebase per chi non accede; conflitti risolti per "progresso maggiore".
- **Offline = Cassa** (01 v3.2): nessuna schermata "Mentre eri via". Al rientro `delta = clamp(now - lastSeen)` (ora server quando disponibile) e incasso = 50% della resa reale dei robot automatizzati nel delta. La Cassa ha capienza pari al 4% della tranche (6% con "Turno di notte") e un limite di sicurezza: l'offline non supera il 20% delle monete del capitolo (data/ch1 v3.3, fonte di verità dei numeri). Le LN restano nella Cassa e Kaia le ritira camminandoci sopra (`ProximityZone`), con un toast di 1 riga. Il raddoppio con ad rewarded è limitato a 2 al giorno (contatore salvato, reset giornaliero su ora server). Droni completano offline, notte e corse no. Tutti i parametri sono in Remote Config, così la v3.3 cambia solo i numeri.
- Anti-cheat orario: confronto con ora server (NTP/backend) quando online; se il clock torna indietro, niente guadagno. Nessun server autoritativo all'inizio. Acquisti con **Google Play Billing Library** (via Unity IAP aggiornato all'ultima versione richiesta da Play), verifica dei token con Play Developer API lato server, acknowledge entro 3 giorni, gestione acquisti pending e rimborsi (Voided Purchases).

## 5. Pipeline asset low-poly "Diorama Solare"
- Blender -> FBX, scala 1 unità = 1 m, pivot a terra, naming `env_`, `prop_`, `bot_`, `chr_`.
- **Colore via palette texture** (atlas 256x256 di swatch terracotta/giallo/menta/limone/teal GrayBox): UV sugli swatch, un solo materiale condiviso -> batching e notte coerente.
- Shader toon URP custom (2-3 bande morbide, rim light, outline opzionale invertito) + luce direzionale unica; notte = swap di LUT/gradiente ambiente + luci calde baked, niente neon.
- LOD 2 livelli per edifici, animazioni robot semplici (rig minimo o animazione procedurale), camera ortografica 3/4 che segue Kaia (vedi §2b), senza pan/zoom.
- Check automatici in import (poly count, materiali, scala).

## 6. Rischi tecnici
1. **Performance Android low-end** con toon shading e luci notturne -> profilare presto su device reale, light baking, limiti di luci dinamiche.
2. **Bilanciamento Cassa offline vs gioco attivo** -> capienza e tempo di riempimento da Remote Config, verificati con test unitari che confrontano resa live e offline sullo stesso core e con `qa/econ_sim_v3.py`.
3. **Corruzione/perdita salvataggi** -> scrittura atomica, backup del save precedente, cloud save.
4. **Peso SDK** (ads, analytics, attribution) su build size e stabilità -> mediazione unica, integrazione progressiva, ANR monitorati (Crashlytics).
5. **Play compliance**: target API level annuale, Data Safety form, policy Families/ads, consenso GDPR UE (UMP), mercato nero da presentare come fiction (rating).
6. **Scope creep** (corse, carichi pesanti, cooperativo, razzo): l'evento cooperativo richiede backend -> rimandarlo dopo il soft launch.

## Prossimo passo (da decidere: Lorenzo)
Proposta di prototipo verticale: diorama 1 zona a joystick, OLDO + clienti, Cassa offline, save, (riferimento giocabile: `demo/assaggio.html`), shader toon su un Android di fascia bassa reale, build AAB su Play internal testing.
