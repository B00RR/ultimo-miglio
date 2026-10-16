# Playtest LIVE WebGL — Ultimo Miglio (build main `25ab5cf`)

**Data:** 2026-10-15 ~18:30 CEST (Europe/Rome)  
**URL:** https://b00rr.github.io/ultimo-miglio-web/ (cache-bust `?v=25ab5cf`)  
**Viewport:** 390×844 (telefono verticale)  
**Metodo:** Playwright MCP `user-Playwright` + script sync `qa/playtest_web_live_p4.py` (Chromium headless, skill webapp-testing + senior-qa adattato Unity/WebGL)  
**Vincoli rispettati:** no Unity editor, no `unity/`, no computerUse/desktop, no spesa reale  
**Destinatario report:** parent / Tech Lead (non Lorenzo)

---

## Sintesi esecutiva — 4 priorità Tech Lead

| # | Domanda | Verdetto | Gravità |
|---|---------|----------|---------|
| 1 | HUD invasivo? | **Sì** | P0 |
| 2 | Zone a terra chiare? | **Parzialmente; spesso poco leggibili** | P1 |
| 3 | Compra NONNO a 15 LN e lavora entro ~10 s? | **No** (pad visto; LN insufficienti in sessione; NavMesh rotto) | P0 |
| 4 | Obiettivo sempre in alto e chiaro? | **Parziale** (Ada in alto sì; waypoint non stabili in alto) | P1 |

---

## Risposte dettagliate (con evidenza)

### 1) HUD invasivo? — **SÌ (P0)**

**Cosa si vede**
- Il biglietto di Nonna Ada occupa ~1/4–1/3 dello schermo in alto e spesso **copre Nina** e la zona di gioco superiore.
- Etichette world-space **Prendi / Scaffale / Pacchi** si sovrappongono a mucchio vicino agli scaffali (testo illeggibile, personaggio nascosto).
- Il cartello pad **40 LN** + freccia obiettivo (es. ARRIVI) si sovrappongono al centro dello schermo.
- La pillola risorse in alto a sinistra resta spesso sotto/accanto al biglietto Ada.

**Passi di riproduzione**
1. Aprire l’URL in viewport 390×844.
2. Attendere il caricamento WebGL.
3. Osservare start tutorial e avanzare di 1–2 step (camminata → Arrivi).

**Evidenza screenshot**
- `qa/playtest_shots/n02_fresh_start.png` — Ada + overlap Prendi/Scaffale + pad 5/40 LN
- `qa/playtest_shots/n03_walk.png` / `n04_arrivi.png` — mucchio Prendi/Scaffale/Pacchi + ARRIVI
- `qa/playtest_shots/p4_05_pickup.png` / `p4_07_bancone.png` / `p4_11_nonno_pad.png` — Ada copre personaggio
- `qa/playtest_shots/p4_13_more_farm.png` — Nina ridotta a “testa dietro” Ada

**Impatto:** su telefono il giocatore non vede dove sta Nina né cosa sta cliccando/avvicinando; onboarding confuso.

---

### 2) Zone a terra chiare? — **PARZIALE / NO abbastanza (P1)**

**Cosa si vede**
- Esistono marker a terra: griglia gialla sotto i pad LN, griglia ciano/blu vicino scaffali/Bancone, X arancio, icone pacco/persona.
- Su pavimento già giallo/arancio il pad giallo ha **contrasto basso**.
- Le etichette flottanti **coprono** le zone proprio quando servono (Arrivi/scaffali).
- La “Zona Arrivi” non ha un rombo/etichetta a terra leggibile tipo “ZONA ARRIVI”; si capisce soprattutto dalla freccia `ARRIVI · N m`.

**Passi di riproduzione**
1. Arrivare allo step “Prendi i pacchi agli Arrivi”.
2. Avvicinarsi agli scaffali / griglia blu.
3. Confrontare leggibilità pad 15/40 LN vs pavimento.

