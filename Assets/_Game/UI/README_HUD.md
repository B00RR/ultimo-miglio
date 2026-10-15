# HUD "Diorama Solare" (UI Toolkit) – Ultimo Miglio, Sprint 1
Owner: UI/UX. Fonte: `04_UI.md` v3.2 (Pedaggio, rate) + `02_TECH.md` v1.3 §2b. Dati: `data/ch1/*.json` via `ChapterDataAsset` del Tech Lead.

## Struttura
```
_Game/UI/
  Runtime/   (asmdef UltimoMiglio.UI → Core, InputSystem)   namespace UltimoMiglio.UI
    HudController.cs     overlay: valute, telefono, chip Pedaggio, etichetta stanza, toast, scheda Cassaforte, popup incasso
    PhoneController.cs   telefono: back stack, Indietro Android, Home/Ordini/Messaggi/Flotta (sola lettura) + "Mostra la strada"
    FloatingJoystick.cs  joystick a schermo intero (EnhancedTouch, dp, deadzone 8, raggio 56, base trascinata, fade 150 ms, 24 dp bordi)
    EdgeArrow.cs         freccia fuori schermo clampata alla safe area (margine 24 dp) + marker quando visibile
    SafeAreaUtil.cs, HudFormat.cs (numeri it-IT: 1.240 / 8,5 / 1,2K)
    Data/HudConfig.cs    IHudConfig + ChapterDataHudConfig (legge debt/offline da ChapterData; nessun numero di fallback)
    World/ ZoneFillRing (_Fill via MaterialPropertyBlock), WorldAnchoredElement, UpgradePadCard, StackCounter (n/cap, MAX, chip buff), CassaCard (PIENA, x2)
  Bridge/    (asmdef UltimoMiglio.UI.Bridge → Gameplay)  HudGameBridge (GameRoot → HUD), RoomDoorway (trigger → etichetta stanza)
  Editor/    HudSetup.cs  menu "Ultimo Miglio/UI/Build HUD" → Settings/*.asset, Prefabs/HUD.prefab, Demo/HudDemo.unity
  Demo/      HudDemo.cs (tasti 1-7, P, Esc) – valori mockup SOLO per anteprima
  UXML/ Hud, Phone, PadCard, StackCounter, CassaCard      USS/DioramaSolare.uss
  Shaders/UM_ZoneRing.shader  anello fallback con _Fill/_Color/_Pulse (il VFX può sostituirlo mantenendo le proprietà)
  Icons/ ic_*.png (96 px, bruno + _cream), sorgenti in Icons/Source~ (ignorata da Unity; `python3 build_icons.py`)
```

## Integrazione (Tech Lead)
1. Aprire il progetto (o batchmode): `Unity -batchmode -quit -nographics -projectPath unity -executeMethod UltimoMiglio.UI.EditorTools.HudSetup.BuildAll`
   → crea `Settings/HudPanelSettings.asset` (Scale With Screen Size 1170×2532, match width) e `PhonePanelSettings.asset` (sort 10), `Prefabs/HUD.prefab`, `Demo/HudDemo.unity`.
2. **Font:** importare Inter (OFL) SemiBold/Bold/ExtraBold come Font Asset (TextCore) e Roboto come fallback; assegnarli nel `PanelTextSettings` delle due PanelSettings (o `-unity-font-definition` nel `:root` della USS). Attivare numeri tabulari (feature `tnum`) se disponibili.
3. Nella scena slice: istanziare `HUD.prefab`; **usare il suo `FloatingJoystick`** come `NinaController.Joystick` (al posto di quello creato vuoto in `SliceSceneBuilder` riga ~64: funziona anche così, ma senza grafica e senza esclusione degli overlay). Impostare `cameraTransform` del joystick (o lasciare yaw fisso 45°) e `cam`/`nina` di `EdgeArrow`.
4. Aggiungere `HudGameBridge` (riferimenti Hud/Phone/Joystick/StackCounter/CassaCard/EdgeArrow/Cassa). `StackCounter` va su Nina (template `UXML/StackCounter.uxml`, offset ~2.2 m), `CassaCard` sulla Cassa, `UpgradePadCard` su ogni pad (template `PadCard.uxml`).
5. `ProximityZone.Ring` usa già `UltimoMiglio.UI.ZoneFillRing`: mettere il componente sul quad dell'anello con materiale `UltimoMiglio/UI/ZoneRing` (o shader VFX con `_Fill`).
6. `RoomDoorway` (trigger) su ogni varco con il nome stanza.
7. Android: `renderOutsideSafeArea = true` è già in `ProjectSetup`; la safe area viene riapplicata a ogni cambio (`Screen.safeArea`).

