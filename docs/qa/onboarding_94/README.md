# QA anteprima — onboarding graduale UI (#94)

Anteprima HTML (`anteprima/index.html`, stessa base della #78) con le classi di `Onboarding.uss`; testi reali della PR #62. Script: `anteprima/shoot.js` (Playwright, 360×800, 390×844, 412×915, DPR 2); misure in `anteprima/risultati.json`. Sfondo = concept, non la scena vera.

| file | stato |
|---|---|
| `onb_*_01_avvio` | avvio: pillola LN nascosta, nessun pad; restano obiettivo, Ada, suggerimento, joystick |
| `onb_*_02_comparsa_LN` | pillola LN a metà comparsa (scala 0,85, opacità 0,55; animazione 0,6→1 in 250 ms) |
| `onb_*_03_pad_barra` | pad obiettivo: freccia ▼ (#78) → barra LN "32/40" → pillola "8 LN" (Zaino 1 = 40 LN, LN in tasca 0) |
| `onb_*_04_aiuto20_freccia_fuori_camera` | aiuto 20 s, bersaglio fuori camera: freccia a bordo schermo |
| `onb_*_05_aiuto20_mano_bersaglio_a_schermo` | aiuto 20 s, bersaglio già a schermo: mano fantasma nella zona joystick, il suggerimento testuale si nasconde (40 s = camera breve ≤ 1,5 s, non catturabile in HTML) |
| `onb_*_06_badge_nonno` | badge NONNO "6 pacchi/min" (22 dp) |
| `onb_*_08_combo_popup` | combo: solo "+3" sopra Nina (clienti di fila; sale 40 dp, sparisce in 1,5 s). Il moltiplicatore non si mostra; nessun contatore HUD |
| `onb_*_07_salta` | "Salta tutorial" a sinistra dell'ingranaggio, riga obiettivo stretta |

## Esito (24 catture)
| controllo | 360 | 390 | 412 |
|---|---|---|---|
| HUD alto entro la fascia libera (350/2532) | 103/111 ✅ | 103/117 ✅ | 103/126 ✅ |
| Sovrapposizioni fra LN, obiettivo, ingranaggio, Salta, Ada, suggerimento, joystick, mano, freccia, barra, prezzo, badge, combo | nessuna ✅ | nessuna ✅ | nessuna ✅ |
| Obiettivo senza ellissi con "Salta tutorial" visibile ("Assumi NONNO") | ✅ | ✅ | ✅ |


> 04_UI.md non è ancora migrato in questo repo (il repo docs è archiviato): la sezione §4.1 vive qui finché 04_UI non arriva in `docs/`.

## Specifica UI — 04_UI §4.1 Onboarding graduale: stati, tempi, misure (rev. 16/10/2026, issue #94, dati `Assets/_Game/Data/ch1/onboarding.json`, #106)
HUD minimo di Lorenzo: **solo** pillola LN + giorno, riga obiettivo, joystick, pad a terra. Niente telefono, niente card grandi. Unità = dp (pannello di riferimento 360-412 dp di larghezza).

**Stati di ogni elemento bloccato** (`lockedAtStart`): `nascosto` (display none, nessun input) → `comparsa` (250 ms) → `visibile`. API `HudReveal.Hide(id)`, `HudReveal.Show(id)`, `HudReveal.ShowAll()`; evento `Revealed(id, pos)` per camera e log `[Onb] reveal <id>`.
| elemento | comparsa |
|---|---|
| HUD (pillola LN, …) | classi USS `reveal` / `reveal--hidden`: scala 0,6→1 (ease-out-back) + opacità 0→1, 250 ms |
| zone, pad, NONNO (mondo) | SetActive + scala 0,6→1 in 250 ms (ease-out-back) + camera breve |
Nella slice attuale: passo Arrivi → `zona.arrivi`; Scaffali → `zona.scaffaleTerracotta`; Bancone → `zona.bancone`; Monete → `hud.pillolaValute`; Nonno → `pad.assumiNonno`, `robot.nonno.attivo`; Zaino → `pad.zaino`; fine o "Salta" → tutto. `TutorialManager.RivelaGraduale` è **acceso di default per le partite nuove** (nessun passo salvato, oppure partita iniziata già in graduale: chiave `um_onb_graduale`); salvataggi precedenti o tutorial finito = tutto visibile.
**Esclusi per decisione di DevOn (#64):** nessun telefono, nessuna barra debito (il debito si vede solo nella fine giornata e nel fumetto di Ada), nessun contatore combo nell'HUD; nessun id riservato in `HudReveal` per questi.

**Fascia alta (≤ 350 px su 1170×2532 ≈ 111-126 dp):** riga 1 (8-48 dp) pillola LN 40 dp a sinistra, obiettivo 28 dp a destra (13 dp), ingranaggio 32 dp; fumetto Ada 54-106 dp (ritratto 36 dp, testo 14 dp, 1-2 righe, mai ellissi). Fine HUD alto: 103 dp.
**Fascia bassa (≤ 500 px ≈ 158-181 dp):** suggerimento `onb.*` 28 dp a 130-158 dp dal fondo (terracotta, testo 13 dp); joystick a riposo 112 dp centrato a 68 dp dal fondo.

**Pad di acquisto (colonna ancorata 1,55 m sopra il pad):** freccia ▼ 32×26 dp (solo pad obiettivo, oscilla ±3 dp) → barra LN 18 dp (traccia 56×8 dp, riempimento terracotta, testo **pagato/totale** "32/40" 12 dp, Zaino 1 = 40 LN) → pillola prezzo = **residuo** ("8 LN", 18 dp). La barra compare con pagamento iniziato, Nina entro 3 m o aiuto attivo (`ForceBar`). Fondo colonna mai sopra 206 dp dall'alto; se il pad è più in alto la colonna si ribalta sotto il pad (freccia ▲).

**Aiuti progressivi** (`OnboardingHints`, ordine della PR #90). Inattività = tempo **senza progresso verso il gate**: progresso = avvicinarsi di almeno 1 m al bersaglio (passo senza bersaglio: muoversi) oppure fare l'azione del passo (cambio passo). Il timer si azzera a ogni progresso. Log `[Onb] hint step=… level=… freccia|mano|camera`.
| dopo | livello | cosa |
|---|---|---|
| 10 s | 1 | biglietto di Ada ripetuto (ricompare con l'animazione) + il bersaglio pulsa: pad a terra ampiezza 16 % (contro 4-7 %), colonna prezzo 1,12× ogni 0,4 s |
| 20 s | 2 | bersaglio fuori camera: freccia a bordo schermo (36 dp + etichetta 12 dp, max 120 dp). Bersaglio già a schermo (o passo senza bersaglio): mano fantasma 48 dp nella zona joystick, trascina 56 dp verso il bersaglio, loop 1,4 s, sparisce quando il dito tocca; il suggerimento testuale si nasconde |
| 40 s | 3 | camera breve sul bersaglio **≤ 1,5 s andata e ritorno incluse** (rampe 0,4 s), joystick libero, **una sola volta per passo** |

**Camera breve** (`CameraPeek.Request(pos, sec)`): 0,4 s andata (smoothstep), tenuta, 0,4 s ritorno; joystick sempre attivo, se Nina si muove la tenuta finisce e la camera torna subito. **2,75 s solo per la presentazione di elementi nuovi** (comparsa di zone/pad/NONNO); **1,5 s** come aiuto a 40 s.

**Badge NONNO:** pillola menta 22 dp, icona 14 dp, testo 12 dp "6 pacchi/min" (= `NonnoRestockPpm` del livello), 2,1 m sopra il robot, solo con NONNO assunto e in camera.

**Combo** (decisione Game Designer, = `comboFeedback` di #106; `ComboPopup.Show(n)`, chiamato da `TutorialManager.NotifyCombo(clientiDiFila, moltiplicatore)`): a schermo **solo "+N"**, N = clienti serviti di fila (da 2 in su), ancorato sopra la testa di Nina (2,3 m), testo 16 dp sole #F4C430 con contorno bruno 2 px; sale di 40 dp e sparisce in 1,5 s (dissolvenza negli ultimi 0,5 s); un popup nuovo sostituisce il precedente; nessun contatore nell'HUD. Il moltiplicatore (x1,2 … x2) è interno: serve solo per il gate del passo 7 (`comboReached ≥ 1,2`) e **non va mai mostrato**.

**Salta tutorial:** pillola crema 26 dp, testo 11 dp, nella riga alta a sinistra dell'ingranaggio (fuori dalla zona pollice). Compare al gate combo (moltiplicatore ≥ 1,2 in `NotifyCombo`) o dopo il passo `SaltaDopo` (oggi Monete). Con il tasto visibile: pillola LN max 104 dp, obiettivo 112 dp → −134 dp. Effetto: rivela tutto, chiude Ada, tutorial chiuso, log `[Onb] skip` (i log `[Onb]` passano da `OnbLog`: accesi in Editor e build di sviluppo, spenti in rilascio WebGL).
QA anteprima: `ultimo-miglio/docs/qa/onboarding_94/` (360/390/412).

**Voce della Civetta** (00_BIBBIA: dealer del mercato nero, ex socio di Nonna Ada): frasi corte, ironica, misteriosa, cadenza ironica senza dialetti regionali, mai minacciosa apertamente; firma con 🦉; scrive solo al tramonto o di notte; parla della nonna con affetto ruvido. Non e' cattiva: e' un'alleata ambigua.