**Evidenza**
- `n03_walk.png`, `n04_arrivi.png` — griglia blu + X ma zone confusе sotto le label
- `p4_05_pickup.png` — pad 15 LN e 40 LN con alone giallo debole
- `p4_10_pad_hunt.png` — pad 40 LN più chiaro (glow), zone blu scaffale/bancone più deboli

**Impatto:** proximity-trigger ok in teoria, ma a 9:16 il giocatore non capisce *dove* stare senza le frecce.

---

### 3) Compra NONNO a 15 LN e lavora entro ~10 s? — **NO (P0)**

**Cosa si vede**
- Pad da **15 LN** compare in mid-tutorial (`p4_05_pickup.png`); allo start fresco a volte si vede anche **5 LN** + 40 LN (`n02_fresh_start.png`) — pricing/pad set da verificare.
- In sessione P4 l’HUD arriva a **4** (icona orologio/LN) con bulloni a **0** (`p4_09_coins.png`, `p4_13_more_farm.png`): **non si raggiungono 15 LN** in modo affidabile nel tempo del playtest automatico prima che il tutorial sposti l’obiettivo su **ZAINO (40 LN)**.
- Nessuno screenshot mostra robot NONNO spawnato / in movimento / che deposita.
- Console Unity (ripetuto a ogni load):

```
Failed to create agent because there is no valid NavMesh
```

  → anche dopo un eventuale acquisto, l’agent NavMesh **non può lavorare**.

**Passi di riproduzione (tentati)**
1. Fresh load cache-bust `25ab5cf`, clear localStorage/IndexedDB.
2. Tutorial: cammina → Arrivi → scaffali → Bancone → monete.
3. Cercare pad 15 LN, sostare ~10+ s (`p4_11` → `p4_12`).
4. Controllare console per NavMesh.

**Evidenza**
- `p4_05_pickup.png` — pad **15 LN** e **40 LN** visibili
- `p4_09_coins.png` / `p4_13_more_farm.png` — tutorial su ZAINO; LN HUD ≈ 4; NONNO assente
- `p4_11_nonno_pad.png` / `p4_12_nonno_after_10s.png` — dopo attesa su pad: ancora ZAINO/40 LN, nessun NONNO
- `qa/playtest_console_final.log` — warning NavMesh ×3+

**Verdetto per TL:** **FAIL.** Non verificato “compra a 15 LN + lavora ≤10 s”. Blocco tecnico NavMesh = P0 indipendente dal tutorial economy.

---

### 4) Obiettivo sempre in alto e chiaro? — **PARZIALE (P1)**

**Cosa si vede**
- Il testo obiettivo via **biglietto Ada** è in alto e in italiano chiaro (passi tutorial leggibili).
- I waypoint (`ARRIVI · Xm`, `BANCONE · Xm`, `SCAFFALI · Xm`, `ZAINO · Xm`) **non restano fissi in alto**: compaiono a bordo mondo / centro / basso, e possono essere coperti da Ada o dalle label Prendi/Scaffale.
- Esempio: `SCAFFALI · 5 m` in basso a sinistra (`p4_05`); `ZAINO · 12 m` a metà destra (`p4_11`); `ARRIVI · 20 m` al centro (`n07`/`n08`).

**Passi di riproduzione**
1. Attraversare almeno 3 step tutorial (movimento → Arrivi → Bancone/Zaino).
2. Osservare posizione del biglietto Ada vs pillola freccia distanza.

**Evidenza**
- `n02_fresh_start.png` — Ada in alto chiaro (start)
- `n03_walk.png` — Ada + ARRIVI mid
- `p4_05_pickup.png` — SCAFFALI in basso
- `p4_07_bancone.png` — Ada + BANCONE · 7 m
- `p4_11_nonno_pad.png` — ZAINO · 12 m non “sempre in alto”

**Impatto:** istruzione testuale ok; guida spaziale inconsistente su portrait.

---

## Altri bug / osservazioni (classifica)

### P0 — Bloccanti
1. **NavMesh assente / agent fallisce** — `Failed to create agent because there is no valid NavMesh` (console). Impatto diretto su NONNO e qualsiasi AI pathfinding.  
   Evidenza: `playtest_console_final.log`, `playtest_console_newbuild.log`