## API principali
- `HudController`: `SetCurrencies(ln, bulloni, gettoni?)`, `PulseCurrency()`, `SetPhoneVisible(bool)`, `SetPhoneBadge(n)`, `RingPhone()`, `ShowPedaggio(fraction?)` / `ShowPedaggio(fraction, preview)` / `HidePedaggio()`, `ShowRoomLabel(name)`, `ShowToast(text)`, `ShowOfflineToast(ln)`, `ShowSafeCard(SafeCardData)` / `HideSafeCard()`, `ShowCollectPopup(screenPos, netLn, tollFraction)`, `IsOverInteractive(panelPos)`, `Config` (IHudConfig).
  Eventi: `PhoneButtonTapped`, `PedaggioTapped` (→ `ShowSafeCard` + `EdgeArrow.SetTarget(cassaforte)`), `SafeCardClosed`.
- `PhoneController`: `OpenPhone(app)`, `Close()`, `Back()`, `HandleBack()`, `UnlockApp`, `SetLockChapter`, `SetBadge`, `SetHome`, `SetOrders`, `SetChat`, `SetFleet`. Eventi: `Opened`, `Closed`, `AppShown`, `ShowTheWayRequested(app, id)`, `QuickReplyChosen(contact, idx)`.
- `FloatingJoystick`: `Value`, `WorldDirection`, `IsActive`, eventi `Moved`, `Released`. Si rilascia da solo all'apertura del telefono (Nina si ferma; il gioco non va in pausa).
- `EdgeArrow`: `SetTarget(transform, "Cliente al Bancone", "icon-customer")`, `Clear()`.
- World: `UpgradePadCard.SetProgress(paid,total)/SetLocked(cap)/SetTitle`, `StackCounter.SetCount(n,cap)/SetBuff(icon,sec)`, `CassaCard.SetAmount(ln,cap)/ShowDoublePrompt`, `ZoneFillRing.SetFill/Drain/PulseOnce`.

## Indietro Android
`PhoneController` legge `Keyboard.current.escapeKey` (Indietro su Android con Input System): app → Home → chiuso. A telefono chiuso **non** consuma l'evento: il gioco deve chiedere conferma prima di uscire (mai uscita diretta). Se il TL ha un gestore Back centrale, chiamare `phone.HandleBack()` e disattivare il polling (o tenerlo: è idempotente solo se un unico gestore agisce).

## Hook richiesti al Tech Lead (non ho modificato file vostri)
Il core non espone ancora: **Bulloni** (passo 0 al momento), **stato mora / TollState** (→ `ShowPedaggio`/`HidePedaggio`, tag "-15%" su `ShowCollectPopup`), **DayClock** (giorno N/365 per scheda Cassaforte e Home), **evento LN incassate con posizione** (popup), **pagamento rata** (scheda), ordini/chat/robot per il telefono. Proposta: eventi C# su `EconomyCore`/`GameRoot` (`OnLnCredited(gross, net, worldPos)`, `OnMoraChanged(bool)`, `DayChanged(int)`), il bridge li inoltra.
SliceSceneBuilder: sostituire la creazione del joystick vuoto con l'istanza di `HUD.prefab` (hook segnalato, non applicato).

## Regole
- Nessun numero di bilanciamento nel codice UI: prezzi, capienze, rate, percentuali arrivano da `ChapterData`/`EconomyCore` o dal chiamante. I tempi UX (1,5 s etichetta, 2,5 s toast, 150 ms fade, 24 dp) vengono da 04_UI e sono campi serializzati.
- Gioco mai in pausa; nessun pulsante d'azione in gameplay; CTA solo nel telefono.

## WebGL / responsive (15/10, v2)
- `ResponsivePanel` su entrambi gli UIDocument: clona la PanelSettings a runtime, ConstantPixelSize con scala dal lato corto: portrait `min(W/1080, H/1920)`, landscape `H/1200` (clamp 0,42-2,5). Classi root `um-portrait` / `um-landscape`. USS riferita a 1080x1920.
- Layout: flexbox ancorato agli angoli (top-bar sx/dx, `top-center` per l'etichetta stanza, `bottom-dock` per la scheda Cassaforte con width 92% / max 780). Elementi contestuali nascosti di default in UXML; i controller fanno bind con retry (prima il tree poteva non esistere → tutti i pannelli visibili insieme in WebGL).
- Telefono: disegnato a 900x1600 (9:16) dentro `#phone-frame`, scalato uniformemente a max 90% altezza / 94% larghezza → colonna centrata (pillarbox) su desktop. TAB apre/chiude, ESC = Indietro (app → home → chiuso). Hint "TAB" sotto il pulsante solo su desktop.
- Input desktop: `FloatingJoystick` legge WASD/frecce e click-drag del mouse nello stesso `Value`/`WorldDirection` del touch (niente TouchSimulation → niente doppio input sui browser touch).
- Verifica: `xvfb-run -a Unity -batchmode -projectPath <copia> -executeMethod UltimoMiglio.UI.EditorTools.HudCapture.Run` → `Captures/hud/hud_<W>x<H>[_phone].png` (solo UI, sfondo trasparente, tutti i contestuali accesi). Opzione `-hudSizes 1920x1080,...`.
- Icone: 24 Lucide (ISC) + 3 originali; vedi `Icons/Source~/build_icons.py` e LICENZE_ASSET.md.
