# HUD direzione C — insegna di noce

Anteprime di layout a 1 px = 1 dp (riferimento 390×844, controllo 360×800). Sfondo crema, solo l'interfaccia: il deposito 3D non c'è in questi scatti.

| File | Contenuto |
|---|---|
| `hud_390x844.png` | Ordine aperto (biglietto di cartone) |
| `hud_390x844_icona.png` | Ordine ritratto a icona |
| `hud_360x800.png` | Controllo 360, ordine a icona |
| `hud_360x800_aperto.png` | Controllo 360, ordine aperto |

Le linee tratteggiate dei mockup non fanno parte dell'HUD.

La CI (job `catture`) rigenera gli stessi quattro file da Unity, scena `HudDemo`, pannello a scala 1. Quelle catture sostituiscono queste anteprime Pillow nella PR, così il Concept Artist rivede il rendering UI Toolkit.