2. **HUD/Ada + label world-space invasivi su 390×844** — vedi §1.

### P1 — Alta
3. **Overlap etichette Prendi/Scaffale/Pacchi** — stacking illeggibile vicino scaffali.  
4. **Zone a terra a basso contrasto / coperte** — vedi §2.  
5. **Waypoint obiettivo non ancorati in alto** — vedi §4.  
6. **Economia tutorial vs pad NONNO** — difficile arrivare a 15 LN prima che Ada chieda ZAINO a 40 LN; ordine/prezzi da riallineare.

### P2 — Media / bassa
7. **CSS UI Toolkit:** `Unknown pseudo class "last-child" in StyleSheet HudV2`.  
8. **favicon.ico 404** su `b00rr.github.io`.  
9. **Deprecation** `JS_FileSystem_Sync` / persistentDataPath (warning Unity WebGL).  
10. **Telefono (TAB):** app tutte lucchettate a Giorno 1 (atteso early tutorial; ok se by design). Evidenza storica: `42_tab_phone.png`.

---

## Copertura tutorial (cosa è stato giocato)

| Step | Esito sessione `25ab5cf` | Shot chiave |
|------|--------------------------|-------------|
| Start / WASD-joystick | OK | `n02_fresh_start.png`, `p4_01_start.png` |
| Cammina | OK | `n03_walk.png`, `p4_03_after_walk.png` |
| Arrivi / pacchi | OK (guida + pickup in P4) | `n04_arrivi.png`, `p4_05_pickup.png` |
| Scaffali | OK (Ada “per colore”) | `p4_05`, `p4_06_shelves.png` |
| Bancone + clienti | OK (clienti visibili) | `p4_07_bancone.png` |
| Monete | Parziale (label Monete; LN basse) | `p4_09_coins.png`, `n07_coins_ln.png` |
| Zaino | Obiettivo Ada attivo; pad 40 LN | `p4_09`–`p4_13` |
| NONNO 15 LN + lavoro ≤10s | **FAIL** | `p4_11`, `p4_12`, console NavMesh |

---

## Errori console Unity rilevanti

| Livello | Messaggio | Note |
|---------|-----------|------|
| WARNING | `Failed to create agent because there is no valid NavMesh` | **P0** — ripete a ogni load |
| WARNING | `Unknown pseudo class "last-child" in StyleSheet HudV2` | UI Toolkit |
| WARNING | `Manual synchronization of Unity Application.persistentDataPath via JS_FileSystem_Sync() is deprecated...` | WebGL config |
| ERROR | `favicon.ico` 404 | Cosmetico host |

Log salvati: `qa/playtest_console_final.log`, `qa/playtest_console_newbuild.log`.

---

## Path artefatti

- **Report:** `/workspace/ultimo-miglio/qa/playtest_web_live.md`
- **Screenshot (nuova build):** `/workspace/ultimo-miglio/qa/playtest_shots/n01_*.png` … `n08_*.png`, `p4_01_*.png` … `p4_13_*.png`
- **Script usati:** `qa/playtest_web_live.py`, `qa/playtest_web_live_p4.py`
- **Console:** `qa/playtest_console_final.log`

---

## Raccomandazioni immediate per Tech Lead

1. **Bake / assegnare NavMesh** sulla scena Ch1 (o disabilitare agent finché non c’è) — sblocca NONNO.  
2. **Clamp biglietto Ada** (safe area, max altezza, non coprire Nina) + **dedupe/stack** label world-space su portrait.  
3. **Waypoint obiettivo fisso in alto** (sotto safe area, sopra Ada o integrato) oltre alla freccia bordo.  
4. **Contrasto zone a terra** (bordo crema / fill distinto dal tile) e label “ZONA ARRIVI”.  
5. **Bilanciare pad NONNO 15 LN** rispetto al flusso tutorial (arrivare a 15 LN *prima* di ZAINO 40, e verificare lavoro ≤10 s in PlayMode).
