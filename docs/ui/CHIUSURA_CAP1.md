# UI del capitolo 1 — chiusura (23/10/2026)

Documento di chiusura per la issue #282. Solo fatti presenti nel repo a questo ramo. Nessun segreto.

## HUD di gioco

Radice `hud-root` con classe `hud--minimal` in `Assets/_Game/UI/UXML/Hud.uxml`. Fogli: `DioramaSolare.uss`, `Tokens.uss`, `HudV2.uss`, `Settings.uss`, `LegnoCartone.uss`, `TutorialHud.uss`, `Onboarding.uss`, `Etichette.uss`.

In vista durante la partita:

- Pillola LN e giorno (`currency-pill`, `ln-value`, `day-value`). Il saldo lungo usa `HudFormat.Amount` (999999 → «999,9K»).
- Riga obiettivo (`tutorial-objective`).
- Joystick (`joystick-base` / `joystick-knob`), anche da tastiera.
- Ingranaggio impostazioni (`settings-button`).
- Pulsante «Salta tutorial» (`skip-tutorial`), a sinistra dell'ingranaggio.
- Freccia a bordo schermo (`edge-arrow`) quando il bersaglio è fuori. L'angolo è la direzione sullo schermo da Kaia al bersaglio, oppure alla tappa `via` della guida (#273).
- Fumetto di Nonna Ada e suggerimenti `onb.*` (pillola terracotta sopra il joystick). Combo solo «+N» sopra Kaia, 1,5 s.

Nascosti nell'HUD minimo: telefono in gioco (il nodo `phone-button` c'è nel documento; il runtime del capitolo 1 non lo usa come superficie di gioco, issue #64), pedaggio, bulloni, etichetta stanza.

## Prezzo sui pad

`InteractionPoint` disegna l'insegna di noce del prezzo solo sui pad di acquisto (`Etichette.uss`, classi `insegna-noce` e `poi-chip__valore`). Font Asset Baloo 2 per il valore. Con Kaia entro il raggio del disco la colonna si sposta oltre il bordo, sul lato opposto alla camera, in 0,15 s (`PricePillLayout`, #272). A riposo resta la proiezione storica a 1,55 m (1,05 m se il pad è nel 28% alto dello schermo).

## Onboarding

`TutorialManager`: 16 passi, da `coldOpen` a `freePlay`, testi per id. `HudReveal` nasconde e mostra l'HUD sulle partite nuove (`RivelaGraduale`). Aiuti `OnboardingHints`: 10 s biglietto, 20 s freccia o mano, 40 s camera. «Salta tutorial» chiude il percorso guidato. Il debito parte dopo la carta e un versamento vero alla Cassaforte.

## Impostazioni

Pannello `settings-root`, aperto dall'ingranaggio (`SettingsController`). Slider Zoom (0,7–1,4), Rotazione (−45…45), Inclinazione (35…60), Ripristina vista, Musica e Suoni. Durante il tutorial rotazione e inclinazione sono bloccate e lo zoom resta fra 0,85 e 1,2. Chiavi `CameraView` (`cam_zoom`, `cam_rot`, `cam_tilt`, `cam_ver`) e `vol_music` / `vol_sfx`.

## Palette e font

Token in `Tokens.uss`: terracotta `#C8633B`, sole `#F4C430`, menta `#8FC9A8`, limone `#F7E26B`, carta `#F3E3C3`, inchiostro `#3B2A20`, noce `#7A4A2E`, legno `#74503A`, outline `#3B2F2A`.

Font: Fredoka (OFL, già in HUD), Baloo 2 Bold e Fredoka Medium aggiunti con #186 (OFL, registro in `docs/LICENZE_ASSET.md`). Inter e Roboto restano nel progetto per testi di sistema. Niente bianco puro sugli angoli dei pannelli: 9-slice RGBA (#185).

## Cosa resta fuori da questa chiusura

- #172 / PR #281: HUD «direzione C» (insegna appesa, cartellino del giorno). Milestone dell'issue: Gioco completo. La PR è a parte: tocca gli stessi `Hud.uxml`, `HudController.cs` e i font Baloo di #186, quindi va riallineata dopo il merge di questa chiusura.
- #64: voce di checklist S04 sul TAB del telefono. Il telefono non è una superficie del capitolo 1.
- #283: tre righe di cronologia già riportate in `STATO_PROGETTO.md` da questa chiusura.
